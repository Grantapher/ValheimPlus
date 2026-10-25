#!/usr/bin/env python3
"""
Publishes a release built by create_releases.sh to GitHub, Thunderstore, Hexium and Nexus Mods,
and writes the Discord post. Run from the repo root after create_releases.sh, once the release notes
in release-notes/<version>.md are filled in and the release commits are pushed.

  python bin/publish_release.py --headline "Valheim 1.0.20 Compatibility" --dry-run
  python bin/publish_release.py --headline "Valheim 1.0.20 Compatibility"
  python bin/publish_release.py --headline "..." --only nexus,hexium
  python bin/publish_release.py --headline "..." --dry-run --skip-git-check   (before pushing)

Tokens come from environment variables or bin/.publish_secrets.json (gitignored):
  {"thunderstore_token": "tss_...", "hexium_token": "...", "nexus_api_key": "..."}
"""
import argparse
import base64
import hashlib
import json
import math
import os
import re
import subprocess
import sys
import time
import urllib.error
import urllib.request
import zipfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
SECRETS_PATH = REPO_ROOT / "bin" / ".publish_secrets.json"

GITHUB_REPO = "Grantapher/ValheimPlus"
PACKAGE_NAMESPACE = "Grantapher"
PACKAGE_NAME = "ValheimPlus_Grantapher_Temporary"
COMMUNITY = "valheim"
THUNDERSTORE_URL = "https://thunderstore.io"
HEXIUM_URL = "https://hexium.gg"

NEXUS_API = "https://api.nexusmods.com/v3"
NEXUS_GAME = "valheim"
NEXUS_MOD_PAGE_ID = "2323"  # the number in the mod page URL
# Release file -> Nexus file name. The API ids are looked up by name. update_mod_version is set on the first.
NEXUS_FILES = [
    ("UnixClient.zip", "ValheimPlus (Bundled with BepInEx)"),
    ("Vortex.zip", "ValheimPlus (Vortex)"),
]

TARGETS = ["github", "thunderstore", "hexium", "nexus", "discord"]
USER_AGENT = "ValheimPlus-publish-release"


class PublishError(Exception):
    pass


def log(message):
    print(message, flush=True)


# ---------------------------------------------------------------- helpers

def read_version():
    source = (REPO_ROOT / "ValheimPlus" / "ValheimPlus.cs").read_text(encoding="utf-8-sig")
    numeric = re.search(r'public const string NumericVersion = "([^"]+)"', source).group(1)
    extra = re.search(r'private const string VersionExtra = "([^"]*)"', source).group(1)
    return numeric + extra


def load_secrets():
    secrets = {}
    if SECRETS_PATH.exists():
        secrets.update(json.loads(SECRETS_PATH.read_text(encoding="utf-8")))
    for key in ("thunderstore_token", "hexium_token", "nexus_api_key"):
        env = os.environ.get(key.upper())
        if env:
            secrets[key] = env
    return secrets


def require(secrets, key):
    value = secrets.get(key)
    if not value:
        raise PublishError(f"Missing {key}. Set {key.upper()} or add it to {SECRETS_PATH.name}.")
    return value


def http(method, url, headers=None, body=None, expect_json=True):
    """Sends a request and returns (parsed body or raw bytes, response headers)."""
    headers = {"User-Agent": USER_AGENT, **(headers or {})}
    if isinstance(body, (dict, list)):
        body = json.dumps(body).encode("utf-8")
        headers.setdefault("Content-Type", "application/json")
    request = urllib.request.Request(url, data=body, method=method, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            data = response.read()
            if expect_json and data:
                return json.loads(data), response.headers
            return data, response.headers
    except urllib.error.HTTPError as e:
        detail = e.read().decode("utf-8", "replace")[:2000]
        raise PublishError(f"{method} {url} failed with {e.code}: {detail}") from None


def split_sections(markdown):
    """Splits markdown into (heading, body) pairs on top-level '# ' headings."""
    sections, heading, lines = [], None, []
    for line in markdown.splitlines():
        if line.startswith("# "):
            if heading is not None or lines:
                sections.append((heading, "\n".join(lines).strip()))
            heading, lines = line[2:].strip(), []
        else:
            lines.append(line)
    sections.append((heading, "\n".join(lines).strip()))
    return sections


def section(sections, name):
    return next((body for heading, body in sections if heading == name), None)


# ---------------------------------------------------------------- GitHub

def git(*args):
    return subprocess.run(["git", *args], cwd=REPO_ROOT, capture_output=True, text=True, encoding="utf-8",
                          errors="replace", check=True).stdout.strip()


def check_local_matches_github():
    """The release is tagged on GitHub's main, so it has to be the commit the files were built from."""
    if git("status", "--porcelain", "--untracked-files=no"):
        raise PublishError("There are uncommitted changes to tracked files. Commit them and rebuild first.")
    local = git("rev-parse", "HEAD")
    remote = subprocess.run(["gh", "api", f"repos/{GITHUB_REPO}/commits/main", "--jq", ".sha"],
                            capture_output=True, text=True, encoding="utf-8", errors="replace",
                            check=True).stdout.strip()
    if local != remote:
        raise PublishError(f"Local HEAD {local[:7]} is not GitHub's main {remote[:7]}. Push (or pull) first.")
    log(f"  local HEAD matches GitHub main ({local[:7]})")


def check_github(ctx):
    tag, title = ctx["version"], f'{ctx["version"]} - {ctx["headline"]}'
    existing = subprocess.run(["gh", "release", "view", tag, "--repo", GITHUB_REPO],
                              capture_output=True, text=True, encoding="utf-8", errors="replace")
    if existing.returncode == 0:
        raise PublishError(f"GitHub release {tag} already exists.")

    assets = sorted(str(p) for p in ctx["release_dir"].iterdir() if p.is_file())
    log(f"  release '{title}' on main")
    log(f"  notes: {ctx['notes_path'].relative_to(REPO_ROOT)}")
    log(f"  {len(assets)} files:")
    for asset in assets:
        log(f"    {Path(asset).relative_to(REPO_ROOT)}")

    def upload():
        subprocess.run(["gh", "release", "create", tag, *assets, "--repo", GITHUB_REPO, "--target", "main",
                        "--title", title, "--notes-file", str(ctx["notes_path"])], check=True)
    return upload


# ---------------------------------------------------------------- Thunderstore and Hexium

def package_categories(base_url):
    """The package's current category slugs in the community, so a new version keeps them."""
    package, _ = http("GET", f"{base_url}/api/experimental/package/{PACKAGE_NAMESPACE}/{PACKAGE_NAME}/")
    listing = next((l for l in package.get("community_listings") or [] if l["community"] == COMMUNITY), None)
    if not listing:
        raise PublishError(f"No {COMMUNITY} listing found on {base_url}.")

    slugs, url = {}, f"{base_url}/api/experimental/community/{COMMUNITY}/category/"
    while url:
        page, _ = http("GET", url)
        for category in page.get("results", []):
            slugs[category["name"]] = category["slug"]
        url = page.get("pagination", {}).get("next_link") or page.get("next")
    missing = [name for name in listing["categories"] if name not in slugs]
    if missing:
        log(f"  warning: no slug found for categories {missing}; they will be left out")
    return package["latest"]["version_number"], [(n, slugs[n]) for n in listing["categories"] if n in slugs]


def check_thunderstore_like(ctx, base_url, token_key):
    zip_path = ctx["release_dir"] / "Thunderstore.zip"
    with zipfile.ZipFile(zip_path) as archive:
        manifest = json.loads(archive.read("manifest.json").decode("utf-8-sig"))
    version = manifest["version_number"]

    latest, named_categories = package_categories(base_url)
    categories = [slug for _, slug in named_categories]
    log(f"  {zip_path.relative_to(REPO_ROOT)}: {PACKAGE_NAMESPACE}-{manifest['name']} {version} (currently {latest})")
    log(f"  {len(categories)} categories: {', '.join(name for name, _ in named_categories)}")
    if manifest["name"] != PACKAGE_NAME:
        raise PublishError(f"manifest.json name is {manifest['name']}, expected {PACKAGE_NAME}.")
    if latest == version:
        raise PublishError(f"{version} is already published on {base_url}.")

    token = require(ctx["secrets"], token_key)
    return lambda: upload_thunderstore_like(base_url, token, zip_path, categories)


def upload_thunderstore_like(base_url, token, zip_path, categories):
    auth = {"Authorization": f"Bearer {token}"}
    api = f"{base_url}/api/experimental"

    size = zip_path.stat().st_size
    initiated, _ = http("POST", f"{api}/usermedia/initiate-upload/", auth,
                        {"filename": zip_path.name, "file_size_bytes": size})
    uuid = initiated["user_media"]["uuid"]
    try:
        parts = []
        with zip_path.open("rb") as f:
            for part in initiated["upload_urls"]:
                f.seek(part["offset"])
                chunk = f.read(part["length"])
                md5 = base64.b64encode(hashlib.md5(chunk).digest()).decode()
                _, headers = http("PUT", part["url"], {"Content-MD5": md5}, chunk, expect_json=False)
                parts.append({"ETag": headers["ETag"], "PartNumber": part["part_number"]})
                log(f"  uploaded part {part['part_number']}/{len(initiated['upload_urls'])}")
        http("POST", f"{api}/usermedia/{uuid}/finish-upload/", auth, {"parts": parts})
    except Exception:
        try:
            http("POST", f"{api}/usermedia/{uuid}/abort-upload/", auth, {})
        except PublishError:
            pass
        raise

    result, _ = http("POST", f"{api}/submission/submit/", auth, {
        "author_name": PACKAGE_NAMESPACE,
        "communities": [COMMUNITY],
        "community_categories": {COMMUNITY: categories},
        "categories": [],
        "has_nsfw_content": False,
        "upload_uuid": uuid,
    })
    log(f"  published {result['package_version']['full_name']}")


# ---------------------------------------------------------------- Nexus

def nexus_upload(ctx, api_key, path):
    headers = {"apikey": api_key}
    created, _ = http("POST", f"{NEXUS_API}/uploads/multipart", headers,
                      {"filename": path.name, "size_bytes": str(path.stat().st_size)})
    data = created["data"]
    upload_id, part_size = data["id"], data["part_size_bytes"]

    parts = []
    with path.open("rb") as f:
        for number, url in enumerate(data["part_presigned_urls"], start=1):
            f.seek((number - 1) * part_size)
            chunk = f.read(part_size)
            _, response_headers = http("PUT", url, {"Content-Type": "application/octet-stream"}, chunk,
                                       expect_json=False)
            parts.append((number, response_headers["ETag"].replace('"', "")))
    xml = "<CompleteMultipartUpload>" + "".join(
        f"<Part><PartNumber>{n}</PartNumber><ETag>{etag}</ETag></Part>" for n, etag in parts
    ) + "</CompleteMultipartUpload>"
    http("POST", data["complete_presigned_url"], {"Content-Type": "application/xml"}, xml.encode(),
         expect_json=False)

    http("POST", f"{NEXUS_API}/uploads/{upload_id}/finalise", headers)
    delay = 2.0
    for _ in range(60):
        state, _ = http("GET", f"{NEXUS_API}/uploads/{upload_id}", headers)
        if state["data"]["state"] == "available":
            return upload_id
        time.sleep(delay)
        delay = min(delay * 1.5, 30.0)
    raise PublishError(f"Nexus upload {upload_id} never became available.")


def check_nexus(ctx):
    api_key = require(ctx["secrets"], "nexus_api_key")
    headers = {"apikey": api_key}

    # The upload API uses its own ids, not the ones in the site's download links, so look them up.
    mod, _ = http("GET", f"{NEXUS_API}/games/{NEXUS_GAME}/mods/{NEXUS_MOD_PAGE_ID}", headers)
    mod_id = mod["data"]["id"]
    listed, _ = http("GET", f"{NEXUS_API}/mods/{mod_id}/files", headers)
    ids_by_name = {f["name"]: f["id"] for f in listed["data"]["mod_files"] if f["is_active"]}
    log(f"  mod {NEXUS_MOD_PAGE_ID} has API id {mod_id}")

    files = []
    for name, display in NEXUS_FILES:
        path = ctx["release_dir"] / name
        if not path.exists():
            raise PublishError(f"{path} is missing.")
        if display not in ids_by_name:
            raise PublishError(f"No active Nexus file named '{display}'. Found: {', '.join(ids_by_name)}")
        files.append((path, ids_by_name[display], display))
        log(f"  {path.relative_to(REPO_ROOT)} -> file {ids_by_name[display]} ({display})")
    log("  changelog:")
    for line in ctx["changelog_lines"]:
        log(f"    {line}")
    return lambda: upload_nexus(ctx, api_key, mod_id, files)


def upload_nexus(ctx, api_key, mod_id, files):
    for index, (path, file_id, display) in enumerate(files):
        upload_id = nexus_upload(ctx, api_key, path)
        http("POST", f"{NEXUS_API}/mod-files/{file_id}/versions", {"apikey": api_key}, {
            "upload_id": upload_id,
            "name": display,
            "version": ctx["version"],
            "file_category": "main",
            "archive_existing_file": False,
            "update_mod_version": index == 0,
        })
        log(f"  published {path.name}")

    if ctx["changelog_lines"]:
        http("POST", f"{NEXUS_API}/mods/{mod_id}/changelogs", {"apikey": api_key},
             {"version": ctx["version"], "changelog": "\n".join(ctx["changelog_lines"])})
        log("  posted changelog")


# ---------------------------------------------------------------- Discord

def build_discord(ctx):
    sections = ctx["sections"]
    version, headline = ctx["version"], ctx["headline"]
    links = section(sections, "Other Links") or ""
    lines = [
        f"# Release `{version}` - {headline}",
        "",
        "## Links",
        f"* [Github](https://github.com/{GITHUB_REPO}/releases/tag/{version})",
        links,
        "",
    ]
    # Behavior changes come first, as a warning before the patch notes.
    behavior_changes = section(sections, "Behavior Changes")
    if behavior_changes:
        lines += ["# Behavior Changes", behavior_changes, ""]
    lines += ["# Patch Notes", section(sections, "Patch Notes") or ""]
    contributors = section(sections, "Contributors")
    if contributors:
        lines += ["", "# Contributors", contributors]
    lines += ["", "Detailed patch notes can be found in the github link above.", ""]
    return "\n".join(lines)


def check_discord(ctx):
    path = ctx["notes_path"].with_name(f"{ctx['version']}-discord.md")
    text = build_discord(ctx)
    log(f"  {path.relative_to(REPO_ROOT)} ({len(text)} characters)")
    if len(text) > 2000:
        log("  note: over Discord's 2000-character message limit for non-Nitro accounts")
    # Written even in a dry run, so the post can be previewed.
    path.write_text(text, encoding="utf-8")
    return lambda: None


# ---------------------------------------------------------------- main

def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--headline", required=True, help='e.g. "Valheim 1.0.20 Compatibility"')
    parser.add_argument("--only", help=f"comma-separated subset of {','.join(TARGETS)}")
    parser.add_argument("--dry-run", action="store_true", help="check and read everything, upload nothing")
    parser.add_argument("--skip-git-check", action="store_true",
                        help="with --dry-run, skip checking that local HEAD is pushed and the tree is clean")
    args = parser.parse_args()

    if args.skip_git_check and not args.dry_run:
        parser.error("--skip-git-check only works with --dry-run")

    targets = TARGETS if not args.only else [t.strip() for t in args.only.split(",")]
    unknown = [t for t in targets if t not in TARGETS]
    if unknown:
        parser.error(f"unknown targets {unknown}")

    version = read_version()
    release_dir = REPO_ROOT / "release" / version
    notes_path = REPO_ROOT / "release-notes" / f"{version}.md"
    for required in (release_dir, notes_path):
        if not required.exists():
            sys.exit(f"{required.relative_to(REPO_ROOT)} is missing. Run create_releases.sh and write the notes first.")

    notes = notes_path.read_text(encoding="utf-8")
    if "TODO" in notes:
        sys.exit(f"{notes_path.relative_to(REPO_ROOT)} still has TODO placeholders.")
    sections = split_sections(notes)
    patch_notes = section(sections, "Patch Notes")
    if not patch_notes:
        sys.exit(f"{notes_path.name} has no '# Patch Notes' section.")

    def changelog_lines(sections):
        """Nexus changelog entries are plain text, so markdown bullets and backticks are dropped."""
        def plain(body, prefix=""):
            return [prefix + re.sub(r"^\s*[*-]\s*", "", l).replace("`", "") for l in body.splitlines() if l.strip()]
        return plain(section(sections, "Behavior Changes") or "", "Behavior change: ") + plain(patch_notes)

    ctx = {
        "version": version,
        "headline": args.headline,
        "release_dir": release_dir,
        "notes_path": notes_path,
        "sections": sections,
        "changelog_lines": changelog_lines(sections),
        "secrets": load_secrets(),
    }

    log(f"Publishing {version} - {args.headline}{' (dry run)' if args.dry_run else ''}")
    checks = {
        "github": check_github,
        "thunderstore": lambda c: check_thunderstore_like(c, THUNDERSTORE_URL, "thunderstore_token"),
        "hexium": lambda c: check_thunderstore_like(c, HEXIUM_URL, "hexium_token"),
        "nexus": check_nexus,
        "discord": check_discord,
    }
    errors = (PublishError, subprocess.CalledProcessError, OSError, KeyError)
    selected = [t for t in TARGETS if t in targets]

    # Check everything before uploading anything, so a problem never leaves a partial release.
    log("Checking")
    uploads, failed = {}, []
    try:
        if args.skip_git_check:
            log("  skipped the git check")
        else:
            check_local_matches_github()
    except errors as e:
        log(f"  FAILED: {e}")
        failed.append("git")
    for target in selected:
        log(f"[{target}]")
        try:
            uploads[target] = checks[target](ctx)
        except errors as e:
            log(f"  FAILED: {e}")
            failed.append(target)
    if failed:
        sys.exit(f"Checks failed for {', '.join(failed)}. Nothing was uploaded.")
    if args.dry_run:
        log("All checks passed. Dry run, so nothing was uploaded.")
        return

    log("Uploading")
    for target in selected:
        log(f"[{target}]")
        try:
            uploads[target]()
            log("  ok")
        except errors as e:
            log(f"  FAILED: {e}")
            failed.append(target)

    if failed:
        sys.exit(f"Failed: {', '.join(failed)}. Retry with --only {','.join(failed)}")
    log("Done.")


if __name__ == "__main__":
    main()

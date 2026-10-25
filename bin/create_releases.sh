#!/bin/bash
# run from the ValheimPlus repo root dir

# Update this as necessary:
BEPINEXPACK_VALHEIM_VERSION="5.4.2351"

# Constants:
NUMERIC_VERSION=$(grep "public const string NumericVersion" ValheimPlus/ValheimPlus.cs | cut -f2 -d'"')
VERSION_EXTRA=$(grep "private const string VersionExtra" ValheimPlus/ValheimPlus.cs | cut -f2 -d'"')
VERSION="${NUMERIC_VERSION}${VERSION_EXTRA}"
OUTPUT_DIR=$(realpath "release/$VERSION")
TEMP_DIR=$(realpath "release/temp")
VALHEIM_PLUS_DLL="ValheimPlus\bin\Debug\ValheimPlus.dll"
BEPINEXPACK_VALHEIM_TEMP_DIR="$TEMP_DIR/denikson-BepInExPack_Valheim"
BEPINEXPACK_VALHEIM_DOWNLOAD_URL="https://gcdn.thunderstore.io/live/repository/packages/denikson-BepInExPack_Valheim-$BEPINEXPACK_VALHEIM_VERSION.zip"
PLUGINS_DIR="$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim/BepInEx/plugins"
VALHEIM_PLUS_DLL_DESTINATION="$PLUGINS_DIR/ValheimPlus.dll"
VALHEIM_PLUS_DLL_DESTINATION_RENAMED="$PLUGINS_DIR/ValheimPlusGrantapher.dll"
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Prompt with version info
echo "This will build release files for ValheimPlus with the following options":
echo -e "${YELLOW}Valheim Plus version        = $VERSION"
echo -e "BepInExPack_Valheim version = $BEPINEXPACK_VALHEIM_VERSION${NC}"

# CHANGELOG.md ships in the Thunderstore zip, so flag a missing entry before building.
if ! grep -q "^## $VERSION\b" "CHANGELOG.md"; then
    echo -e "${RED}Warning: CHANGELOG.md has no '## $VERSION' entry.${NC}" 1>&2
    echo -e "${RED}Thunderstore will show the changelog without this release.${NC}" 1>&2
fi

# Start the release notes that publish_release.py reads, unless they already exist.
RELEASE_NOTES="release-notes/$VERSION.md"
if [ ! -f "$RELEASE_NOTES" ]; then
    GAME_VERSION=$(grep "TargetGameVersion = new(" ValheimPlus/ValheimPlus.cs | sed -E 's/.*new\(([0-9]+), *([0-9]+), *([0-9]+)\).*/\1.\2.\3/')
    mkdir -p "release-notes"
    cat <<EOF > "$RELEASE_NOTES"
# Other Links
* [Thunderstore](https://valheim.thunderstore.io/package/Grantapher/ValheimPlus_Grantapher_Temporary/)
* [Hexium](https://valheim.hexium.gg/mods/Grantapher/ValheimPlus_Grantapher_Temporary)
* [Nexus](https://www.nexusmods.com/valheim/mods/2323)

# Patch Notes
* TODO

# Install

See [\`INSTALL.md\`](https://github.com/Grantapher/ValheimPlus/blob/main/INSTALL.md). Will update as needed, so join [the discord](https://discord.gg/WU69A2JTcn) if you have issues. Also open to PRs updating this.

# Troubleshooting

See [\`TROUBLESHOOTING.md\`](https://github.com/Grantapher/ValheimPlus/blob/main/TROUBLESHOOTING.md). Will update as needed, so join [the discord](https://discord.gg/WU69A2JTcn) if you have issues. Also open to PRs updating this.

# Compatibility
* ✅ - \`Valheim $GAME_VERSION\` + \`BepInExPack_Valheim $BEPINEXPACK_VALHEIM_VERSION\` + \`ValheimPlus $VERSION\`
* ⛔️ - Anything else

# Detailed Patch Notes

* TODO
EOF
    echo -e "${YELLOW}Created $RELEASE_NOTES. Fill in the TODOs (and add a '# Contributors' section if needed) before publishing.${NC}"
fi

# Build the solution (not the project; the project alone fails its post-build step).
VSWHERE="/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe"
MSBUILD=$("$VSWHERE" -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | head -n 1 | tr -d '\r')
if [ -z "$MSBUILD" ]; then
    echo -e "${RED}Couldn't find MSBuild through vswhere.${NC}" 1>&2
    exit 1
fi
echo "Building ValheimPlus..."
if ! "$MSBUILD" ValheimPlus.sln -t:Build -p:Configuration=Debug -v:minimal -nologo; then
    echo -e "${RED}Build failed.${NC}" 1>&2
    exit 1
fi

# Catch a stale build before packaging it.
DLL_VERSION=$(powershell.exe -NoProfile -Command "(Get-Item '$VALHEIM_PLUS_DLL').VersionInfo.ProductVersion" | tr -d '\r')
if [ "$DLL_VERSION" != "$VERSION" ]; then
    echo -e "${RED}ValheimPlus.dll is version '$DLL_VERSION', expected '$VERSION'. Rebuild first.${NC}" 1>&2
    exit 1
fi

while true; do
    read -p "Proceed? [y/n] " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) exit 1;;
        * ) echo "Please answer yes or no.";;
    esac
done

# Clear and recreate dirs
rm -rf "$OUTPUT_DIR" "$TEMP_DIR"
mkdir -p "$OUTPUT_DIR"
mkdir -p "$TEMP_DIR"

# move simple files
cp "$VALHEIM_PLUS_DLL" "$OUTPUT_DIR"
if [ $? -ne 0 ]; then
    echo -e "${RED}Couldn't find the ValheimPlus.dll $BEPINEXPACK_VALHEIM_VERSION${NC}" 1>&2
    exit 1
fi

# Create Bepinex bundle
BEPINEXPACK_VALHEIM_ZIP_FILE="$TEMP_DIR/denikson-BepInExPack_Valheim.zip"
curl -fso "$BEPINEXPACK_VALHEIM_ZIP_FILE" "$BEPINEXPACK_VALHEIM_DOWNLOAD_URL"
if [ $? -ne 0 ]; then
    echo -e "${RED}Couldn't download BepInExPack_Valheim version $BEPINEXPACK_VALHEIM_VERSION${NC}" 1>&2
    exit 1
fi

unzip -q "$BEPINEXPACK_VALHEIM_ZIP_FILE" -d "$BEPINEXPACK_VALHEIM_TEMP_DIR"
mkdir -p "$PLUGINS_DIR"
cp "$VALHEIM_PLUS_DLL" "$VALHEIM_PLUS_DLL_DESTINATION"

# trim down files to just those needed for Unix.
rm "$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim/changelog.txt"

# create unix zips
( \
    cd "$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim/" \
        && zip -qr "$OUTPUT_DIR/UnixServer.zip" . \
        && tar -czf "$OUTPUT_DIR/UnixServer.tar.gz" * \
)

cp "$OUTPUT_DIR/UnixServer.zip" "$OUTPUT_DIR/UnixClient.zip"
cp "$OUTPUT_DIR/UnixServer.tar.gz" "$OUTPUT_DIR/UnixClient.tar.gz"


# rename dll and re-zip
mv "$VALHEIM_PLUS_DLL_DESTINATION" "$VALHEIM_PLUS_DLL_DESTINATION_RENAMED"
( \
    cd "$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim/" \
        && zip -qr "$OUTPUT_DIR/UnixServerRenamed.zip" . \
        && tar -czf "$OUTPUT_DIR/UnixServerRenamed.tar.gz" * \
)

# Trim down to windows files
rm "$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim/start_game_bepinex.sh"
rm "$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim/start_server_bepinex.sh"

# create windows zips
( \
    cd "$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim/" \
        && zip -qr "$OUTPUT_DIR/WindowsServerRenamed.zip" . \
        && tar -czf "$OUTPUT_DIR/WindowsServerRenamed.tar.gz" * \
)

# restore original dll name and re-zip
mv "$VALHEIM_PLUS_DLL_DESTINATION_RENAMED" "$VALHEIM_PLUS_DLL_DESTINATION"
( \
    cd "$BEPINEXPACK_VALHEIM_TEMP_DIR/BepInExPack_Valheim/" \
        && zip -qr "$OUTPUT_DIR/WindowsServer.zip" . \
        && tar -czf "$OUTPUT_DIR/WindowsServer.tar.gz" * \
)

cp "$OUTPUT_DIR/WindowsServer.zip" "$OUTPUT_DIR/WindowsClient.zip"
cp "$OUTPUT_DIR/WindowsServer.tar.gz" "$OUTPUT_DIR/WindowsClient.tar.gz"

# Vortex zip
VORTEX="$TEMP_DIR/Vortex/"
VORTEX_MOD_DIR="$VORTEX/plugins/ValheimPlus"
mkdir -p "$VORTEX_MOD_DIR"
cp "$VALHEIM_PLUS_DLL" "$VORTEX_MOD_DIR"
(cd "$VORTEX" && zip -qr "$OUTPUT_DIR/Vortex.zip" .)

# Thunderstore zip
THUNDERSTORE="$TEMP_DIR/Thunderstore"
THUNDERSTORE_PLUGINS="$THUNDERSTORE/BepInEx/plugins"
mkdir -p "$THUNDERSTORE_PLUGINS"
cp "resources/images/icon.png" "$THUNDERSTORE"
cp "README.md" "$THUNDERSTORE"
cp "CHANGELOG.md" "$THUNDERSTORE"
cp "$VALHEIM_PLUS_DLL" "$THUNDERSTORE_PLUGINS"

cat <<EOF > "$THUNDERSTORE/manifest.json"
{
    "name": "ValheimPlus_Grantapher_Temporary",
    "version_number": "$(echo "$NUMERIC_VERSION" | cut -d'.' -f1-3)",
    "website_url": "https://discord.gg/WU69A2JTcn",
    "description": "A (not so) temporary Valheim Plus fork. Provides many configurable game tweaks.",
    "dependencies": [
        "denikson-BepInExPack_Valheim-$BEPINEXPACK_VALHEIM_VERSION"
    ]
}
EOF

(cd "$THUNDERSTORE" && zip -qr "$OUTPUT_DIR/Thunderstore.zip" .)

rm -rf "$TEMP_DIR"

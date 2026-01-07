Param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Resolve repo root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "ValheimPlus test runner starting..."

# Check VALHEIM_INSTALL requirement
if (-not $env:VALHEIM_INSTALL) {
    $repoValheim = Join-Path $RepoRoot "valheim/server"
    if (Test-Path $repoValheim) {
        $env:VALHEIM_INSTALL = (Resolve-Path -Path $repoValheim).Path
        Write-Host "Auto-detected VALHEIM_INSTALL at '$($env:VALHEIM_INSTALL)'" -ForegroundColor Cyan
    }
}
if (-not $env:VALHEIM_INSTALL) {
    Write-Error "ENV 'VALHEIM_INSTALL' not set and no repo-local valheim/server found. Tests rely on Valheim/BepInEx assemblies. Set VALHEIM_INSTALL to your Valheim install (client or dedicated server)."
    exit 1
}

# Ensure NuGet CLI
function Get-NuGetExe {
    $nugetCmd = Get-Command nuget -ErrorAction SilentlyContinue
    if ($nugetCmd) { return $nugetCmd.Path }

    $localNuget = Join-Path $RepoRoot "packages/nuget.exe"
    if (-not (Test-Path $localNuget)) {
        Write-Host "Downloading nuget.exe..."
        $nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
        Invoke-WebRequest -Uri $nugetUrl -OutFile $localNuget
    }
    return $localNuget
}

$nugetExe = Get-NuGetExe
Write-Host "Using NuGet at: $nugetExe"

# Restore packages for solution
& $nugetExe restore (Join-Path $RepoRoot "ValheimPlus.sln") | Write-Host

# Build solution (tests + main project)
$msbuildCmd = Get-Command msbuild -ErrorAction SilentlyContinue
# Build tests project only to avoid game dependency builds
Write-Host "Building tests project..." -ForegroundColor Green
$testsProj = Join-Path $PSScriptRoot "..\tests\ValheimPlus.Tests\ValheimPlus.Tests.csproj"
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) {
    & $msbuild.Path "$testsProj" /t:Build /p:Configuration=Debug
}
else {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) {
        & $dotnet build "$testsProj" -c Debug
    }
    else {
        throw "Neither msbuild nor dotnet found. Please install one to build the tests project."
    }
}

# Copy Valheim managed assemblies into test bin to satisfy runtime deps
$valheimData = Join-Path $env:VALHEIM_INSTALL "valheim_server_Data"
if (-not (Test-Path (Join-Path $valheimData "Managed"))) {
    $valheimData = Join-Path $env:VALHEIM_INSTALL "valheim_Data"
}
$managedDir = Join-Path $valheimData "Managed"
$pubDir = Join-Path $managedDir "publicized_assemblies"
$testsBin = Join-Path $RepoRoot ("tests/ValheimPlus.Tests/bin/" + $Configuration)
if (-not (Test-Path $testsBin)) { New-Item -ItemType Directory -Path $testsBin | Out-Null }
Get-ChildItem -Path $managedDir -Filter '*.dll' -File | ForEach-Object { Copy-Item $_.FullName -Destination $testsBin -Force }
if (Test-Path $pubDir) {
    Get-ChildItem -Path $pubDir -Filter '*.dll' -File | ForEach-Object { Copy-Item $_.FullName -Destination $testsBin -Force }
}

# Copy MonoMod dependencies used by HarmonyX (net452 assets)
$mmRuntime = Join-Path $RepoRoot "packages/MonoMod.RuntimeDetour.22.5.1.1/lib/net452/MonoMod.RuntimeDetour.dll"
$mmUtils = Join-Path $RepoRoot "packages/MonoMod.Utils.22.5.1.1/lib/net452/MonoMod.Utils.dll"
if (Test-Path $mmRuntime) { Copy-Item $mmRuntime -Destination $testsBin -Force }
if (Test-Path $mmUtils) { Copy-Item $mmUtils -Destination $testsBin -Force }

# Locate NUnit console runner
$runnerDefault = Join-Path $RepoRoot "packages/NUnit.ConsoleRunner.3.16.3/tools/nunit3-console.exe"
$runner = $runnerDefault
if (-not (Test-Path $runnerDefault)) {
    $found = Get-ChildItem -Path (Join-Path $RepoRoot "packages") -Recurse -Filter "nunit3-console.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { $runner = $found.FullName }
}
if (-not (Test-Path $runner)) {
    throw "NUnit console runner not found. Ensure packages restored (NUnit.ConsoleRunner)."
}
Write-Host "Using NUnit console: $runner"

# Test assembly path
$testDll = Join-Path $RepoRoot ("tests/ValheimPlus.Tests/bin/" + $Configuration + "/ValheimPlus.Tests.dll")
if (-not (Test-Path $testDll)) {
    throw "Test assembly not found at $testDll. Build may have failed."
}

# Run tests
& $runner $testDll --noresult --labels=All

Write-Host "Tests completed."
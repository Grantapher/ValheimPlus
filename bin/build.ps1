param(
    [string]$Configuration = "Release",
    [string]$ValheimInstall,
    [string]$PluginCopyDest,
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"
Write-Host "ValheimPlus build starting..." -ForegroundColor Green

# Resolve repo root
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

# Set VALHEIM_INSTALL if provided
if ($ValheimInstall) {
    $resolved = $ValheimInstall
    try { $resolved = (Resolve-Path -Path $ValheimInstall).Path } catch {}
    Write-Host "Setting VALHEIM_INSTALL to '$resolved'" -ForegroundColor Cyan
    $env:VALHEIM_INSTALL = $resolved
}

# Auto-detect repo-local valheim/server if env/param not provided
if (-not $env:VALHEIM_INSTALL) {
    $repoValheim = Join-Path $RepoRoot "valheim/server"
    if (Test-Path $repoValheim) {
        $resolvedRepoValheim = (Resolve-Path -Path $repoValheim).Path
        Write-Host "Auto-detected VALHEIM_INSTALL at '$resolvedRepoValheim'" -ForegroundColor Cyan
        $env:VALHEIM_INSTALL = $resolvedRepoValheim
    }
}

if (-not $env:VALHEIM_INSTALL) {
    Write-Error "ENV 'VALHEIM_INSTALL' is not set. Provide -ValheimInstall or set VALHEIM_INSTALL to your Valheim install so Unity/Valheim assemblies resolve."
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  -ValheimInstall 'C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server'" -ForegroundColor Yellow
    Write-Host "  -ValheimInstall 'C:\Program Files (x86)\Steam\steamapps\common\Valheim'" -ForegroundColor Yellow
    Write-Host "  Repo-local option (if present): '$repoValheim'" -ForegroundColor Yellow
    exit 1
}

# Preflight: validate expected Valheim/BepInEx layout
$serverData = Join-Path $env:VALHEIM_INSTALL "valheim_server_Data"
$clientData = Join-Path $env:VALHEIM_INSTALL "valheim_Data"
if (Test-Path (Join-Path $serverData "Managed")) {
    $ValheimDataDir = $serverData
}
elseif (Test-Path (Join-Path $clientData "Managed")) {
    $ValheimDataDir = $clientData
}
else {
    Write-Error "Could not find Managed folder. Checked: `n - $serverData\Managed `n - $clientData\Managed"
    Write-Host "Ensure VALHEIM_INSTALL points to a valid Valheim installation (client or dedicated server)." -ForegroundColor Yellow
    exit 1
}

$unityEngineDll = Join-Path (Join-Path $ValheimDataDir "Managed") "UnityEngine.dll"
if (-not (Test-Path $unityEngineDll)) {
    Write-Error "UnityEngine.dll not found at $unityEngineDll"
    Write-Host "Verify your Valheim install is complete and Managed assemblies exist." -ForegroundColor Yellow
    exit 1
}

$bepinexDll = Join-Path $env:VALHEIM_INSTALL "BepInEx/core/BepInEx.dll"
if (-not (Test-Path $bepinexDll)) {
    Write-Error "BepInEx.dll not found at $bepinexDll"
    Write-Host "Install BepInEx 5.x into your VALHEIM_INSTALL and ensure core is present (BepInEx\\core)." -ForegroundColor Yellow
    Write-Host "Download: https://github.com/BepInEx/BepInEx/releases (x64 for Windows)" -ForegroundColor Yellow
    exit 1
}

$publicizedValheim = Join-Path (Join-Path $ValheimDataDir "Managed/publicized_assemblies") "assembly_valheim_publicized.dll"
if (-not (Test-Path $publicizedValheim)) {
    Write-Warning "Publicized Valheim assembly not found at $publicizedValheim"
    Write-Host "Run the server/client once with BepInEx to generate publicized_assemblies, or install an Assembly Publicizer plugin." -ForegroundColor Yellow
    Write-Host "Fallback will likely fail to compile since the project references publicized types." -ForegroundColor Yellow
}

# NuGet restore (packages.config)
function Invoke-NuGetRestore {
    $solution = Join-Path $RepoRoot "ValheimPlus.sln"
    $nugetCmd = Get-Command nuget -ErrorAction SilentlyContinue
    if ($nugetCmd) {
        Write-Host "Using NuGet at: $($nugetCmd.Path)" -ForegroundColor Cyan
        & $nugetCmd.Path restore $solution | Write-Host
        return
    }

    # Fallback: download nuget.exe to bin/tools
    $toolsDir = Join-Path $RepoRoot "bin/tools"
    if (-not (Test-Path $toolsDir)) { New-Item -ItemType Directory -Path $toolsDir | Out-Null }
    $nugetExe = Join-Path $toolsDir "nuget.exe"
    if (-not (Test-Path $nugetExe)) {
        Write-Host "Downloading nuget.exe..." -ForegroundColor Cyan
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", $nugetExe)
    }
    Write-Host "Using NuGet at: $nugetExe" -ForegroundColor Cyan
    & $nugetExe restore $solution | Write-Host
}

if (-not $SkipRestore) {
    Invoke-NuGetRestore
}
else {
    Write-Host "Skipping NuGet restore as requested." -ForegroundColor Yellow
}

# Build ValheimPlus project
$projPath = Join-Path $RepoRoot "ValheimPlus/ValheimPlus.csproj"
Write-Host "Building $projPath ($Configuration)..." -ForegroundColor Green
$msbuildCmd = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuildCmd) {
    & $msbuildCmd.Path $projPath /t:Build /p:Configuration=$Configuration /m
}
else {
    $dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetCmd) {
        & $dotnetCmd msbuild $projPath /t:Build /p:Configuration=$Configuration /m
    }
    else {
        throw "Neither msbuild nor dotnet found. Please install one to build the project."
    }
}

# Verify output DLL
$pluginDll = Join-Path $RepoRoot ("ValheimPlus/bin/" + $Configuration + "/ValheimPlus.dll")
if (-not (Test-Path $pluginDll)) {
    throw "Build completed but DLL not found at $pluginDll"
}
Write-Host "Built plugin: $pluginDll" -ForegroundColor Green

# Optional copy to server plugins
if ($PluginCopyDest) {
    $destDir = Split-Path $PluginCopyDest -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir | Out-Null }
    Copy-Item $pluginDll -Destination $PluginCopyDest -Force
    Write-Host "Copied plugin to: $PluginCopyDest" -ForegroundColor Cyan
}

Write-Host "Build finished." -ForegroundColor Green

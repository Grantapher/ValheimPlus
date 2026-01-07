param(
    [string]$ValheimInstall,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
Write-Host "Setting up BepInEx Assembly Publicizer CLI and generating publicized assemblies..." -ForegroundColor Green

# Resolve repo root
$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot -and $PSCommandPath) { $ScriptRoot = Split-Path -Parent $PSCommandPath }
if (-not $ScriptRoot) { $ScriptRoot = (Get-Location).Path }
$RepoRoot = Resolve-Path (Join-Path $ScriptRoot "..")

# Set VALHEIM_INSTALL if provided
if ($ValheimInstall) {
    try { $env:VALHEIM_INSTALL = (Resolve-Path -Path $ValheimInstall).Path } catch { $env:VALHEIM_INSTALL = $ValheimInstall }
}

# Auto-detect repo-local valheim/server
if (-not $env:VALHEIM_INSTALL) {
    $repoValheim = Join-Path $RepoRoot "valheim/server"
    if (Test-Path $repoValheim) {
        $env:VALHEIM_INSTALL = (Resolve-Path -Path $repoValheim).Path
        Write-Host "Auto-detected VALHEIM_INSTALL at '$($env:VALHEIM_INSTALL)'" -ForegroundColor Cyan
    }
}

if (-not $env:VALHEIM_INSTALL) {
    throw "VALHEIM_INSTALL is not set. Provide -ValheimInstall or set the environment variable."
}

$valheimPath = $env:VALHEIM_INSTALL

# Detect Managed folder (prefer dedicated server layout)
$serverManaged = Join-Path $valheimPath "valheim_server_Data\Managed"
$clientManaged = Join-Path $valheimPath "valheim_Data\Managed"
if (Test-Path $serverManaged) { $managedDir = (Resolve-Path $serverManaged).Path }
elseif (Test-Path $clientManaged) { $managedDir = (Resolve-Path $clientManaged).Path }
else { throw "Managed folder not found under '$valheimPath'. Ensure Valheim install path is correct." }

$pubDir = Join-Path $managedDir "publicized_assemblies"
if (-not (Test-Path $pubDir)) { New-Item -ItemType Directory -Path $pubDir | Out-Null }

# Ensure dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI not found. Install .NET SDK (e.g., 'winget install Microsoft.DotNet.SDK.8') and retry."
}

# Install the publicizer CLI if missing or if Force is specified
$toolInstalled = $false
try {
    $toolList = dotnet tool list -g | Out-String
    if ($toolList -match "bepinex\.assemblypublicizer\.cli") { $toolInstalled = $true }
}
catch { }

if (-not $toolInstalled -or $Force) {
    Write-Host "Installing BepInEx.AssemblyPublicizer.Cli..." -ForegroundColor Cyan
    dotnet tool install -g BepInEx.AssemblyPublicizer.Cli | Out-Host
}
else {
    Write-Host "Assembly Publicizer CLI already installed." -ForegroundColor Yellow
}

# Function to publicize a single assembly if present
function Invoke-PublicizeAssembly {
    param(
        [Parameter(Mandatory = $true)][string]$Name
    )
    $inputPath = Join-Path $managedDir "$Name.dll"
    if (-not (Test-Path $inputPath)) {
        Write-Warning "Skipping: $Name.dll not found in Managed."
        return
    }
    $outputPath = Join-Path $pubDir "${Name}_publicized.dll"
    Write-Host "Publicizing $Name.dll -> $outputPath" -ForegroundColor Cyan
    assembly-publicizer $inputPath -o $outputPath -f | Out-Host
}

# Publicize required Valheim assemblies
Invoke-PublicizeAssembly -Name "assembly_valheim"
Invoke-PublicizeAssembly -Name "assembly_utils"
Invoke-PublicizeAssembly -Name "assembly_guiutils"

Write-Host "Done. Publicized assemblies are in: $pubDir" -ForegroundColor Green
Write-Host "Re-run build: .\\bin\\build.ps1 -Configuration Release" -ForegroundColor Green

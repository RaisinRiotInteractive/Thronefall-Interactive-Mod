# build_release.ps1 — builds and packages Thronefall Interactive Mod for Thunderstore
# Usage: .\build_release.ps1
# Output: ThronefallInteractive-<version>.zip in the project root

param(
    [string]$Version = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root      = $PSScriptRoot
$ModCsproj = Join-Path $Root "ThronefallInteractive.csproj"
$CfgCsproj = Join-Path $Root "Configurator\ThronefallInteractiveConfigurator.csproj"

# ── Resolve version ──────────────────────────────────────────────────────────
if (-not $Version) {
    $manifest = Get-Content (Join-Path $Root "manifest.json") | ConvertFrom-Json
    $Version  = $manifest.version_number
}
Write-Host "Packaging version $Version" -ForegroundColor Cyan

# ── Build mod (Release) ──────────────────────────────────────────────────────
Write-Host "`nBuilding mod..." -ForegroundColor Cyan
dotnet build $ModCsproj -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Write-Error "Mod build failed."; exit 1 }

# ── Publish configurator (self-contained, single-file) ───────────────────────
Write-Host "`nPublishing configurator..." -ForegroundColor Cyan
dotnet publish $CfgCsproj -c Release -r win-x64 --self-contained true --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Write-Error "Configurator publish failed."; exit 1 }

# ── Assemble staging folder ──────────────────────────────────────────────────
$Stage      = Join-Path $Root "_staging"
$PluginDir  = Join-Path $Stage "BepInEx\plugins\ThronefallInteractive"
$ConfigDir  = Join-Path $Stage "BepInEx\config"

if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
New-Item -ItemType Directory -Path $PluginDir | Out-Null
New-Item -ItemType Directory -Path $ConfigDir  | Out-Null

Write-Host "`nAssembling package..." -ForegroundColor Cyan

# Thunderstore root files
Copy-Item (Join-Path $Root "manifest.json") $Stage
Copy-Item (Join-Path $Root "README.md")     $Stage
Copy-Item (Join-Path $Root "icon.png")      $Stage

# Mod DLL + runtime dependencies (exclude .pdb, .deps.json — not needed at runtime)
$ModOut = Join-Path $Root "bin\Release\netstandard2.1"
$ExcludePatterns = @("*.pdb", "*.deps.json")
Get-ChildItem $ModOut -File | Where-Object {
    $f = $_.Name
    -not ($ExcludePatterns | Where-Object { $f -like $_ })
} | Copy-Item -Destination $PluginDir

# Pre-populated enemy list so the Configurator works on a fresh install
$SpawnsJson = Join-Path $Root "interactive_spawns.json"
if (-not (Test-Path $SpawnsJson)) {
    Write-Error "interactive_spawns.json not found. Add it to the project root."
    exit 1
}
Copy-Item $SpawnsJson $ConfigDir

# Default config so users don't need to launch the game first
$DefaultCfg = Join-Path $Root "com.raisinriotinteractive.thronefall.interactive.cfg"
if (-not (Test-Path $DefaultCfg)) {
    Write-Error "Default cfg not found. Add com.raisinriotinteractive.thronefall.interactive.cfg to the project root."
    exit 1
}
Copy-Item $DefaultCfg $ConfigDir

# Configurator single-file EXE
$CfgExe = Join-Path $Root "Configurator\bin\Release\net6.0-windows\win-x64\publish\ThronefallInteractiveConfigurator.exe"
if (-not (Test-Path $CfgExe)) {
    Write-Error "Configurator EXE not found at: $CfgExe"
    exit 1
}
Copy-Item $CfgExe $PluginDir

# ── Zip ──────────────────────────────────────────────────────────────────────
$ZipName = "Thronefall-Interactive-Mod-$Version.zip"
$ZipPath = Join-Path $Root $ZipName

if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($Stage, $ZipPath)

Remove-Item $Stage -Recurse -Force

$Size = [math]::Round((Get-Item $ZipPath).Length / 1MB, 2)
Write-Host "`nDone: $ZipName ($Size MB)" -ForegroundColor Green
Write-Host "Contents:" -ForegroundColor Green

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
$zip.Entries | ForEach-Object { Write-Host "  $($_.FullName)" }
$zip.Dispose()

# publish_release.ps1 — creates a GitHub release and uploads the zip asset
# Usage: .\publish_release.ps1
# Reads version from manifest.json. Expects the zip to already exist (run build_release.ps1 first).
# Prompts for a GitHub PAT with repo scope if not set in $env:GITHUB_TOKEN.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root  = $PSScriptRoot
$Owner = "RaisinRiotInteractive"
$Repo  = "Thronefall-Interactive-Mod"

# ── Version + zip ────────────────────────────────────────────────────────────
$manifest = Get-Content (Join-Path $Root "manifest.json") | ConvertFrom-Json
$Version  = $manifest.version_number
$Tag      = "v$Version"
$ZipPath  = Join-Path $Root "Thronefall-Interactive-Mod-$Version.zip"

if (-not (Test-Path $ZipPath)) {
    Write-Error "Zip not found: $ZipPath`nRun .\build_release.ps1 first."
    exit 1
}

# ── Auth token ───────────────────────────────────────────────────────────────
$Token = $env:GITHUB_TOKEN
if (-not $Token) {
    $Token = (Read-Host "GitHub Personal Access Token (repo scope)").Trim()
}
if (-not $Token) { Write-Error "No token provided."; exit 1 }

$Headers = @{
    Authorization = "Bearer $Token"
    Accept        = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

# ── Release notes from CHANGELOG ─────────────────────────────────────────────
$ChangelogPath = Join-Path $Root "CHANGELOG.md"
$ReleaseNotes  = ""
if (Test-Path $ChangelogPath) {
    $lines   = Get-Content $ChangelogPath
    $capture = $false
    $buf     = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $lines) {
        if ($line -match "^## \[$Version\]") { $capture = $true; continue }
        elseif ($capture -and $line -match "^## \[")  { break }
        elseif ($capture) { $buf.Add($line) }
    }
    $ReleaseNotes = ($buf | Where-Object { $_ -ne "" } | Out-String).Trim()
}
if (-not $ReleaseNotes) { $ReleaseNotes = "Release $Tag" }

# ── Create release ───────────────────────────────────────────────────────────
Write-Host "`nCreating release $Tag..." -ForegroundColor Cyan

$Body = @{
    tag_name         = $Tag
    target_commitish = "main"
    name             = "Thronefall Interactive Mod $Tag"
    body             = $ReleaseNotes
    draft            = $false
    prerelease       = $false
} | ConvertTo-Json

try {
    $Release = Invoke-RestMethod `
        -Uri "https://api.github.com/repos/$Owner/$Repo/releases" `
        -Method Post `
        -Headers $Headers `
        -Body $Body `
        -ContentType "application/json"
} catch {
    $msg = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
    Write-Error "Failed to create release: $($msg.message ?? $_.Exception.Message)"
    exit 1
}

Write-Host "Release created: $($Release.html_url)" -ForegroundColor Green

# ── Upload zip asset ─────────────────────────────────────────────────────────
$UploadUrl = $Release.upload_url -replace '\{.*\}', ''
$ZipName   = Split-Path $ZipPath -Leaf
$ZipBytes  = [System.IO.File]::ReadAllBytes($ZipPath)

Write-Host "Uploading $ZipName ($([math]::Round($ZipBytes.Length/1MB,1)) MB)..." -ForegroundColor Cyan

try {
    $Asset = Invoke-RestMethod `
        -Uri "${UploadUrl}?name=$ZipName&label=$ZipName" `
        -Method Post `
        -Headers $Headers `
        -Body $ZipBytes `
        -ContentType "application/zip"
} catch {
    $msg = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
    Write-Error "Upload failed: $($msg.message ?? $_.Exception.Message)"
    exit 1
}

Write-Host "Asset uploaded: $($Asset.browser_download_url)" -ForegroundColor Green
Write-Host "`nDone! Release page: $($Release.html_url)" -ForegroundColor Green

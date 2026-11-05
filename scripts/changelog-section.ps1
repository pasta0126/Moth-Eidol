<#
.SYNOPSIS
  Prints the CHANGELOG.md entry of one version as plain text, for the Microsoft Store
  "What's new in this version" field (Partner Center).

.EXAMPLE
  powershell -File scripts\changelog-section.ps1 -Version 1.7.0
  powershell -File scripts\changelog-section.ps1            # uses the version of Directory.Build.props

.NOTES
  Fails (non-zero exit) when the version has no entry or the text is longer than -Limit characters.
#>
param(
    [string]$Version,
    [int]$Limit = 1500,
    [string]$Changelog
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $Changelog) { $Changelog = Join-Path $root 'CHANGELOG.md' }

if (-not $Version) {
    $props = Get-Content (Join-Path $root 'Directory.Build.props') -Raw
    if ($props -match '<Version>([^<]+)</Version>') { $Version = $Matches[1] }
    else { Write-Error 'Could not read the version from Directory.Build.props.'; exit 1 }
}

if (-not (Test-Path $Changelog)) { Write-Error "CHANGELOG not found: $Changelog"; exit 1 }

$lines = Get-Content $Changelog
$escaped = [regex]::Escape($Version)
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "^##\s+\[?$escaped\]?(\s|$)") { $start = $i; break }
}
if ($start -lt 0) { Write-Error "No changelog entry for version $Version."; exit 1 }

$out = New-Object System.Collections.Generic.List[string]
for ($i = $start + 1; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match '^##\s') { break }                         # next version
    if ($line -match '^###\s+(.+)$') { $out.Add($Matches[1] + ':'); continue }   # Added / Changed / Fixed
    $out.Add(($line -replace '\[([^\]]+)\]\([^)]*\)', '$1' -replace '`', ''))     # links and code ticks
}

$text = (($out -join "`n") -replace "(\n){3,}", "`n`n").Trim()
if (-not $text) { Write-Error "The entry for version $Version is empty."; exit 1 }
if ($text.Length -gt $Limit) {
    Write-Error "The entry for version $Version has $($text.Length) characters; the Store limit is $Limit."
    exit 1
}

Write-Output $text

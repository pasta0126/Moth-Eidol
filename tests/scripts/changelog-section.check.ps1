# Checks scripts/changelog-section.ps1: existing version, missing version, entry too long.
# Run: pwsh -File tests/scripts/changelog-section.check.ps1   (exit code 0 = all good)
$ErrorActionPreference = 'Continue'
$script = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'scripts/changelog-section.ps1'
$failures = 0

function Check($name, $ok) {
    if ($ok) { Write-Host "ok   $name" } else { Write-Host "FAIL $name"; $script:failures++ }
}

$text = & pwsh -NoProfile -File $script -Version 1.7.0 2>$null
Check 'existing version prints its entry' (($LASTEXITCODE -eq 0) -and ($text -match 'Added:') -and ($text -notmatch '###|\]\(|`'))

& pwsh -NoProfile -File $script -Version 9.9.9 *> $null
Check 'missing version fails' ($LASTEXITCODE -ne 0)

& pwsh -NoProfile -File $script -Version 1.7.0 -Limit 50 *> $null
Check 'entry over the limit fails' ($LASTEXITCODE -ne 0)

& pwsh -NoProfile -File $script -Version 1.7.0 -Limit 5000 *> $null
Check 'entry within the limit passes' ($LASTEXITCODE -eq 0)

# Every version listed in the changelog yields a Store text within the limit.
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
foreach ($line in (Get-Content (Join-Path $root 'CHANGELOG.md'))) {
    if ($line -match '^##\s+\[(\d+\.\d+\.\d+)\]') {
        $v = $Matches[1]
        & pwsh -NoProfile -File $script -Version $v *> $null
        Check "changelog entry for $v fits the Store limit" ($LASTEXITCODE -eq 0)
    }
}

exit $failures

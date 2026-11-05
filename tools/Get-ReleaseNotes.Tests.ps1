<#
.SYNOPSIS
    Proves that Get-ReleaseNotes.ps1 writes a version's section and fails without one (spec 009,
    AC-009-02 and AC-009-03), using temporary changelogs.
#>

$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'Get-ReleaseNotes.ps1'
$failures = 0

function Assert-True([bool] $condition, [string] $message) {
    Write-Output ("{0}  {1}" -f $(if ($condition) { 'PASS' } else { 'FAIL' }), $message)
    if (-not $condition) { $script:failures++ }
}

$changelog = @'
# Changelog

## [Unreleased]

## [1.2.3] - 2026-11-05

### Added

- Battery in the tray.

## [1.2.2] - 2026-11-01

### Fixed

- Older entry.

## [1.2.1] - 2026-10-31

[Unreleased]: https://github.com/jordanzdev/CloudStatus/compare/v1.2.3...HEAD
'@

$temp = Join-Path ([IO.Path]::GetTempPath()) ("CloudStatus-notes-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $changelogPath = Join-Path $temp 'CHANGELOG.md'
    [IO.File]::WriteAllText($changelogPath, $changelog.Replace("`r`n", "`n"))
    $notes = Join-Path $temp 'notes.md'

    & $script -Version 1.2.3 -OutFile $notes -Changelog $changelogPath | Out-Null
    $text = [IO.File]::ReadAllText($notes)
    Assert-True ($text.StartsWith("### Added`n`n- Battery in the tray.")) 'notes start with the version section'
    Assert-True (-not $text.Contains('Older entry')) 'notes stop at the next version'
    Assert-True ($text.Contains('CloudStatus-1.2.3.msi') -and $text.Contains('CloudStatus-1.2.3-portable.exe') -and $text.Contains('SHA256SUMS.txt')) 'notes have the downloads table'
    Assert-True (-not $text.Contains("`r")) 'notes use LF'

    foreach ($case in @(@('9.9.9', 'missing'), @('1.2.1', 'empty'))) {
        $failed = $false
        try { & $script -Version $case[0] -OutFile $notes -Changelog $changelogPath | Out-Null } catch { $failed = $true }
        Assert-True $failed "fails when the section is $($case[1])"
    }
}
finally {
    Remove-Item $temp -Recurse -Force
}

if ($failures -gt 0) {
    Write-Output "$failures check(s) failed."
    exit 1
}
Write-Output 'Release notes checks behave as expected.'

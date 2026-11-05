<#
.SYNOPSIS
    Writes the GitHub Release notes of a version: its CHANGELOG.md section plus a downloads table.

.DESCRIPTION
    Fails when CHANGELOG.md has no section for the version, so no release goes out without notes.

.EXAMPLE
    .\tools\Get-ReleaseNotes.ps1 -Version 1.2.3 -OutFile artifacts\RELEASE_NOTES.md
#>
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [Parameter(Mandatory)]
    [string] $OutFile,

    [string] $Changelog
)

$ErrorActionPreference = 'Stop'
if (-not $Changelog) {
    # Resolved here: Windows PowerShell leaves $PSScriptRoot empty in parameter defaults.
    $Changelog = Join-Path (Split-Path $PSScriptRoot -Parent) 'CHANGELOG.md'
}
$lines = [IO.File]::ReadAllLines($Changelog)

$start = [Array]::FindIndex($lines, [Predicate[string]] { param($line) $line -match "^## \[$([regex]::Escape($Version))\]" })
if ($start -lt 0) {
    throw "CHANGELOG.md has no section for $Version. Add '## [$Version] - YYYY-MM-DD' (move the [Unreleased] entries there) before releasing."
}

# The section ends at the next version heading or at the link references at the bottom.
$end = $start + 1
while ($end -lt $lines.Length -and $lines[$end] -notmatch '^## \[' -and $lines[$end] -notmatch '^\[[^\]]+\]: ') {
    $end++
}

$section = ($lines[($start + 1)..($end - 1)] -join "`n").Trim()
if (-not $section) {
    throw "The CHANGELOG.md section for $Version is empty."
}

$downloads = @"
### Downloads

| File | |
|---|---|
| ``CloudStatus-$Version.msi`` | Installer, in English or Spanish (chosen on its first page). No administrator rights needed. |
| ``CloudStatus-$Version-portable.exe`` | The same app without installing it. |
| ``SHA256SUMS.txt`` | SHA-256 checksums: ``sha256sum -c SHA256SUMS.txt`` |
"@

New-Item -ItemType Directory -Force (Split-Path $OutFile -Parent) | Out-Null
[IO.File]::WriteAllText($OutFile, "$section`n`n$($downloads.Replace("`r`n", "`n"))`n", (New-Object Text.UTF8Encoding $false))
Write-Host "Release notes for $Version written to $OutFile"

<#
.SYNOPSIS
    Checks the files build.ps1 produced: size budget, native exe, manifest and hashes.

.EXAMPLE
    .\tools\Test-Artifacts.ps1 -Version 1.2.3
#>
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [long] $MaxExeBytes = 1572864 # 1.5 MB, docs/PERFORMANCE.md
)

$ErrorActionPreference = 'Stop'
$artifacts = Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts'
$exeName = "CloudStatus-$Version-portable.exe"
$exe = Join-Path $artifacts $exeName
$failures = 0

function Assert-True([bool] $condition, [string] $message) {
    Write-Output ("{0}  {1}" -f $(if ($condition) { 'PASS' } else { 'FAIL' }), $message)
    if (-not $condition) { $script:failures++ }
}

$bytes = [IO.File]::ReadAllBytes($exe)
Assert-True ($bytes.Length -le $MaxExeBytes) "exe size $($bytes.Length) bytes, budget $MaxExeBytes"

# A managed exe has a CLR header: data directory 14 of the PE optional header (PE32+).
$peHeader = [BitConverter]::ToInt32($bytes, 0x3C)
$clrDirectory = $peHeader + 24 + 112 + 14 * 8
Assert-True ([BitConverter]::ToInt32($bytes, $clrDirectory) -eq 0) 'exe is native: no CLR header, no .NET runtime needed'

$text = [Text.Encoding]::ASCII.GetString($bytes)
Assert-True ($text.Contains('level="asInvoker"')) 'embedded manifest requests asInvoker'

$checksums = [IO.File]::ReadAllBytes((Join-Path $artifacts 'SHA256SUMS.txt'))
Assert-True ($checksums -notcontains 13) 'SHA256SUMS.txt has no CR characters'

$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$content = [Text.Encoding]::ASCII.GetString($checksums)
Assert-True ($content.Contains("$hash  $exeName`n")) 'SHA256SUMS.txt matches the exe'

if ($failures -gt 0) {
    Write-Output "$failures check(s) failed."
    exit 1
}
exit 0

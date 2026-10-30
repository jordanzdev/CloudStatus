<#
.SYNOPSIS
    Builds the native CloudStatus.exe into .\artifacts.

.EXAMPLE
    .\build.ps1 -Version 1.2.3
#>
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$publish = Join-Path $artifacts 'publish'

# Visual Studio 2026 (v18) vcvarsall.bat calls vswhere.exe without a full path.
$vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if (Test-Path $vsInstaller) {
    $env:PATH = "$vsInstaller;$env:PATH"
}

function Invoke-Checked([scriptblock] $Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code $LASTEXITCODE" }
}

Write-Host "Publishing CloudStatus $Version (NativeAOT)..."
Invoke-Checked { dotnet publish (Join-Path $root 'CloudStatus\CloudStatus.csproj') -c Release -p:Version=$Version -o $publish }

$exe = Join-Path $publish 'CloudStatus.exe'
Copy-Item $exe (Join-Path $artifacts "CloudStatus-$Version-portable.exe") -Force

# "hash  name" lines with LF endings, so `sha256sum -c SHA256SUMS.txt` works on any platform.
$checksums = Get-ChildItem $artifacts -Filter "CloudStatus-$Version*" -File |
    Get-FileHash -Algorithm SHA256 |
    ForEach-Object { "$($_.Hash.ToLowerInvariant())  $(Split-Path $_.Path -Leaf)" }
[IO.File]::WriteAllText((Join-Path $artifacts 'SHA256SUMS.txt'), (($checksums -join "`n") + "`n"))

Write-Host "Done:"
Get-ChildItem $artifacts -File | ForEach-Object { "  $($_.Name)" }

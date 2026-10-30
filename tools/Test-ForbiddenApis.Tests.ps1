<#
.SYNOPSIS
    Proves that Test-ForbiddenApis.ps1 catches violations, using temporary copies of the source.
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$checker = Join-Path $PSScriptRoot 'Test-ForbiddenApis.ps1'
$failures = 0

function Assert-Check([string] $name, [string] $file, [string] $line, [bool] $expectFailure) {
    $temp = Join-Path ([IO.Path]::GetTempPath()) ("CloudStatus-policy-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temp | Out-Null
    try {
        $source = Join-Path $root 'CloudStatus'
        Get-ChildItem $source -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
            $target = Join-Path $temp ('CloudStatus' + $_.FullName.Substring($source.Length))
            New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
            Copy-Item $_.FullName $target
        }

        if ($file) {
            $path = Join-Path $temp $file
            New-Item -ItemType Directory -Force (Split-Path $path -Parent) | Out-Null
            [IO.File]::WriteAllText($path, "// first line`n$line`n")
        }

        $output = (& $checker -Root $temp) -join "`n"
        $exitCode = $LASTEXITCODE
        $named = (-not $file) -or ($output -match [regex]::Escape("file=$($file.Replace('\', '/')),line=2"))
        $passed = if ($expectFailure) { $exitCode -ne 0 -and $named } else { $exitCode -eq 0 }
        Write-Output ("{0}  {1}" -f $(if ($passed) { 'PASS' } else { 'FAIL' }), $name)
        if (-not $passed) {
            Write-Output $output
            $script:failures++
        }
    }
    finally {
        Remove-Item $temp -Recurse -Force
    }
}

$bad = 'CloudStatus/Ui/Injected.cs'

Assert-Check 'clean source passes' $null $null $false
Assert-Check 'hook' $bad 'SetWindowsHookEx(0);' $true
Assert-Check 'keyboard polling' $bad 'GetAsyncKeyState(0);' $true
Assert-Check 'raw input' $bad 'RegisterRawInputDevices(0);' $true
Assert-Check 'input injection' $bad 'SendInput(0);' $true
Assert-Check 'other process' $bad 'OpenProcess(0);' $true
Assert-Check 'process enumeration' $bad 'CreateToolhelp32Snapshot(0);' $true
Assert-Check 'blocking HID write' $bad 'HidD_SetFeature(0);' $true
Assert-Check 'threading' $bad 'Task.Run(Work);' $true
Assert-Check 'audio' $bad 'IMMDevice device;' $true
Assert-Check 'graphics' $bad 'dxgi.Present();' $true
Assert-Check 'reflection' $bad 'using System.Reflection;' $true
Assert-Check 'network' $bad 'new HttpClient();' $true
Assert-Check 'NuGet package' 'CloudStatus/Injected.csproj' '<PackageReference Include="X" />' $true
Assert-Check 'WriteFile outside the transport' $bad 'WriteFile(0);' $true
Assert-Check 'WriteFile in the transport is allowed' 'CloudStatus/Device/HidTransport.cs' 'WriteFile(0);' $false
Assert-Check 'P/Invoke outside Native' $bad '[LibraryImport("x.dll")]' $true
Assert-Check 'P/Invoke in Native is allowed' 'CloudStatus/Native/Hid.cs' '[LibraryImport("x.dll")]' $false
Assert-Check 'WinHTTP outside Updates' $bad 'WinHttpOpen(0);' $true

if ($failures -gt 0) {
    Write-Output "$failures check(s) failed."
    exit 1
}
Write-Output 'All policy checks behave as expected.'
exit 0

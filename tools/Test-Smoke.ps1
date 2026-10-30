<#
.SYNOPSIS
    Runs the real exe: a second launch exits silently and WM_CLOSE ends the app with code 0.

.EXAMPLE
    .\tools\Test-Smoke.ps1 -Exe artifacts\CloudStatus-1.2.3-portable.exe
#>
param(
    [Parameter(Mandatory)]
    [string] $Exe
)

$ErrorActionPreference = 'Stop'
$timeoutMs = 10000
$failures = 0

Add-Type -Namespace CloudStatusSmoke -Name Win32 -MemberDefinition @'
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
public static extern IntPtr FindWindowW(string className, string windowName);
[DllImport("user32.dll")]
public static extern bool PostMessageW(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
'@

function Assert-True([bool] $condition, [string] $message) {
    Write-Output ("{0}  {1}" -f $(if ($condition) { 'PASS' } else { 'FAIL' }), $message)
    if (-not $condition) { $script:failures++ }
}

$exe = (Resolve-Path $Exe).Path
$first = Start-Process $exe -PassThru
$null = $first.Handle # needed for ExitCode after the process ends

try {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $window = [IntPtr]::Zero
    while ($window -eq [IntPtr]::Zero -and $watch.ElapsedMilliseconds -lt $timeoutMs) {
        # Not $null: PowerShell would pass an empty string, which only matches an empty title.
        $window = [CloudStatusSmoke.Win32]::FindWindowW('CloudStatusHidden', [NullString]::Value)
    }
    Assert-True ($window -ne [IntPtr]::Zero) "hidden window created after $($watch.ElapsedMilliseconds) ms"

    $second = Start-Process $exe -PassThru
    $null = $second.Handle
    Assert-True $second.WaitForExit($timeoutMs) 'second instance exits by itself'
    Assert-True ($second.ExitCode -eq 0) "second instance exit code is $($second.ExitCode)"
    Assert-True (-not $first.HasExited) 'first instance keeps running'

    [void] [CloudStatusSmoke.Win32]::PostMessageW($window, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) # WM_CLOSE
    Assert-True $first.WaitForExit($timeoutMs) 'WM_CLOSE ends the app'
    Assert-True ($first.ExitCode -eq 0) "exit code after WM_CLOSE is $($first.ExitCode)"
}
finally {
    if (-not $first.HasExited) { $first.Kill() }
}

if ($failures -gt 0) {
    Write-Output "$failures check(s) failed."
    exit 1
}
exit 0

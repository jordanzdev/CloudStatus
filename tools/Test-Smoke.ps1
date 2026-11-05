<#
.SYNOPSIS
    Runs the real exe: started by hand it shows Settings, with --startup it stays in the tray, a
    second launch exits silently and WM_CLOSE ends the app with code 0.

.EXAMPLE
    .\tools\Test-Smoke.ps1 -Exe artifacts\CloudStatus-1.2.3-portable.exe
#>
param(
    [Parameter(Mandatory)]
    [string] $Exe
)

$ErrorActionPreference = 'Stop'
$timeoutMs = 10000
$settleMs = 1500
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

# Not $null: PowerShell would pass an empty string, which only matches an empty title.
function Find-Window([string] $className) {
    [CloudStatusSmoke.Win32]::FindWindowW($className, [NullString]::Value)
}

function Wait-Window([string] $className) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $window = [IntPtr]::Zero
    while ($window -eq [IntPtr]::Zero -and $watch.ElapsedMilliseconds -lt $timeoutMs) {
        $window = Find-Window $className
    }
    [pscustomobject]@{ Handle = $window; Ms = $watch.ElapsedMilliseconds }
}

function Start-App([string[]] $arguments) {
    $process = if ($arguments) { Start-Process $exe -ArgumentList $arguments -PassThru } else { Start-Process $exe -PassThru }
    $null = $process.Handle # needed for ExitCode after the process ends
    $process
}

function Stop-App($process, $window, [string] $label) {
    [void] [CloudStatusSmoke.Win32]::PostMessageW($window, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) # WM_CLOSE
    Assert-True $process.WaitForExit($timeoutMs) "$label WM_CLOSE ends the app"
    Assert-True ($process.ExitCode -eq 0) "$label exit code after WM_CLOSE is $($process.ExitCode)"
    Assert-True ((Find-Window 'CloudStatusSettings') -eq [IntPtr]::Zero) "$label no settings window left"
}

$exe = (Resolve-Path $Exe).Path

# Started by hand: Settings appears.
$first = Start-App @()
try {
    $hidden = Wait-Window 'CloudStatusHidden'
    Assert-True ($hidden.Handle -ne [IntPtr]::Zero) "hidden window created after $($hidden.Ms) ms"
    $settings = Wait-Window 'CloudStatusSettings'
    Assert-True ($settings.Handle -ne [IntPtr]::Zero) 'started by hand: settings window shown'

    $second = Start-App @()
    Assert-True $second.WaitForExit($timeoutMs) 'second instance exits by itself'
    Assert-True ($second.ExitCode -eq 0) "second instance exit code is $($second.ExitCode)"
    Assert-True (-not $first.HasExited) 'first instance keeps running'

    Stop-App $first $hidden.Handle 'by hand:'
}
finally {
    if (-not $first.HasExited) { $first.Kill() }
}

# Started at sign-in: stays in the tray.
$startup = Start-App @('--startup')
try {
    $hidden = Wait-Window 'CloudStatusHidden'
    Assert-True ($hidden.Handle -ne [IntPtr]::Zero) "--startup: hidden window created after $($hidden.Ms) ms"
    Start-Sleep -Milliseconds $settleMs
    Assert-True ((Find-Window 'CloudStatusSettings') -eq [IntPtr]::Zero) '--startup: no settings window'

    Stop-App $startup $hidden.Handle '--startup:'
}
finally {
    if (-not $startup.HasExited) { $startup.Kill() }
}

if ($failures -gt 0) {
    Write-Output "$failures check(s) failed."
    exit 1
}
exit 0

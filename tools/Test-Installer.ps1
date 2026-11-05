<#
.SYNOPSIS
    Installs, reinstalls and uninstalls the MSI silently and checks that nothing is left behind
    (spec 009, AC-009-05 to AC-009-07).

.DESCRIPTION
    It installs into the current user's profile and deletes HKCU\Software\CloudStatus, so it only
    runs on a GitHub Actions runner. Never run it on a machine where CloudStatus is in use.

.EXAMPLE
    .\tools\Test-Installer.ps1 -Msi artifacts\CloudStatus-0.0.0.msi
#>
param(
    [Parameter(Mandatory)]
    [string] $Msi
)

$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') {
    throw 'Test-Installer.ps1 changes the current user profile; it only runs on GitHub Actions.'
}

$timeoutMs = 30000
$failures = 0
$msiPath = (Resolve-Path $Msi).Path
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\CloudStatus'
$exe = Join-Path $installDir 'CloudStatus.exe'
$startMenuLink = Join-Path ([Environment]::GetFolderPath('Programs')) 'CloudStatus.lnk'
$desktopLink = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'CloudStatus.lnk'
$settingsKey = 'HKCU:\Software\CloudStatus'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
# Some runner images have no Run key yet; the app creates it with RegCreateKeyExW, the test must too.
if (-not (Test-Path $runKey)) { New-Item $runKey -Force | Out-Null }

Add-Type -Namespace CloudStatusInstaller -Name Win32 -MemberDefinition @'
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
public static extern IntPtr FindWindowW(string className, string windowName);
'@

function Assert-True([bool] $condition, [string] $message) {
    Write-Output ("{0}  {1}" -f $(if ($condition) { 'PASS' } else { 'FAIL' }), $message)
    if (-not $condition) { $script:failures++ }
}

function Invoke-Msiexec([string] $arguments) {
    $process = Start-Process msiexec.exe -ArgumentList $arguments -Wait -PassThru
    $process.ExitCode
}

function Get-ArpEntries {
    foreach ($root in 'HKCU:', 'HKLM:') {
        $path = "$root\Software\Microsoft\Windows\CurrentVersion\Uninstall"
        if (Test-Path $path) {
            Get-ChildItem $path | Get-ItemProperty | Where-Object { $_.DisplayName -eq 'CloudStatus' }
        }
    }
}

# Runs one argument of the installed exe: it must exit by itself, with code 0, showing no window.
function Invoke-Argument([string] $argument) {
    $process = Start-Process $exe -ArgumentList $argument -PassThru
    $null = $process.Handle
    $sawWindow = $false
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while (-not $process.HasExited -and $watch.ElapsedMilliseconds -lt $timeoutMs) {
        if ([CloudStatusInstaller.Win32]::FindWindowW('CloudStatusHidden', [NullString]::Value) -ne [IntPtr]::Zero) { $sawWindow = $true }
        Start-Sleep -Milliseconds 20
    }
    Assert-True $process.HasExited "$argument exits by itself"
    if (-not $process.HasExited) { $process.Kill() }
    Assert-True ($process.ExitCode -eq 0) "$argument exit code is $($process.ExitCode)"
    Assert-True (-not $sawWindow) "$argument starts no app window"
}

# Install
Assert-True ((Invoke-Msiexec "/i `"$msiPath`" /qn") -eq 0) 'silent install succeeds'
Assert-True (Test-Path $exe) 'exe installed under %LOCALAPPDATA%\Programs\CloudStatus'
Assert-True (Test-Path $startMenuLink) 'Start menu shortcut created'
$installed = (Get-ItemProperty "$settingsKey\Install" -ErrorAction SilentlyContinue).Executable
Assert-True ($installed -eq $exe) 'Install\Executable records the installed exe'
Assert-True ($null -eq (Get-ItemProperty $settingsKey -Name Language -ErrorAction SilentlyContinue)) 'silent install keeps the language setting'

# Reinstall the same version: one entry in Apps & features (AC-009-06)
Assert-True ((Invoke-Msiexec "/i `"$msiPath`" /qn") -eq 0) 'same-version reinstall succeeds'
Assert-True (@(Get-ArpEntries).Count -eq 1) "one Apps & features entry (found $(@(Get-ArpEntries).Count))"

# Arguments (AC-009-07)
Invoke-Argument '--create-desktop-shortcut'
Assert-True (Test-Path $desktopLink) 'desktop shortcut created'

# What the app writes and the MSI does not know about: settings, Start with Windows
New-ItemProperty $settingsKey -Name LowBatteryPercent -Value 30 -PropertyType DWord -Force | Out-Null
New-ItemProperty $runKey -Name CloudStatus -Value "`"$exe`" --startup" -PropertyType String -Force | Out-Null

Invoke-Argument '--uninstall'
Assert-True (-not (Test-Path $desktopLink)) '--uninstall removes the desktop shortcut'
Assert-True ($null -eq (Get-ItemProperty $runKey -Name CloudStatus -ErrorAction SilentlyContinue)) '--uninstall removes the Run entry'
Assert-True (-not (Test-Path $settingsKey)) '--uninstall removes HKCU\Software\CloudStatus'

# Uninstall through the MSI, after writing everything again
New-Item $settingsKey -Force | Out-Null
New-ItemProperty $settingsKey -Name LowBatteryPercent -Value 30 -PropertyType DWord -Force | Out-Null
New-ItemProperty $runKey -Name CloudStatus -Value "`"$exe`" --startup" -PropertyType String -Force | Out-Null
Invoke-Argument '--create-desktop-shortcut'

Assert-True ((Invoke-Msiexec "/x `"$msiPath`" /qn") -eq 0) 'silent uninstall succeeds'
Assert-True (-not (Test-Path $installDir)) 'install folder removed'
Assert-True (-not (Test-Path $startMenuLink)) 'Start menu shortcut removed'
Assert-True (-not (Test-Path $desktopLink)) 'desktop shortcut removed'
Assert-True ($null -eq (Get-ItemProperty $runKey -Name CloudStatus -ErrorAction SilentlyContinue)) 'Run entry removed'
Assert-True (-not (Test-Path $settingsKey)) 'HKCU\Software\CloudStatus removed'
Assert-True (@(Get-ArpEntries).Count -eq 0) 'no Apps & features entry left'

if ($failures -gt 0) {
    Write-Output "$failures check(s) failed."
    exit 1
}
exit 0

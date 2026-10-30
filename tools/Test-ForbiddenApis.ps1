<#
.SYNOPSIS
    Fails if the source uses an API that CloudStatus promises never to use (ARCHITECTURE.md, D17).

.EXAMPLE
    .\tools\Test-ForbiddenApis.ps1
#>
param(
    [string] $Root
)

$ErrorActionPreference = 'Stop'
if (-not $Root) {
    # Resolved here: Windows PowerShell leaves $PSScriptRoot empty in parameter defaults.
    $Root = Split-Path $PSScriptRoot -Parent
}

# Forbidden in every file, by category.
$forbidden = [ordered]@{
    'hook'             = 'SetWindowsHookEx|SetWinEventHook'
    'keyboard polling' = 'GetAsyncKeyState|\bGetKeyState\b'
    'raw input'        = 'RegisterRawInputDevices|GetRawInputData'
    'input injection'  = 'SendInput|keybd_event|mouse_event'
    'other process'    = 'OpenProcess|ReadProcessMemory|WriteProcessMemory|CreateRemoteThread|CreateToolhelp32Snapshot|Process32(First|Next)|Module32(First|Next)|EnumProcesses|EnumProcessModules|System\.Diagnostics\.Process'
    'HID write'        = 'HidD_SetOutputReport|HidD_SetFeature|\bDeviceIoControl\b'
    'HID read'         = 'HidD_Get\w*String'
    'threading'        = '\bTask\b|\bValueTask\b|\basync\b|\bawait\b|ThreadPool|System\.Threading\.Timer|\bnew Thread\b|\bCreateThread\b|\bParallel\.'
    'audio'            = 'IMMDevice|MMDeviceEnumerator|IAudioClient|IAudioEndpoint|\bwaveOut|mmdevapi'
    'graphics'         = '\bd3d\d*|\bdxgi\b|opengl32|Direct3D'
    'reflection'       = 'System\.Reflection|Activator\.CreateInstance'
    'network'          = 'System\.Net\b|\bHttpClient\b|\bTcpClient\b|\bSocket\b'
    'NuGet package'    = '<PackageReference\b'
}

# Allowed only in the files that match the second value (paths relative to the root).
$restricted = @(
    @{ Name = 'HID write'; Pattern = '\bWriteFile\b'; Allowed = '^CloudStatus/(Native/Kernel32\.cs|Device/HidTransport\.cs)$' }
    @{ Name = 'HID read'; Pattern = '\bReadFile\b|\bGENERIC_READ\b|\bGENERIC_WRITE\b'; Allowed = '^CloudStatus/(Native/Kernel32\.cs|Device/HidTransport\.cs)$' }
    @{ Name = 'P/Invoke'; Pattern = '\[(LibraryImport|DllImport)\b'; Allowed = '^CloudStatus/Native/' }
    @{ Name = 'device access'; Pattern = '\bCreateFileW\b'; Allowed = '^CloudStatus/(Native/Kernel32\.cs|Device/)' }
    @{ Name = 'network'; Pattern = '\bWinHttp\w*'; Allowed = '^CloudStatus/(Native/WinHttp\.cs|Updates/)' }
)

$sources = foreach ($folder in 'CloudStatus', 'installer') {
    $path = Join-Path $Root $folder
    if (Test-Path $path) {
        Get-ChildItem $path -Recurse -File -Include *.cs, *.csproj, *.manifest, *.wxs |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    }
}

$violations = 0

function Report($relativePath, $line, $category, $text) {
    Write-Output "::error file=$relativePath,line=$line::Forbidden ${category}: $text"
    $script:violations++
}

foreach ($file in $sources) {
    $relativePath = $file.FullName.Substring($Root.TrimEnd('\', '/').Length + 1).Replace('\', '/')

    foreach ($category in $forbidden.Keys) {
        foreach ($hit in Select-String -LiteralPath $file.FullName -Pattern $forbidden[$category]) {
            Report $relativePath $hit.LineNumber $category $hit.Matches[0].Value
        }
    }

    foreach ($rule in $restricted) {
        if ($relativePath -match $rule.Allowed) { continue }
        foreach ($hit in Select-String -LiteralPath $file.FullName -Pattern $rule.Pattern) {
            Report $relativePath $hit.LineNumber $rule.Name $hit.Matches[0].Value
        }
    }
}

if ($violations -gt 0) {
    Write-Output "$violations violation(s) found."
    exit 1
}
Write-Output "No forbidden APIs found in $(@($sources).Count) files."
exit 0

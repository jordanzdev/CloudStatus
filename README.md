# CloudStatus

A Windows 10/11 tray app that shows the status of a **HyperX Cloud II Wireless** headset: battery,
charging and whether it is on, with a notice when the battery runs low. Built to cost nothing while
playing Counter-Strike 2 competitively.

> The firmware version is planned for a later version (spec 011). The project follows Spec Driven
> Development: code only comes after an approved spec.

Supported: the HP-branded dongle (`0x03F0:0x018B`, tested) and `0x03F0:0x0696` (same protocol,
untested). Kingston-era dongles are not supported in 1.0.

Not affiliated with or endorsed by HyperX or HP.

## Installation

Download the latest version from **[Releases](../../releases)**:

| File | Purpose |
|---|---|
| `CloudStatus-x.y.z.msi` | **Per-user** Windows Installer package: no administrator rights needed. Its first page lets you choose English or Español (for the installer and for CloudStatus); it installs to `%LOCALAPPDATA%\Programs\CloudStatus`, adds a Start menu shortcut, and its last page offers to create a desktop shortcut and to open CloudStatus. Uninstall it from *Settings → Apps*. |
| `CloudStatus-x.y.z-portable.exe` | The same executable without installing: copy it anywhere and run it. |
| `SHA256SUMS.txt` | Hashes to verify the downloads: `Get-FileHash <file>` in PowerShell, or `sha256sum -c SHA256SUMS.txt`. |

The binaries are not code-signed, so Windows SmartScreen may warn you the first time
(*More info → Run anyway*). They are built by GitHub Actions from this source code
([workflow](.github/workflows/release.yml)).

**Upgrading** with a newer MSI keeps your settings. **Uninstalling** also removes the settings, the
*Start with Windows* entry and the desktop shortcut. If CloudStatus is running, Windows Installer
closes it automatically.

## Usage

| Action | How |
|---|---|
| Battery at a glance | The tray icon shows the percentage; hover or open the menu for the details |
| Settings | Double-click the tray icon, press Enter on it, or menu → **Settings…** |
| Exit | Menu → **Exit** |
| Closing Settings with the **X** button | Asks: **Minimize to tray** (the app keeps running) or **Close CloudStatus**. *Cancel* and Esc just close the window. |

The icon follows the taskbar's light or dark theme. When all is well it is just the number, like
the system icons next to it; a colored background means something worth a look:

| Icon | Meaning |
|---|---|
| Number alone | Battery at 50 % or more |
| Number on amber | Below 50 % |
| Number on red | Below the low battery limit (20 % by default), or a charging error |
| Number on teal | Charging or full |
| Square outline | Dongle found, asking the headset |
| Headset | Headset off |
| Headset crossed out | No dongle |

With high contrast on, every state keeps its own background, as in 1.0. The tooltip and the first
line of the menu spell the state out, for example *78 % · Charging*.

Charging and power changes show up as they happen, because the headset reports them. The battery
itself is read every 5 minutes (configurable). When the battery drops under the low limit, and
again under 10 %, a silent Windows notice appears once; it comes back only after charging or after
the battery recovers 5 points. During a full-screen game Windows' *Do not disturb* holds it in the
notification center.

When you open CloudStatus yourself (Start menu, the exe, or the end of the installation) it shows
the Settings window; when Windows starts it at sign-in (*Start with Windows*) it stays silently in
the tray. After the PC sleeps, it reconnects to the dongle by itself.

### Settings

- **Notify when the battery is low**: on by default.
- **Low battery below (%)**: 15–45, 20 by default. It moves both the red icon and the notice.
- **Read the battery every (min)**: 1–30, 5 by default. Shorter means more wake-ups.
- **Language**: *Automatic (system)* (Spanish if Windows is in Spanish, English otherwise),
  *Español* or *English*.
- **Start with Windows**.

Changes apply on save, without restarting. The window title shows the installed version. The
window, its title bar and the tray menu follow Windows' light or dark mode, also while open.

### Updates

**Check for updates** (in Settings) looks up the latest release on GitHub. If there is a newer one,
it asks first, then downloads the MSI, verifies it against the release's `SHA256SUMS.txt` before
writing it to disk, and starts it; the installer preselects your current language, replaces the app,
keeps your settings and offers to open CloudStatus at the end. With the portable exe it opens the
download page instead. Every release's changes are listed in [CHANGELOG.md](CHANGELOG.md). The app
never checks for updates on its own.

## Why it has no impact

- **One thread that sleeps.** The app waits in `MsgWaitForMultipleObjectsEx` for a window message or
  a HID completion. Nothing is polled in a loop: 0 % CPU at rest.
- **Overlapped HID.** One read is always pending on the dongle; the headset's answers and its own
  reports arrive as events. Every command has a 500 ms deadline, and nothing ever blocks the thread
  on the device.
- **Few wake-ups.** Charging and power are pushed by the headset. The battery query runs once per
  polling interval on a coalescable timer, so Windows can group it with other work. With the headset
  off or no dongle there is no timer at all: plugging the dongle in is a device notification.
- **Small.** About 3.5 MB of private memory, 2 threads and a native exe of about 1 MB, with no .NET
  runtime to load. The icon is redrawn only when what it shows changes.
- **Nothing per input event, nothing in the game.** No hooks, no overlays, no audio endpoints.
- **Coexists.** The HID interface is opened shared, so NGENUITY and other tools keep working, and
  CloudStatus also reads the answers to their queries. Close other battery tools while measuring:
  their polling wakes everything up.

Measured budgets and how to check them: [docs/PERFORMANCE.md](docs/PERFORMANCE.md).

## What it does NOT do

Intended for review against anti-cheat software. The code **does not use**:

| Category | APIs that do not appear in the code |
|---|---|
| Hooks | `SetWindowsHookEx` (any type), `SetWinEventHook` |
| Keyboard polling | `GetAsyncKeyState`, `GetKeyState` |
| Input | Raw Input (`RegisterRawInputDevices`, `GetRawInputData`) |
| Input injection | `SendInput`, `keybd_event`, `mouse_event` |
| Other processes | `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, `CreateRemoteThread`, enumerating processes or modules |
| Graphics | DirectX/OpenGL overlays; nothing is drawn inside the game |
| Audio | Core Audio, audio endpoints |
| Threads | `Task`, `async`, the thread pool, timers on other threads |

Also:

- **The headset is read-only.** The only reports CloudStatus sends are two queries, battery and
  charging, defined in [`Device/Commands.cs`](CloudStatus/Device/Commands.cs) with their source in
  [docs/protocol.md](docs/protocol.md). It never changes a headset setting, never sends the command
  that resets its state, and never touches firmware.
- **No network access and no telemetry**, with one exception: when you click **Check for updates**,
  it connects to `github.com` only (nothing about you or your PC is sent; GitHub serves the download
  itself from its own host) and, if you accept, starts the verified installer or opens the download
  page. Nothing happens automatically or in the background. It never contacts HP or HyperX.
- It does not know or care which program is in the foreground.
- No administrator rights (`asInvoker` manifest), no packers or obfuscators.

CI enforces the list on every pull request with
[`tools/Test-ForbiddenApis.ps1`](tools/Test-ForbiddenApis.ps1), which also limits device access,
WinHTTP and starting processes to the few files that need them.

## Design documents

- [Vision](specs/000-vision.md): what this is and what it is not.
- [Roadmap](specs/ROADMAP.md) and [how specs work](specs/README.md).
- [Architecture](docs/ARCHITECTURE.md), [performance budgets](docs/PERFORMANCE.md) and the
  [HID protocol](docs/protocol.md).

## Building

Requirements:

1. **.NET 10 SDK** (`dotnet --list-sdks` must list a 10.x version).
2. **Visual Studio 2022 or later** (or Build Tools) with the **"Desktop development with C++"**
   workload, including the **Windows SDK**. NativeAOT needs it for the linker (`link.exe`) and the
   Windows import libraries. Without it, `dotnet publish` fails with *"Platform linker not found"*.

The installer uses [WiX Toolset](https://wixtoolset.org) 5 as an MSBuild SDK: it is downloaded
automatically at build time, nothing else to install. The app itself uses no NuGet packages.

```powershell
.\build.ps1 -Version 1.2.3
```

Output in `artifacts\`:

```
CloudStatus-1.2.3.msi
CloudStatus-1.2.3-portable.exe
SHA256SUMS.txt
```

The executable only, without the installer:

```powershell
dotnet publish CloudStatus\CloudStatus.csproj -c Release
# → CloudStatus\bin\Release\net10.0-windows\win-x64\publish\CloudStatus.exe
```

Checks that CI also runs:

```powershell
.\tools\Test-ForbiddenApis.ps1          # APIs CloudStatus promises never to use
dotnet test tests\CloudStatus.Core.Tests # unit tests of the pure logic
.\tools\Test-Artifacts.ps1 -Version 1.2.3
.\tools\Test-Smoke.ps1 -Exe artifacts\CloudStatus-1.2.3-portable.exe
```

`tools\Test-Installer.ps1` installs and uninstalls the MSI for real, so it only runs on GitHub
Actions.

> With Visual Studio 2026 (v18), `vcvarsall.bat` calls `vswhere.exe` without a full path and linking
> fails. `build.ps1` works around it by adding the Visual Studio Installer folder to `PATH`; if you
> run `dotnet publish` directly, use the *Developer PowerShell for VS*.

### Publishing a release

1. In [CHANGELOG.md](CHANGELOG.md), move the `[Unreleased]` entries to a new
   `## [1.2.3] - YYYY-MM-DD` section (and update the links at the bottom). Commit and push.
2. Tag and push:

   ```powershell
   git tag -a v1.2.3 -m "CloudStatus 1.2.3"
   git push origin v1.2.3
   ```

The [`release.yml`](.github/workflows/release.yml) workflow builds the exe and the MSI on
`windows-latest` and creates the GitHub Release with these files. Its notes are the version's
changelog section plus a downloads table (`tools/Get-ReleaseNotes.ps1`); if the section is missing,
the release fails before building anything.

## Registry

| Key | Values |
|---|---|
| `HKCU\Software\CloudStatus` | `LowBatteryNotice` (DWORD 0/1), `LowBatteryPercent` (DWORD, 15–45), `PollMinutes` (DWORD, 1–30), `Language` (DWORD: 0 automatic, 1 Spanish, 2 English) |
| `HKCU\Software\CloudStatus\Install` | Used by the MSI to track what it installed |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | `CloudStatus` = `"<path of the exe>" --startup` (only when *Start with Windows* is enabled; `--startup` keeps it in the tray) |

Missing or invalid values fall back to their defaults, each on its own.

Removing the portable version by hand: untick *Start with Windows*, exit, delete the exe and,
optionally, the `HKCU\Software\CloudStatus` key. `CloudStatus.exe --uninstall` does the registry
part and removes the desktop shortcut if it points to that exe (it is what the MSI runs on
uninstall); `CloudStatus.exe --create-desktop-shortcut` creates the shortcut (what the installer's
last page runs).

## Robustness

- **Single instance** (`Local\CloudStatus` mutex): a second launch exits silently.
- **Dongle plugged in or removed**: followed through device notifications, with no timers.
- **Headset off, out of range or not answering**: shown as off; it comes back when the headset says
  it is on again.
- **Sleep**: the dongle is closed before the PC sleeps and opened again on resume.
- **Explorer restarts**: the tray icon comes back (`TaskbarCreated`).
- **Session end or an installer closing it**: the app exits cleanly on `WM_ENDSESSION`.
- **Invalid settings in the registry**: each one falls back to its default.

## Project structure

```
build.ps1                    exe + MSI + hashes into artifacts\
.github/workflows/           build.yml (every pull request), release.yml (v* tags)
CHANGELOG.md                 changes per version (also used as release notes)
specs/                       one spec per feature, the roadmap and the vision
docs/                        architecture decisions, performance budgets, HID protocol
installer/                   per-user MSI (WiX 5): Package.wxs, Dialogs.wxs, Texts.wxs
assets/                      logo (CloudStatus.ico, logo.svg) and installer bitmaps
tools/                       branding, release notes, and the checks CI runs
tests/CloudStatus.Core.Tests xUnit tests of Core/ (the only project with NuGet packages)
CloudStatus/
  CloudStatus.csproj         NativeAOT, no NuGet packages
  app.manifest               Common Controls v6, PerMonitorV2, asInvoker
  Program.cs                 entry point: arguments, single instance
  App.cs                     lifetime and use cases spanning several components
  SingleInstance.cs          single-instance mutex
  Core/                      pure logic, no Windows calls, all of it unit tested
    HeadsetMonitor.cs        headset state machine: events in, actions out
    HpReports.cs             parsing of the headset's reports
    IconDescription.cs, LowBatteryAlert.cs, SuspendGate.cs, CommandTracker.cs
    InterfaceSelector.cs, DevicePath.cs, Config.cs, StartupCommand.cs, ReleaseInfo.cs, AppVersion.cs
  Device/                    the dongle
    SupportedDevices.cs      the dongles CloudStatus talks to
    Commands.cs              every report it may send, each with its protocol.md reference
    HidDiscovery.cs, DongleWatcher.cs, HidTransport.cs, HeadsetLink.cs
  Persistence/               HKCU registry and the desktop shortcut
  Updates/                   "Check for updates" (the only network code, run on click)
  Ui/                        Win32: hidden window, message loop, tray icon, notice, strings
    Settings/                settings window, close confirmation, update check
  Native/                    P/Invoke with [LibraryImport], no logic
```

## Contributing

Issues and pull requests are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

## Credits

The protocol facts come from the reverse engineering done by
[auto94/HyperX-Cloud-2-Battery-Monitor](https://github.com/auto94/HyperX-Cloud-2-Battery-Monitor),
[LennardKittner/HyperHeadset](https://github.com/LennardKittner/HyperHeadset) and
[Sapd/HeadsetControl](https://github.com/Sapd/HeadsetControl). See
[docs/protocol.md](docs/protocol.md) for details and licenses.

## License

[MIT](LICENSE) © jordanzdev

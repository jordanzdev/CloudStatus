# CloudStatus

A Windows 10/11 tray app that shows the status of a **HyperX Cloud II Wireless** headset: battery,
charging, whether it is on, and firmware version. Built to cost nothing while playing
Counter-Strike 2 competitively.

> **Early development.** The tray icon shows the battery and status of an HP-branded headset, with
> a low-battery notice and a Settings window. There is no release yet. The project follows Spec
> Driven Development: code only comes after an approved spec.

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

## Status

- [Vision](specs/000-vision.md): what this is and what it is not.
- [Roadmap](specs/ROADMAP.md): the order of the work.
- [How specs work](specs/README.md).
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

## Credits

The protocol facts come from the reverse engineering done by
[auto94/HyperX-Cloud-2-Battery-Monitor](https://github.com/auto94/HyperX-Cloud-2-Battery-Monitor),
[LennardKittner/HyperHeadset](https://github.com/LennardKittner/HyperHeadset) and
[Sapd/HeadsetControl](https://github.com/Sapd/HeadsetControl). See
[docs/protocol.md](docs/protocol.md) for details and licenses.

## License

[MIT](LICENSE) © jordanzdev

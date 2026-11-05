# Contributing to CloudStatus

Thanks for helping! Bug reports, ideas and pull requests are welcome.

## Reporting a bug or suggesting a feature

Open an [issue](../../issues). For bugs, include your Windows version, the dongle's ID (*Device
Manager → the HID device → Details → Hardware Ids*, for example `VID_03F0&PID_018B`), the
CloudStatus version from the Settings window title, which other headset tools are running, and the
steps to reproduce.

## Specs first

CloudStatus follows Spec Driven Development: **no production code without an approved spec**. Read
[specs/README.md](specs/README.md) before changing behavior.

1. Propose the change in an issue, or draft a spec from the [template](specs/README.md#template).
2. Once the spec is approved, implement it on a branch named `spec/NNN-name`, with tests.
3. If the implementation contradicts the spec, stop and propose a change to the spec first.
4. The pull request updates the spec's status and `CHANGELOG.md` `[Unreleased]`, and lists every
   `[hw]` criterion that someone has to check with a headset.

Small fixes that change no behavior (typos, comments, build scripts) do not need a spec.

## Making a change

1. Fork the repository and create a branch from `main`.
2. Build and check locally (see [Building](README.md#building)):

   ```powershell
   .\tools\Test-ForbiddenApis.ps1
   dotnet test tests\CloudStatus.Core.Tests
   .\build.ps1 -Version 0.0.0
   .\tools\Test-Artifacts.ps1 -Version 0.0.0
   .\tools\Test-Smoke.ps1 -Exe artifacts\CloudStatus-0.0.0-portable.exe
   ```

3. Add a line for user-visible changes under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md).
4. Open a pull request against `main`, describing what changed and how you tested it.

Every pull request is built by the [Build](.github/workflows/build.yml) workflow, which runs the
checks above, installs and uninstalls the MSI on the runner, and attaches the MSI and the portable
exe to the run.

## Rules that keep CloudStatus safe and fast

These are the reasons the app exists; pull requests that break them cannot be accepted.

- **Anti-cheat safety.** Never use the APIs listed in
  [What it does NOT do](README.md#what-it-does-not-do): no hooks, no keyboard polling, no Raw Input,
  no input injection, no access to other processes, no overlays, no audio endpoints. No telemetry
  and no administrator rights.
- **The headset is read-only.** Every report the app may send is a constant in
  `Device/Commands.cs`, with its source in `docs/protocol.md`; `WriteFile` appears only in
  `Device/HidTransport.cs`. A new command needs its own spec and real-hardware evidence. Never send
  commands that change settings or firmware, and never invent bytes.
- **Network only on request.** The only network code is *Check for updates* (`Updates/`): it talks
  to `github.com` only, runs only when the user clicks it, sends no user data, and only starts the
  downloaded installer (after its SHA-256 check) or the download page.
- **Minimal resources.** One thread, no `Task`/`async`/thread pool, overlapped I/O only, no timers
  without a dongle, no allocations in the message loop or when nothing changed. See the budgets in
  [docs/PERFORMANCE.md](docs/PERFORMANCE.md).
- **NativeAOT clean.** No reflection, no NuGet packages in the app, and no warnings, including
  trimming and AOT ones (IL2xxx/IL3xxx).
- **Release assets keep their names.** The updater downloads `CloudStatus-<version>.msi` and
  `SHA256SUMS.txt` from the release tagged `v<version>`.
- **Original branding.** No HyperX or HP logos, names in the icon, or assets.

## Code style

- All code, comments and documentation in English.
- Follow the structure: `Core` (pure logic, tested), `Device` (the dongle), `Persistence`
  (registry), `Updates` (network), `Ui` (Win32), `Native` (P/Invoke declarations only, with
  `[LibraryImport]`).
- User-facing text goes in `Ui/Strings.cs` (app) and `installer/Texts.wxs` (installer), in English
  and Spanish.
- Window procedures are `[UnmanagedCallersOnly]` and wrapped in `try`/`catch`: an exception must
  never cross into native code.
- PowerShell scripts stay ASCII: Windows PowerShell 5.1 reads files without a BOM as ANSI.

## Releases

Maintainers publish a release in two steps:

1. In [CHANGELOG.md](CHANGELOG.md), move the `[Unreleased]` entries to a new
   `## [1.2.3] - YYYY-MM-DD` section, update the links at the bottom, commit and push.
2. Push a version tag:

   ```powershell
   git tag -a v1.2.3 -m "CloudStatus 1.2.3"
   git push origin v1.2.3
   ```

The [Release](.github/workflows/release.yml) workflow builds the MSI and the portable exe and
attaches them, with their SHA-256 hashes, to a new GitHub Release whose notes are the version's
changelog section. It fails before building if that section is missing.

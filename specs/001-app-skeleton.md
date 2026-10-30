# 001: App skeleton, build and CI

**Status:** Implemented

## Context

Everything else needs a running app: discovery needs a window for `WM_DEVICECHANGE` (D6), and the
transport needs the message loop (D3). Every later pull request also needs CI.

This spec ships an app that does almost nothing, on purpose: it sits in the tray, costs nothing and
exits cleanly. It follows CursorLock's structure, adapted to D1, D3, D17 and D18. It has no device
code and sends nothing to the headset.

## Goals

- A native `CloudStatus.exe` that starts, shows a tray icon and exits cleanly.
- The message loop that later specs extend.
- A build, tests and CI that enforce the project's rules from the first commit.

## Non-goals

- Anything related to the headset (002 onwards).
- Settings, the registry, and `--startup` (007).
- MSI, `release.yml`, `--uninstall` and `--create-desktop-shortcut` (009).
- The percentage icon (005). The tray shows the app logo.

## Requirements

### Project

- **REQ-001-01** The solution MUST contain `CloudStatus/CloudStatus.csproj`, with the NativeAOT and
  size properties of `CursorLock.csproj`, adding `TreatWarningsAsErrors`.
- **REQ-001-02** `CloudStatus.csproj` MUST have no `PackageReference`.
- **REQ-001-03** `app.manifest` MUST declare Common Controls v6, PerMonitorV2 and `asInvoker`.
- **REQ-001-04** The folders `Core/`, `Device/`, `Persistence/`, `Ui/`, `Updates/` and `Native/`
  MUST follow the [architecture table](../docs/ARCHITECTURE.md#overview). Only folders with code
  are created in this spec.
- **REQ-001-05** `Native/` MUST hold every `[LibraryImport]`, and no logic.

### Lifetime

- **REQ-001-06** A second launch MUST exit silently, using the mutex `Local\CloudStatus`.
- **REQ-001-07** The app MUST create a hidden top-level window that routes messages.
- **REQ-001-08** Window procedures MUST be `[UnmanagedCallersOnly]` and wrapped in `try`/`catch`.
- **REQ-001-09** The app MUST exit cleanly on the *Exit* menu item, `WM_CLOSE` and `WM_ENDSESSION`,
  removing the tray icon on every path.

### Message loop

- **REQ-001-10** The loop MUST use `MsgWaitForMultipleObjectsEx` with `QS_ALLINPUT`,
  `MWMO_INPUTAVAILABLE` and an `INFINITE` timeout (D3). In this spec it waits on zero handles.
- **REQ-001-11** The loop MUST drain all pending messages with `PeekMessageW` after each wake-up,
  and MUST keep `IsDialogMessageW` support for later windows.
- **REQ-001-12** The loop MUST NOT allocate managed memory per iteration.
- **REQ-001-13** No timer may exist at rest, except the tray retry below while it is needed.

### Tray

- **REQ-001-14** The app MUST show one tray icon using the app logo, with the tooltip `CloudStatus`.
- **REQ-001-15** If adding the icon fails at session start, the app MUST retry every 2 s, up to 30
  times, as in CursorLock.
- **REQ-001-16** The icon MUST come back after Explorer restarts (`TaskbarCreated`).
- **REQ-001-17** The icon MUST follow `WM_DPICHANGED`.
- **REQ-001-18** The right-click menu MUST offer *Exit*. Handles created for it MUST be released.

### Language

- **REQ-001-19** User-facing text MUST live in `Ui/Strings.cs`, in English and Spanish, chosen
  automatically from the Windows display language.

### Branding

- **REQ-001-20** `tools/New-Branding.ps1` MUST generate `assets/CloudStatus.ico` and
  `assets/logo.svg` from one geometry, with an original design and no HyperX or HP marks.

### Build and tests

- **REQ-001-21** `build.ps1 -Version x.y.z` MUST produce `artifacts/CloudStatus-x.y.z-portable.exe`
  and `artifacts/SHA256SUMS.txt`, with LF line endings. The MSI is added in 009.
- **REQ-001-22** `tests/CloudStatus.Core.Tests` MUST exist as an xUnit project that compiles `Core/`
  through linked files (D18).
- **REQ-001-23** `Core/` MUST contain `AppVersion` (from CursorLock), so the tests have real code
  to compile from the first commit.

### CI

- **REQ-001-24** `tools/Test-ForbiddenApis.ps1` MUST enforce the lists in D17 and exit non-zero with
  file and line for each violation.
- **REQ-001-25** `.github/workflows/build.yml` MUST run on every pull request and on pushes to
  `main`: forbidden-API check, tests, `build.ps1`, exe size check and artifact upload.
- **REQ-001-26** The exe size check MUST fail above 1.5 MB (1,572,864 bytes).

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| Exe size | Sets the baseline, expected well below 1.5 MB | AC-001-13 |
| CPU at rest | Must be 0 % | AC-001-16 |
| Threads | 2 at rest | AC-001-15 |
| Private memory | ≤ 5 MB, expected near 3 MB | AC-001-15 |
| Handles and GDI | Stable | AC-001-17 |
| Wake-ups | None at rest | AC-001-16 |
| Start to icon | ≤ 300 ms | AC-001-18 |

## Acceptance criteria

- **AC-001-01** `[auto]` Given a clean checkout, when `build.ps1 -Version 0.0.0` runs, then it
  succeeds with no warnings and produces the exe and `SHA256SUMS.txt`.
- **AC-001-02** `[auto]` Given `SHA256SUMS.txt`, when it is read as bytes, then it has no CR
  characters and its hash matches the exe.
- **AC-001-03** `[auto]` Given `CloudStatus.csproj`, when it is scanned, then it contains no
  `PackageReference`.
- **AC-001-04** `[auto]` Given the source, when `Test-ForbiddenApis.ps1` runs, then it exits 0.
- **AC-001-05** `[auto]` Given a temporary copy of the source with a line using each forbidden
  category added, when the script runs, then it exits non-zero and names each file and line.
- **AC-001-06** `[auto]` Given a temporary copy with `WriteFile` used outside the allowed files,
  when the script runs, then it exits non-zero.
- **AC-001-07** `[auto]` Given a temporary copy with `[LibraryImport]` outside `Native/`, when the
  script runs, then it exits non-zero.
- **AC-001-08** `[auto]` Given the tests, when `dotnet test` runs, then they pass.
- **AC-001-09** `[auto]` Given a pull request, when CI runs, then every step in REQ-001-25 runs and
  passes.
- **AC-001-10** `[auto]` Given a running instance, when the exe starts again, then the second
  process exits with code 0 and one instance remains.
- **AC-001-11** `[auto]` Given a running instance, when `WM_CLOSE` is sent to its hidden window,
  then the process exits with code 0.
- **AC-001-12** `[auto]` Given the exe, when its PE imports are listed, then it needs no .NET runtime
  and its manifest requests `asInvoker`.
- **AC-001-13** `[auto]` Given the published exe, when CI measures it, then it is at most 1,572,864
  bytes, and CI fails if it is not.
- **AC-001-14** `[hw]` Given the app running, when the icon is right-clicked and *Exit* chosen, then
  the icon disappears and the process ends. No headset needed.
- **AC-001-15** `[hw]` Given the app at rest for 5 minutes, when measured as in PERFORMANCE.md, then
  it shows 2 threads and at most 5 MB of private memory. No headset needed.
- **AC-001-16** `[hw]` Given the app at rest, when a WPR trace covers 2 minutes, then the app has no
  context switches in that time. No headset needed.
- **AC-001-17** `[hw]` Given the app running, when Explorer restarts 10 times, then one icon is shown
  and handle and GDI counts return to their starting values. No headset needed.
- **AC-001-18** `[hw]` Given the app is not running, when it starts, then the icon appears within
  300 ms, as a median of five. No headset needed.
- **AC-001-19** `[hw]` Given two monitors with different scaling, when the DPI changes, then the icon
  stays sharp. No headset needed.
- **AC-001-20** `[hw]` Given Windows in English and in Spanish, when the menu opens, then it matches
  the display language. No headset needed.

Criteria AC-001-14 to AC-001-20 could partly be scripted, but they depend on a real desktop session,
so they stay manual.

## Open questions

None block this spec.

## Status history

- 2026-10-30: Draft.
- 2026-10-30: Approved.
- 2026-10-30: Implemented, `[hw]` criteria pending.

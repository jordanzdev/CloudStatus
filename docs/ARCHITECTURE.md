# Architecture

Numbered decisions. A decision is never edited to mean something else: a new decision replaces it
and the old one is marked as superseded. Requirements live in the [specs](../specs/README.md) and
protocol facts in [protocol.md](protocol.md).

## Overview

```
                 ┌───────────────────────── one thread ─────────────────────────┐
 Windows ──────► │ Message loop ──► MainWindow ──► App                          │
 (messages,      │ (MsgWaitFor…)                    │                           │
  device and     │      │                           ▼                           │
  power events)  │      │ I/O event          HeadsetMonitor (Core: pure logic)  │
                 │      ▼                     ▲             │                    │
 Dongle ◄──────► │ HidTransport ──► Parser ───┘             ▼                    │
                 │ (Device/)        (Core/)           TrayIcon, notices (Ui/)    │
                 └───────────────────────────────────────────────────────────────┘
```

| Folder | Holds | Win32 |
|---|---|---|
| `Core/` | Pure logic: state machine, parser, schedule, config values | No |
| `Device/` | HID discovery, transport, supported IDs, outbound reports | Yes, through `Native/` |
| `Persistence/` | `HKCU` registry | Yes, through `Native/` |
| `Ui/` | Windows, tray, icons, text | Yes, through `Native/` |
| `Updates/` | "Check for updates", the only network code | Yes, through `Native/` |
| `Native/` | `[LibraryImport]` declarations, no logic | Declarations only |

## Decisions

### D1. C# on .NET 10 with NativeAOT, plain Win32

**Context.** The app must be small and start fast, and CursorLock already proves this stack.
**Decision.** One native `CloudStatus.exe`, with the same project properties as CursorLock, no
NuGet packages in the app, and no reflection.
**Consequences.** Warnings are errors (`TreatWarningsAsErrors`), which CursorLock does not enforce.
Trimming and AOT warnings cannot slip through.

### D2. One thread, all HID I/O overlapped

**Context.** The app has to wait for a device without burning CPU, and it must never freeze.
**Decision.** Everything runs on the UI thread: no `Task`, `async`, thread pool, reader threads or
`System.Threading.Timer`. The device is opened with `FILE_FLAG_OVERLAPPED`.
**Consequences.** Blocking HID calls are not allowed. That rules out `HidD_SetOutputReport` and
`HidD_SetFeature`, which are synchronous. Writes use overlapped `WriteFile`.

### D3. Message loop built on `MsgWaitForMultipleObjectsEx`

**Context.** `GetMessageW` cannot wait on I/O events.
**Decision.** The loop waits on the active I/O events and on messages, with `QS_ALLINPUT`,
`MWMO_INPUTAVAILABLE` and an `INFINITE` timeout, then drains the queue with `PeekMessageW`. With no
device, the wait has zero handles and behaves like `GetMessageW`.
**Consequences.** The thread sleeps until something happens. The loop must not allocate.
`IsDialogMessageW` stays in the loop, as in CursorLock.

### D4. Modal loops do not break I/O

**Context.** While a tray menu, `MessageBoxW` or `TaskDialogIndirect` is open, Windows runs its own
loop, so the loop in D3 does not run. "Check for updates" also blocks the thread for a second or
two (D16).
**Decision.** I/O events are manual-reset, so a completion is not lost, only delayed. While a
command is in flight, a one-shot `WM_TIMER` fires at its deadline. Modal loops dispatch timers, and
the handler first checks for completions without blocking (`GetOverlappedResult` with `bWait` set
to false) and only then declares a timeout.
**Consequences.** A push report can wait until the menu closes, which is harmless. A response that
arrived in time is never reported as a timeout.

### D5. Events before polling

**Context.** Waking up on a timer costs energy and CPU, while a pending read costs nothing.
**Decision.** One overlapped read is always pending while a device is open. Polling is the backup,
and it is required for battery, which is not pushed according to [H] in protocol.md.
**Consequences.** Every report goes through the same parser, whether it answers a query or not.

### D6. Hot plug through device notifications

**Context.** With no dongle there must be no timers.
**Decision.** `RegisterDeviceNotificationW` with `GUID_DEVINTERFACE_HID` on the hidden main window.
`WM_DEVICECHANGE` is filtered by VID and PID in the device path, and enumeration runs only at
start-up and when a matching device arrives.
**Consequences.** Other HID devices wake the app briefly when plugged in, which is negligible. The
main window stays a hidden top-level window, as in CursorLock.

### D7. Pure state machine in `Core/`

**Context.** The hard logic (on/off, backoff, when to query) must be tested without a headset.
**Decision.** `Core/` holds a state machine that takes events and a time value, and returns what to
do next: send a command, schedule a timer, update the display. It calls nothing itself.
**Consequences.** Timers, I/O and UI are thin adapters, and every transition has a unit test.

### D8. Variants and IDs in one table

**Context.** More PIDs may appear, and the VID selects the protocol.
**Decision.** `Device/SupportedDevices.cs` is the only list of VID, PID, usage page and usage.
Adding a PID is one line. Interfaces are chosen by usage page and usage, never by probing.
**Consequences.** A descriptor can be read by opening a collection with access 0, which sends
nothing to the device.

### D9. Kingston is out of 1.0

**Context.** No source gives the Kingston usage (OQ-01), and nobody here can test one.
**Decision.** 1.0 supports HP only. Kingston packets stay in protocol.md, and the table in D8 gets
a Kingston row when a user supplies the usage.
**Consequences.** The `GetInputReport` preparation step and `DeviceIoControl` are not needed in
1.0, so neither is the CI exception for them.

### D10. Every outbound report in one file

**Context.** A wrong byte could change a setting or reset the headset.
**Decision.** `Device/Commands.cs` holds every outbound report as a constant, each citing
protocol.md section 6. A unit test asserts that the set matches exactly what that section allows.
**Consequences.** Adding a command means changing protocol.md, `Commands.cs` and the test.

### D11. Parsing is pure and allocation-free

**Context.** Parsing runs on every report.
**Decision.** The parser lives in `Core/`, takes a `ReadOnlySpan<byte>` and returns a value type.
Reports are matched by content (report ID, header, command), not by remembering what was sent, and
unknown reports are ignored.
**Consequences.** Responses to NGENUITY's queries are valid data too. Buffers are allocated once
when the device opens.

### D12. Two kinds of timer

**Context.** Polling can be late, but a command deadline cannot.
**Decision.** Polling uses `SetCoalescableTimer` with a wide tolerance so Windows can group
wake-ups. Command deadlines use a plain one-shot `SetTimer`, which exists only while a command is in
flight. With no dongle, neither exists.
**Consequences.** There is at most one wake-up per polling interval, plus device events.

### D13. Close the device while suspended

**Context.** Handles may not survive sleep.
**Decision.** On `PBT_APMSUSPEND` cancel I/O, stop timers and close the handle. On
`PBT_APMRESUMEAUTOMATIC` enumerate again.
**Consequences.** Resume takes the same path as plugging the dongle in.

### D14. Tray icon drawn in memory, only when it changes

**Context.** The percentage changes rarely, and GDI objects are a classic leak (a possible one was
reported against auto94's monitor, issue #19).
**Decision.** The icon is drawn into a DIB as in CursorLock, and only when the displayed value
changes. One `HICON` is cached and the previous one destroyed.
**Consequences.** Spec 005 picks how to draw digits at 16 px and at every DPI.

### D15. Settings in `HKCU\Software\CloudStatus`

**Context.** Same as CursorLock.
**Decision.** No files. A missing or invalid value falls back to its default.

### D16. Updates work as in CursorLock

**Context.** The update code exists and is proven.
**Decision.** Same flow: only on click, only `github.com`, SHA-256 verified, synchronous WinHTTP on
the UI thread.
**Consequences.** The thread blocks for a second or two after a click. That is an explicit user
action, and D4 keeps I/O correct across it.

### D17. Policy is enforced by a script

**Context.** The promises in "What it does NOT do" only hold if something checks them.
**Decision.** `tools/Test-ForbiddenApis.ps1` runs in CI and locally. It holds these lists:
- **Forbidden everywhere:** the CursorLock list; process enumeration; `HidD_SetOutputReport`,
  `HidD_SetFeature`; thread and async primitives; Core Audio and graphics APIs.
- **Restricted by file:** `WriteFile` only in `Native/Kernel32.cs` and `Device/HidTransport.cs`;
  `[LibraryImport]` only in `Native/`; network APIs only in `Native/WinHttp.cs` and `Updates/`.
**Consequences.** Declaring a P/Invoke in `Native/` and calling it from the transport does not
break the rule. `DeviceIoControl` joins the restricted list if Kingston is ever added.

### D18. Tests compile `Core/` directly

**Context.** The app is a NativeAOT executable, and its types are internal.
**Decision.** `tests/CloudStatus.Core.Tests` (xUnit) includes the sources of `Core/`,
`Device/Commands.cs` and `Device/SupportedDevices.cs` as linked files instead of referencing the app
project. It is the only project
with NuGet packages, and it is not distributed.
**Consequences.** Anything in `Core/` that needs Win32 fails to compile in the tests, which also
enforces the purity rule.

### D19. Coexist with NGENUITY

**Context.** Both may talk to the dongle at once.
**Decision.** Open with `FILE_SHARE_READ | FILE_SHARE_WRITE`, send one command at a time, and keep
the polling interval long.
**Consequences.** Simultaneous commands from both apps stay possible, but are rare. A lost response
is an ordinary timeout.

### D20. Runtime threads are accepted

**Context.** The budget says no threads of our own, but CursorLock shows 2 threads at rest. The
second one comes from the runtime, not from its code. Which runtime thread it is has not been
confirmed.
**Decision.** The budget is **no thread created by our code**, measured as a count of 2 at rest.
**Consequences.** See [PERFORMANCE.md](PERFORMANCE.md). Any count above 2 at rest is a failure to
explain.

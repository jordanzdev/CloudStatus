# 002: Dongle discovery, interface selection and hot plug

**Status:** Approved

## Context

CloudStatus must know whether a supported dongle is present, and which HID collection speaks the
protocol, without timers and without sending anything to the device (D6, D8). On the target dongle
that collection is `COL03`, usage page `0xFF90`, usage `0x0303`
([protocol.md](../docs/protocol.md#2-devices)).

This spec only **finds** the interface. It reads the descriptor Windows has already cached, with
access 0. Opening the device for I/O belongs to 003, so **no report reaches the headset in this
spec**.

## Goals

- Detect a supported dongle at start-up and when it is plugged in or removed.
- Select the protocol interface by usage page and usage, reading its report lengths.
- Show the result in the tooltip, so the behavior can be checked by hand.

## Non-goals

- Reading from or writing to the device (003).
- Battery or any other headset state (004).
- Suspend and resume (008).
- Kingston dongles (D9).
- Choosing between two connected dongles in the UI. See REQ-002-08.

## Requirements

### Supported devices

- **REQ-002-01** `Device/SupportedDevices.cs` MUST be the only list of VID, PID, usage page and
  usage (D8). For 1.0 it MUST contain exactly `0x03F0:0x018B` and `0x03F0:0x0696`, both with usage
  page `0xFF90` and usage `0x0303`.

### Discovery

- **REQ-002-02** Discovery MUST list the present interfaces of `GUID_DEVINTERFACE_HID` and skip
  every path whose VID and PID are not in the table, without opening it.
- **REQ-002-03** Remaining paths MUST be opened with access 0 and `FILE_SHARE_READ |
  FILE_SHARE_WRITE`, read with `HidD_GetPreparsedData` and `HidP_GetCaps`, and closed straight away.
- **REQ-002-04** The app MUST NOT open any device with read or write access in this spec, and MUST
  NOT call any `HidD_Get*String`, `ReadFile` or `WriteFile`.
- **REQ-002-05** The selected interface MUST match the table's usage page and usage, and have input
  and output report lengths above 0. An interface that fails any check is ignored, never probed.
- **REQ-002-06** The result MUST keep the device path and both report lengths for 003.
- **REQ-002-07** Selection logic MUST be pure, in `Core/`, taking the candidates' values as input.
- **REQ-002-08** With several matching interfaces, the app MUST pick the first by table order and
  then by ordinal path order, so the choice is deterministic. Others are ignored.
- **REQ-002-09** Failure to open or read one path MUST skip that path and MUST NOT end discovery.

### Hot plug

- **REQ-002-10** The app MUST register `RegisterDeviceNotificationW` for `GUID_DEVINTERFACE_HID` on
  the hidden window, and unregister on shutdown.
- **REQ-002-11** On `DBT_DEVICEARRIVAL` with no dongle selected, discovery MUST run only if the
  path's VID and PID are in the table. One dongle sends one arrival per collection, so a run that
  finds nothing is normal, and the next arrival tries again.
- **REQ-002-12** On `DBT_DEVICEREMOVECOMPLETE`, the dongle MUST count as gone only if the removed
  path equals the selected one, compared case-insensitively. Discovery then runs once, in case
  another dongle is present.
- **REQ-002-13** Notifications for other devices MUST return without enumerating or allocating.
- **REQ-002-14** No timer may exist because of this spec, with or without a dongle.

### Display

- **REQ-002-15** The tray icon MUST appear before the first discovery runs.
- **REQ-002-16** The tooltip MUST read `CloudStatus: Dongle connected` or `CloudStatus: No dongle`,
  from `Ui/Strings.cs` in English and Spanish, and update only when the state changes.

### Policy

- **REQ-002-17** `Test-ForbiddenApis.ps1` MUST allow `CreateFileW` only in `Native/Kernel32.cs` and
  `Device/`, and forbid `GENERIC_READ` and `GENERIC_WRITE` everywhere. Spec 003 relaxes the second
  rule for the transport.
- **REQ-002-18** The test project MUST also link `Device/SupportedDevices.cs`, which needs a change
  to D18.

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| CPU at rest | Unchanged, 0 % | AC-002-14 |
| Wake-ups | None at rest, with or without a dongle | AC-002-14 |
| Threads | Unchanged, 2 at rest | AC-002-15 |
| Handles and GDI | Stable across plug cycles | AC-002-15 |
| Private memory | ≤ 5 MB | AC-002-15 |
| Start to icon | Unchanged, because discovery runs after the icon | AC-002-16 |
| Exe size | Grows slightly, CI enforces it | CI |

## Acceptance criteria

- **AC-002-01** `[auto]` Given the table, when a test reads it, then it holds exactly the two entries
  in REQ-002-01 and no Kingston entry.
- **AC-002-02** `[auto]` Given paths in upper and lower case, when VID and PID are parsed, then both
  give the same values, and malformed paths give no match without throwing.
- **AC-002-03** `[auto]` Given the four collections observed on `0x03F0:0x018B`, when selection
  runs, then it picks `COL03` and never `COL04`.
- **AC-002-04** `[auto]` Given a candidate with the right usage and an input or output length of 0,
  when selection runs, then it is rejected.
- **AC-002-05** `[auto]` Given two matching candidates in any order, when selection runs, then the
  same one is picked.
- **AC-002-06** `[auto]` Given no matching candidate, when selection runs, then it reports none.
- **AC-002-07** `[auto]` Given a removal for a path that differs from the selected one, when it is
  evaluated, then the dongle stays selected; with the same path in another case, it counts as gone.
- **AC-002-08** `[auto]` Given a temporary copy of the source using `GENERIC_WRITE`, or `CreateFileW`
  outside the allowed files, when the policy script runs, then it exits non-zero.
- **AC-002-09** `[hw]` Given the dongle plugged in, when the app starts, then the tooltip shows it as
  connected.
- **AC-002-10** `[hw]` Given no dongle, when the app starts, then the tooltip says there is none.
- **AC-002-11** `[hw]` Given the app running, when the dongle is removed and plugged in again, then
  the tooltip follows within 1 s each time, with the headset on and with it off.
- **AC-002-12** `[hw]` Given the app running, when a different USB device is plugged in, then the
  tooltip does not change.
- **AC-002-13** `[hw]` Given audio playing and the microphone muted on the headset, when the app
  starts and runs discovery, then there is no dropout or click, and the mute state does not change.
- **AC-002-14** `[hw]` Given the app at rest, with a dongle and then without one, when a 2-minute WPR
  trace runs, then the app has no context switches.
- **AC-002-15** `[hw]` Given the app running, when the dongle is unplugged and replugged 20 times,
  then handles and GDI objects return to their starting values, with 2 threads and at most 5 MB.
- **AC-002-16** `[hw]` Given the dongle plugged in, when the app starts, then the icon appears within
  300 ms, as a median of five.

## Open questions

None block this spec. `0x03F0:0x0696` is assumed to use the same usage as `0x018B`, because [C] in
protocol.md gives one usage for both, and it cannot be tested here.

## Status history

- 2026-10-30: Draft.
- 2026-10-30: Approved.

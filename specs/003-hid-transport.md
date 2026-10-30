# 003: Overlapped HID transport in the message loop

**Status:** Implemented

## Context

This is the first spec that **sends bytes to the headset**. Discovery (002) gives the path and report
lengths of the protocol interface. This spec opens it for I/O on the UI thread, without blocking it
(D2, D3, D4), keeps one read pending (D5), and sends one command at a time (D19).

To prove the transport on real hardware it sends a single battery query per connection and shows the
result. Parsing charging, on/off and polling belong to 004.

It answers OQ-05 (does `WriteFile` work on `COL03`?) and OQ-04 (real command latency).

## Goals

- Non-blocking reads and writes in the message loop, with timeout and cancellation.
- A response is handled as soon as it arrives, with no sleeps.
- Measured latency of a real command.

## Non-goals

- Polling, charging, on/off, push events (004).
- Any report other than the battery query.
- Suspend and resume (008).

## Requirements

### Outbound reports

- **REQ-003-01** `Device/Commands.cs` MUST hold every outbound report (D10). In this spec that is only
  the HP battery query, `06 FF BB 02 00`, citing protocol.md section 6.
- **REQ-003-02** A report MUST be zero padded to the interface's output length. A report longer than
  that MUST NOT be sent.
- **REQ-003-03** A unit test MUST assert the exact set of outbound reports, and that none has command
  `0x01`.

### Opening

- **REQ-003-04** Only the path selected by 002 may be opened, with `GENERIC_READ | GENERIC_WRITE`,
  `FILE_SHARE_READ | FILE_SHARE_WRITE` and `FILE_FLAG_OVERLAPPED`.
- **REQ-003-05** Report lengths above 64 bytes MUST make the transport refuse the interface.
- **REQ-003-06** Buffers and `OVERLAPPED` structures MUST live in native memory allocated once per
  open, because the kernel writes to them while managed code runs.

### Reading

- **REQ-003-07** While open, exactly one overlapped read MUST be pending, and it MUST be re-armed as
  soon as a report is handled.
- **REQ-003-08** The message loop MUST wait on the read and write events (D3), which are manual reset.
- **REQ-003-09** Unknown reports MUST be ignored. Parsing MUST be pure and allocation-free (D11).
- **REQ-003-10** A read or write error other than a cancellation MUST close the transport, which stays
  closed until the next discovery. It MUST NOT retry in a loop.

### Commands

- **REQ-003-11** At most one command may be in flight. `Core/` MUST hold that state (D7).
- **REQ-003-12** A command is done when a report with its header and command ID arrives. Responses
  are matched by content (D11).
- **REQ-003-13** A one-shot `SetTimer` MUST set a 500 ms deadline, existing only while a command is in
  flight (D12).
- **REQ-003-14** The deadline handler MUST first check for completions without blocking, and only
  then declare a timeout (D4). A timed-out write MUST be cancelled with `CancelIoEx`.
- **REQ-003-15** After a timeout the transport MUST stay open and the read MUST stay pending.

### Lifetime

- **REQ-003-16** After discovery selects a dongle, the app MUST open it and send one battery query.
- **REQ-003-17** When the dongle leaves, and on shutdown, the app MUST cancel I/O, wait for the
  cancellation to complete, then free buffers and close handles.

### Display

- **REQ-003-18** The tooltip MUST show the raw result, `CloudStatus: 87 % (34 ms)`, or that there was
  no response. This text is temporary: 005 replaces it.

### Policy

- **REQ-003-19** `Test-ForbiddenApis.ps1` MUST allow `ReadFile`, `GENERIC_READ` and `GENERIC_WRITE`
  only in `Native/Kernel32.cs` and `Device/HidTransport.cs`.

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| Command latency | First real measurement, ≤ 100 ms typical, 500 ms timeout | AC-003-11 |
| Blocking the UI thread | Never | AC-003-12 |
| CPU at rest | 0 % with a read pending | AC-003-13 |
| Threads | Unchanged, 2 at rest | AC-003-13 |
| Wake-ups | Only the query at connection, plus reports the device sends | AC-003-13 |
| Handles and memory | Stable across reconnections | AC-003-14 |

## Acceptance criteria

- **AC-003-01** `[auto]` Given `Commands.cs`, when a test reads it, then the only report is
  `06 FF BB 02 00`, and it has no command `0x01`.
- **AC-003-02** `[auto]` Given a report and an output length, when it is padded, then the result has
  that length with zeros; a length shorter than the report is refused.
- **AC-003-03** `[auto]` Given a valid battery response, when it is parsed, then byte 7 is the percent.
- **AC-003-04** `[auto]` Given a short report, a wrong header, another command, or a percent above
  100, when it is parsed, then it gives no battery value and does not throw.
- **AC-003-05** `[auto]` Given a command in flight, when a second one is requested, then it is refused.
- **AC-003-06** `[auto]` Given a command in flight, when its response arrives before the deadline,
  then it completes with the elapsed time and the deadline is cleared.
- **AC-003-07** `[auto]` Given a command in flight, when the deadline passes with no response, then
  it ends as a timeout and another command can start.
- **AC-003-08** `[auto]` Given `ReadFile` outside the allowed files, when the policy script runs, then
  it exits non-zero.
- **AC-003-09** `[hw]` Given the headset on, when the app starts, then the tooltip shows the same
  battery percent as NGENUITY or a later reading, within 5 points.
- **AC-003-10** `[hw]` Given the headset off, when the app starts, then the tooltip shows no response
  after 500 ms and the app stays responsive. Note what it shows: this informs OQ-03.
- **AC-003-11** `[hw]` Given 10 connections, when the latency in the tooltip is noted, then the
  typical value is at most 100 ms. Note the values in PERFORMANCE.md: this answers OQ-04.
- **AC-003-12** `[hw]` Given the tray menu held open while the dongle is plugged in, when the menu
  closes, then the result appears correctly and no false timeout is shown (D4).
- **AC-003-13** `[hw]` Given the app at rest after its query, when a 2-minute WPR trace runs, then it
  has no context switches, except for reports the device sends. Note those: they inform OQ-02.
- **AC-003-14** `[hw]` Given the dongle replugged 20 times, then handles return to their starting
  value, with 2 threads and at most 5 MB.
- **AC-003-15** `[hw]` Given audio playing and the microphone **muted**, when the query runs, then the
  mute state does not change and there is no click or dropout. This answers OQ-09 for one query.
- **AC-003-16** `[hw]` Given NGENUITY open, when the query runs, then NGENUITY keeps working.

## Open questions

- **OQ-05** is answered by this spec's first run. If `WriteFile` fails on `COL03`, stop and propose a
  spec change: no other write path is approved.

## Status history

- 2026-10-30: Draft.
- 2026-10-30: Approved, including sending the battery query to the headset.
- 2026-10-30: Implemented, `[hw]` criteria pending.

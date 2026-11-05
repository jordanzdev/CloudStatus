# 008: Suspend and resume

**Status:** Implemented

## Context

Spec 003 keeps a read pending on the dongle at all times, and 004 polls on a timer. Across sleep,
neither is safe: a handle opened before sleep may point to a device that Windows enumerated again on
resume, a pending read may fail late or never complete, and a coalescable timer that expired during
sleep fires right after resume. D13 already decides the approach: close everything on suspend and
take the plug-in path again on resume.

Windows tells top-level windows about sleep with `WM_POWERBROADCAST`. The hidden main window (001)
is top-level for `TaskbarCreated`, so it receives it with no registration. `PBT_APMSUSPEND` arrives
before sleep, and `PBT_APMRESUMEAUTOMATIC` arrives on every resume. `PBT_APMRESUMESUSPEND` follows it
only when the resume was caused by a person, so it adds nothing.

The maintainer's PC supports S3 sleep only (`powercfg /a`, 2026-11-05): no Modern Standby and no
hibernation. That is what can be tested.

## Goals

- After sleep, the icon shows the right state without unplugging the dongle or restarting the app.
- Nothing runs while the PC sleeps or goes to sleep, and resume costs what a plug-in costs.
- No new threads, timers or APIs beyond one window message.

## Non-goals

- Modern Standby (S0 low power idle). Nobody on the project can test it. CloudStatus SHOULD behave
  the same if Windows sends the same messages, but this spec does not promise it.
- Hibernation and Fast Startup. They are disabled on the test PC; Fast Startup ends the app at
  shutdown anyway.
- Recovering from a transport failure that comes with no device or power event. That stays as in 003:
  replugging the dongle recovers.
- Retrying on a timer when the dongle is not back yet at resume. The arrival notification covers it.
- Anything related to the headset's own sleep or auto shutdown. That is the headset being off (004).

## Requirements

### Suspend

- **REQ-008-01** On `PBT_APMSUSPEND` the app MUST cancel any pending I/O, close the device handle,
  and stop the polling and deadline timers, as when the dongle is removed. The device notification
  registration (002) MUST stay, so that resume needs no new registration.
- **REQ-008-02** While suspended, device arrivals and removals MUST be ignored: nothing is
  enumerated or opened before the resume message.
- **REQ-008-03** Handling the suspend message MUST NOT block. It only closes handles and kills
  timers, well inside the time Windows gives.

### Resume

- **REQ-008-04** On `PBT_APMRESUMEAUTOMATIC` the app MUST search for the dongle again and open the one
  found as a new connection, even if its path is the same as before. Then it follows the plug-in path
  of 004: *Checking*, the battery query, and the charging query.
- **REQ-008-05** `PBT_APMRESUMESUSPEND` MUST be ignored, so that one resume reconnects once.
- **REQ-008-06** A resume with no suspend before it (a missed message) MUST also reconnect, so that a
  stale handle is never kept.
- **REQ-008-07** If the dongle is not found at resume, the app MUST show *No dongle* and wait for its
  arrival notification, with no timer.
- **REQ-008-08** Resume MUST NOT rearm the low-battery notice: like replugging the dongle (006), the
  battery has not recovered.
- **REQ-008-09** Resume MUST send only the battery and charging queries already approved (004). No
  new outbound report is added.

### Logic

- **REQ-008-10** Whether to ignore device events, and what to do on each power message, MUST be
  decided by pure code in `Core/`, so every sequence can be tested.

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| CPU at rest | Unchanged, 0 % | AC-008-11 |
| Wake-ups | None while asleep; after resume, one search and two queries, as a plug-in | AC-008-10, AC-008-11 |
| Threads | Unchanged | AC-008-12 |
| Handles, GDI and memory | Back to the same values after each sleep | AC-008-12 |
| Exe size | Grows slightly, CI enforces it | CI |
| Forbidden APIs | None added: one window message | CI |

## Acceptance criteria

- **AC-008-01** `[auto]` Given a connected dongle, when the suspend message arrives, then the action
  is to disconnect, and the state is suspended.
- **AC-008-02** `[auto]` Given the suspended state, when a device arrival or removal arrives, then it
  is ignored.
- **AC-008-03** `[auto]` Given the suspended state, when `PBT_APMRESUMEAUTOMATIC` arrives, then the
  action is to search and reconnect, and device events are handled again.
- **AC-008-04** `[auto]` Given a resume already handled, when `PBT_APMRESUMESUSPEND` arrives, then
  nothing happens.
- **AC-008-05** `[auto]` Given no suspend before, when `PBT_APMRESUMEAUTOMATIC` arrives, then the
  action is still to search and reconnect.
- **AC-008-06** `[auto]` Given two suspend messages in a row, when the second arrives, then nothing
  more happens.
- **AC-008-07** `[hw]` Given the headset on, when the PC sleeps for at least one minute and wakes up,
  then within 2 s of the desktop appearing the icon shows the battery, with no replug.
- **AC-008-08** `[hw]` Given the dongle removed while the PC sleeps, when it wakes up, then the icon
  shows *No dongle*; plugging it in then shows the battery.
- **AC-008-09** `[hw]` Given the headset turned off while the PC sleeps, when it wakes up, then the
  icon shows the headset off.
- **AC-008-10** `[hw]` Given a WPR trace across a sleep, then CloudStatus has no activity between the
  suspend and the resume, and one short burst after resume.
- **AC-008-11** `[hw]` Given 10 minutes after resume with other headset programs closed, then CPU at
  rest is 0 % and wake-ups are one per polling interval.
- **AC-008-12** `[hw]` Given five sleep and wake cycles, then threads, handles and GDI objects are the
  same as before the first, and memory stays at most 5 MB.
- **AC-008-13** `[hw]` Given a low-battery notice already shown, when the PC sleeps and wakes with
  the battery still low, then the notice does not appear again.

## Open questions

None block this spec.

## Status history

- 2026-11-05: Draft.
- 2026-11-05: Approved.
- 2026-11-05: Implemented, `[hw]` criteria pending.

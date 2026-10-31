# 004: Status: battery, charging, on/off and push events

**Status:** Implemented

## Context

The transport (003) can ask for the battery once. This spec keeps the headset's state up to date.
A passive listen on the target hardware settled the design (OQ-02,
[protocol.md](../docs/protocol.md#push-events)): charging and power are pushed, and battery is not.
So events drive everything (D5) and the only timer is the battery poll.

## Goals

- Battery, charging and on/off, always current, shown in the tooltip.
- With the headset off or no dongle, no timer at all.
- All decisions in a pure state machine in `Core/` (D7).

## Non-goals

- The percentage icon and colors (005).
- Low-battery notice (006).
- A configurable interval (007): 5 minutes is a constant until then.
- Mute (012). Reports `0x20` are ignored here.
- Sending `0x01`, ever. Power state comes only from push events and from query results.

## Requirements

### Commands

- **REQ-004-01** `Device/Commands.cs` MUST add the HP charging query, `06 FF BB 03 00`, citing
  protocol.md section 6. The set is then exactly the battery and charging queries.

### Parsing

- **REQ-004-02** The parser MUST read charging from `0x03`: `0` not charging, `1` charging, `2` full,
  anything else error.
- **REQ-004-03** The parser MUST read power from `0x01`: `1` or `4` on, `3` off. Any other value MUST
  count as off, because the headset is not usable (`2` is pairing).
- **REQ-004-04** Reports with other commands MUST be ignored.

### State machine

- **REQ-004-05** `Core/` MUST hold a state machine with the states *No dongle*, *Checking*, *On* and
  *Off*. It takes events and the time, and returns what to do. It calls nothing.
- **REQ-004-06** When a dongle connects, it MUST go to *Checking* and refresh.
- **REQ-004-07** A refresh is a battery query followed by a charging query, one at a time.
- **REQ-004-08** In *Checking*, a valid battery above 0 % MUST go to *On*. A timeout, or a battery of
  0 %, MUST go to *Off*. This covers both possible answers to OQ-03.
- **REQ-004-09** A power-off push MUST go to *Off* at once, keeping the last battery value, and stop
  polling.
- **REQ-004-10** A power-on push MUST go to *Checking* and refresh.
- **REQ-004-11** A charging push MUST update the charging state at once, in *On*, without a query.
- **REQ-004-12** Two poll timeouts in a row in *On* MUST go to *Off*, in case a push was missed.
- **REQ-004-13** Reports answering another program's query MUST be accepted as valid data (D11).
- **REQ-004-14** A failed transport MUST go to *No dongle* until the next discovery.

### Polling

- **REQ-004-15** In *On*, the app MUST poll battery only, every 5 minutes, with
  `SetCoalescableTimer` and a tolerance of 30 s (D12).
- **REQ-004-16** In every other state the poll timer MUST NOT exist. With the headset off the app
  waits for the power-on push.

### Display

- **REQ-004-17** The tooltip MUST show `87 %`, `87 %, charging`, `87 %, full`, `Headset off` or `No
  dongle`, after `CloudStatus: `, in English and Spanish, and change only when its text changes. This
  replaces the temporary text of REQ-003-18.
- **REQ-004-18** A charge error MUST show as *charging error*, in the same way.

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| Wake-ups | One per 5-minute interval in *On*, plus device events. None when off or with no dongle | AC-004-16, AC-004-17 |
| CPU at rest | 0 % between polls | AC-004-16 |
| Threads, memory, handles | Unchanged | AC-004-18 |
| Allocations | None when a report does not change the text; the tooltip allocates when it does | Code review |
| Audio | A poll causes no click or dropout | AC-004-19 |

## Acceptance criteria

- **AC-004-01** `[auto]` Given `Commands.cs`, when a test reads it, then it holds exactly the two
  queries and no command `0x01`.
- **AC-004-02** `[auto]` Given the observed reports in protocol.md, when parsed, then they give the
  right charging and power values.
- **AC-004-03** `[auto]` Given a report with an unknown power or charging value, when parsed, then it
  gives off and error, respectively, and does not throw.
- **AC-004-04** `[auto]` Given a dongle connecting, when the machine runs, then it asks for battery,
  then charging, never both at once.
- **AC-004-05** `[auto]` Given *Checking*, when a battery above 0 % arrives, then it goes to *On*.
- **AC-004-06** `[auto]` Given *Checking*, when a timeout or a battery of 0 % arrives, then it goes to
  *Off*, with no poll timer.
- **AC-004-07** `[auto]` Given *On*, when a power-off push arrives, then it goes to *Off*, keeps the
  last battery and cancels the poll.
- **AC-004-08** `[auto]` Given *Off*, when a power-on push arrives, then it refreshes.
- **AC-004-09** `[auto]` Given *On*, when a charging push arrives, then the state updates with no
  query.
- **AC-004-10** `[auto]` Given *On*, when one poll times out and the next answers, then it stays *On*;
  with two timeouts in a row, it goes to *Off*.
- **AC-004-11** `[auto]` Given *On*, when a poll is due, then only the battery is queried.
- **AC-004-12** `[auto]` Given a transport failure, when it is reported, then it goes to *No dongle*
  with no timers.
- **AC-004-13** `[hw]` Given the headset on, when the cable is plugged in and unplugged, then the
  tooltip follows within 1 s.
- **AC-004-14** `[hw]` Given the app running, when the headset is turned off and on, then the tooltip
  follows within 1 s.
- **AC-004-15** `[hw]` Given the app starts with the headset off, then it shows *Headset off*, and
  turning it on shows the battery.
- **AC-004-16** `[hw]` Given the headset on and other headset programs closed, when a 12-minute WPR
  trace runs, then there are at most 3 bursts of activity.
- **AC-004-17** `[hw]` Given the headset off, or no dongle, when a 2-minute WPR trace runs, then the
  app has no context switches.
- **AC-004-18** `[hw]` Given the soak test in PERFORMANCE.md, then it passes.
- **AC-004-19** `[hw]` Given a CS2 match, when polls happen, then there is no click or dropout and
  the mute state never changes.
- **AC-004-20** `[hw]` Given a full charge, when it finishes, then the tooltip shows *full*. Note
  whether a report arrived: value `2` has not been seen on this hardware.

## Open questions

- **OQ-03** does not block this spec: REQ-004-08 handles both possible answers. AC-004-15 confirms it.

## Status history

- 2026-10-30: Draft.
- 2026-10-30: Approved, including sending the charging query to the headset.
- 2026-10-30: Implemented, `[hw]` criteria pending.

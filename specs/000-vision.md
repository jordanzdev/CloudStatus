# 000: Vision

**Status:** Approved

## Problem

Checking the battery of a HyperX Cloud II Wireless on the PC means opening NGENUITY, a full
application for what is one number. Nothing shows it at a glance, so the headset can die in the
middle of a match.

Existing tools solve this at a cost that does not fit competitive play: they depend on runtimes,
poll on a timer whether or not a dongle is present, or block a thread on every read.

## Users

The primary user is a person playing Counter-Strike 2 competitively with this headset on Windows
10 or 11, who wants to glance at the taskbar and know the battery state, and who will not accept
any risk to game performance or to anti-cheat standing.

## What CloudStatus is

A tray app that shows the state of the headset:

- Battery percentage.
- Whether it is charging.
- Whether the headset is on.
- Firmware version, if the protocol allows it (OQ-06, OQ-07).

## Pillars, in priority order

1. **Minimal impact on the PC.** No added latency, 0 % CPU at rest, minimal memory. See
   [PERFORMANCE.md](../docs/PERFORMANCE.md).
2. **Same conventions as [CursorLock](https://github.com/jordanzdev/CursorLock).**
3. **Spec Driven Development.** See [README.md](README.md).

## Scope for 1.0

- HP dongle `0x03F0:0x018B` as the target, and `0x03F0:0x0696` as a compatible untested ID.
- Tray icon with the percentage and colors, plus a tooltip.
- Low-battery notice.
- Settings stored in `HKCU`, in English and Spanish.
- Per-user MSI, portable exe and a manual "Check for updates".

## Non-goals

- **Kingston dongles in 1.0.** Their packets are documented, but the interface to use is unknown
  (OQ-01) and nobody on the project can test one.
- **Changing anything on the headset**: no sidetone, no auto shutdown, no EQ, no mute control.
  CloudStatus is read-only as far as the headset's settings go.
- **Flashing firmware**, or checking online for a newer version.
- **Replacing NGENUITY.** The two must coexist.
- **Other headsets, other operating systems.**
- **Telemetry** or any network access besides the manual update check.
- **Touching audio**: no Core Audio, no audio endpoints.

## Success metrics

| Metric | Target | How it is measured |
|---|---|---|
| Resource budgets | All met on hardware | [PERFORMANCE.md](../docs/PERFORMANCE.md), per version |
| Correctness of the battery value | Matches NGENUITY within 5 points | Side-by-side `[hw]` check |
| Surprise side effects | None: no unmute, no audio dropout, no click | `[hw]` check during a real session |
| Time for a state change to show | Under 1 s for pushed events; one polling interval otherwise | `[hw]` check |
| Coexistence | NGENUITY keeps working with CloudStatus running | `[hw]` check |
| Anti-cheat review | The source contains none of the forbidden APIs | CI |

## Status history

- 2026-10-30: Draft.
- 2026-10-30: Approved.

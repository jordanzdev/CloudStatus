# 005: Tray icon with percentage and colors

**Status:** Implemented

## Context

Spec 004 keeps the headset's state current, but it is only visible by hovering over the icon. This
spec shows it at a glance, which is the reason CloudStatus exists. With it, the portable exe is
usable as version 0.1.0 ([ROADMAP.md](ROADMAP.md#release-plan)).

The icon is drawn in memory and redrawn only when what it shows changes (D14). What to show is
decided by pure code in `Core/`; drawing is a thin layer in `Ui/`.

## Goals

- Read the battery in the taskbar without hovering, on any DPI and on light or dark taskbars.
- Tell apart low battery, charging, headset off and no dongle at a glance.
- No cost when nothing changes, and no GDI leaks.

## Non-goals

- Low-battery notice (006).
- Configurable colors or thresholds (007): they are constants until then.
- Animations. Anything that redraws on a timer breaks the wake-up budget.
- Reading the Windows theme: the icon has its own background, so it works on both.

## Requirements

### What is shown

- **REQ-005-01** `Core/` MUST turn the headset status into an icon description: its text and its
  style. It MUST be pure and allocation-free.
- **REQ-005-02** With the headset on, the text MUST be the battery number, without the `%` sign,
  which does not fit at 16 px.
- **REQ-005-03** The style MUST be:

  | State | Text | Background |
  |---|---|---|
  | Charging or full | Battery | Teal `#26C6A6`, the logo's accent |
  | On, 50 % or more | Battery | Dark `#2B313D` |
  | On, 20–49 % | Battery | Amber `#F9A825` |
  | On, under 20 % | Battery | Red `#E53935` |
  | Charging error | Battery | Red `#E53935` |
  | Checking | None | Dark `#2B313D` |
  | Headset off | None | Grey `#6B7280` |
  | No dongle | None | Grey `#6B7280`, hollow |

- **REQ-005-04** Text MUST use the color with the higher contrast against its background, black or
  white.
- **REQ-005-05** The 20 % and 50 % limits MUST be named constants, so that 007 can replace the first.

### Drawing

- **REQ-005-06** The icon MUST be a rounded square drawn into a 32 bpp DIB at the small-icon size for
  the current DPI, with no icon files, as in CursorLock.
- **REQ-005-07** Text MUST be drawn with GDI in the bold system font, on a separate mask, and used as
  alpha coverage, so that the DIB's alpha stays correct.
- **REQ-005-08** The font MUST shrink until the text fits inside the square, so that `100` fits at
  every size.
- **REQ-005-09** The app logo remains the icon of the exe, and of windows from 007 onwards.

### Lifetime

- **REQ-005-10** The icon MUST be redrawn only when its description or DPI changes, or when Explorer
  restarts.
- **REQ-005-11** Only the current `HICON` may be kept. The previous one MUST be destroyed after the
  shell has the new one, and every bitmap, DC and font MUST be released on every path.
- **REQ-005-12** Drawing failure MUST keep the previous icon rather than remove it.

### Tooltip

- **REQ-005-13** The tooltip of 004 stays, and it remains the only place that spells out the state.

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| CPU at rest | 0 %: the icon is not touched between changes | AC-005-14 |
| Wake-ups | Unchanged | AC-005-14 |
| GDI objects | Stable, after many redraws | AC-005-15 |
| Handles and memory | Stable, at most 5 MB | AC-005-15 |
| Exe size | Grows slightly, CI enforces it | CI |
| Allocations | None when nothing changed | AC-005-07, code review |

## Acceptance criteria

- **AC-005-01** `[auto]` Given each state in REQ-005-03, when the description is built, then it has the
  text and background in the table.
- **AC-005-02** `[auto]` Given batteries of 19, 20, 49 and 50 %, when described, then they fall on
  the right side of each limit.
- **AC-005-03** `[auto]` Given charging at 10 %, when described, then the style is the charging one,
  not the low one.
- **AC-005-04** `[auto]` Given a charging error, when described, then the style is red.
- **AC-005-05** `[auto]` Given every background, when text color is chosen, then its contrast with the
  background is at least 4.5:1.
- **AC-005-06** `[auto]` Given 0 and 100 %, when described, then the text is `0` and `100`.
- **AC-005-07** `[auto]` Given two identical statuses, when described, then the descriptions are equal,
  so nothing is redrawn.
- **AC-005-08** `[hw]` Given the headset on, then the taskbar shows the same number as the tooltip, and
  it is readable at 100 %, 125 %, 150 % and 200 % scaling. No headset state needs changing.
- **AC-005-09** `[hw]` Given light and dark taskbars, then the icon is readable on both.
- **AC-005-10** `[hw]` Given the charging cable plugged in and unplugged, then the background follows
  within 1 s.
- **AC-005-11** `[hw]` Given the headset turned off and on, and the dongle removed and replugged, then
  each state in REQ-005-03 appears.
- **AC-005-12** `[hw]` Given the battery crossing 50 % and 20 %, then the background changes. This
  happens with normal use, within one polling interval.
- **AC-005-13** `[hw]` Given Explorer restarted, then the icon comes back with the current state.
- **AC-005-14** `[hw]` Given other headset programs closed, when a 12-minute WPR trace runs, then
  there are at most 3 bursts of activity.
- **AC-005-15** `[hw]` Given the headset turned off and on 20 times, then GDI objects and handles
  return to their starting values, with 2 threads and at most 5 MB.

## Open questions

None block this spec.

## Status history

- 2026-10-31: Draft.
- 2026-10-31: Approved.
- 2026-10-31: Implemented, `[hw]` criteria pending.

# 006: Low-battery notice

**Status:** Implemented

## Context

Spec 005 turns the icon red under 20 %, but only someone looking at the taskbar sees it. This spec
adds a Windows notice when the battery crosses a low limit, so the headset does not die unnoticed.
It is one of the 1.0 items in [000-vision.md](000-vision.md#scope-for-10).

The notice is the same mechanism CursorLock uses for its warnings: a balloon on the existing tray
icon (`Shell_NotifyIconW` with `NIF_INFO`), which Windows 10 and 11 show as a notification. It adds
no API, thread, timer or window. Whether to notify is decided by pure code in `Core/`, fed by the
state changes that spec 004 already produces.

**Notices and games.** Windows decides when a notification is shown. With Focus Assist (Windows 10)
or Do Not Disturb (Windows 11) active, including its default automatic rule for full-screen games,
the notice goes straight to the notification center without appearing over the game. CloudStatus
accepts that: drawing over a game would be an overlay, which the project forbids. During a match the
red icon is what remains, and the notice is waiting when the match ends.

## Goals

- Know that the battery is low without watching the icon.
- One notice per limit and discharge, never a stream of them.
- Never disturb a game: no focus change, no sound, nothing drawn over it.
- No cost: no timers or wake-ups of its own.

## Non-goals

- Turning the notice off, or changing its limits (007). Until then they are constants.
- A notice for the charging error. The icon already turns red for it (005).
- A notice when charging ends or the battery is full.
- Any action when the notice is clicked.
- Getting around Focus Assist or Do Not Disturb, or showing anything over a full-screen app.
- A sound of its own, or any audio. See REQ-006-09.

## Requirements

### When to notify

- **REQ-006-01** `Core/` MUST decide whether to notify from the power, battery and charge state. It
  MUST be pure and allocation-free, and it MUST be evaluated only when that state changes.
- **REQ-006-02** There MUST be two levels:

  | Level | Fires when the battery is | Rearms at |
  |---|---|---|
  | Low | Under 20 % (`IconDescription.LowLimit`, the red icon's limit) | 25 % or more, or charging |
  | Critical | Under 10 % | 15 % or more, or charging |

  Each limit and the 5-point rearm margin MUST be named constants, so that 007 can replace the low
  limit.
- **REQ-006-03** Each level MUST notify at most once until it rearms. The margin keeps a battery that
  wavers around a limit (19, 20, 19…) from notifying again.
- **REQ-006-04** Only one notice MUST be shown per state change: if both levels fire at once, only
  the critical one is shown and both count as notified. This covers starting the app at 8 %, or a
  headset turned on after a long time off.
- **REQ-006-05** Nothing MUST be notified unless the headset is on and not charging. Charging or full
  rearms both levels. A charging error counts as not charging.
- **REQ-006-06** Turning the headset off and on, or removing and replugging the dongle, MUST NOT
  rearm a level: the battery has not recovered. Only the rearm conditions in REQ-006-02 do, or
  restarting the app.
- **REQ-006-07** If the battery is already low when the app starts, the matching notice MUST be shown
  once, as soon as the first battery value arrives.

### How it is shown

- **REQ-006-08** The notice MUST be a balloon on the tray icon (`NIM_MODIFY` with `NIF_INFO`) with the
  warning icon (`NIIF_WARNING`) and `NIIF_RESPECT_QUIET_TIME`, as in CursorLock. No other window, API
  or library is added.
- **REQ-006-09** The notice MUST be silent (`NIIF_NOSOUND`). The headset is the default output device
  during a match, and the icon and the notice are enough.
- **REQ-006-10** The notice MUST NOT take focus, and the app MUST NOT check whether a game is running.
- **REQ-006-11** The title MUST name the level and the text MUST give the percentage, in the
  language chosen by `Ui/Strings.cs`:

  | Level | Title | Text |
  |---|---|---|
  | Low | Batería baja / Low battery | Cascos al N %. Cárgalos pronto. / Headset at N %. Charge it soon. |
  | Critical | Batería muy baja / Battery very low | Cascos al N %. Cárgalos ya. / Headset at N %. Charge it now. |

  Both MUST fit `szInfoTitle` (64) and `szInfo` (256) without truncation.
- **REQ-006-12** If the icon is not in the tray yet (Explorer not ready), the notice MUST be kept and
  shown once the icon is added, as in CursorLock. Only the latest pending notice is kept.
- **REQ-006-13** Restarting Explorer MUST NOT show a notice again.

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| CPU at rest | 0 %: evaluated only on a state change | AC-006-14 |
| Wake-ups | Unchanged: no timer or event of its own | AC-006-14 |
| Threads | Unchanged, 2 | AC-006-15 |
| Handles, GDI and memory | Stable, at most 5 MB | AC-006-15 |
| Exe size | Grows slightly, CI enforces it | CI |
| Allocations | None unless a notice is shown; then only its two strings | AC-006-01, code review |
| Forbidden APIs | None added | CI |

## Acceptance criteria

- **AC-006-01** `[auto]` Given the same state twice, when evaluated, then no notice is returned the
  second time.
- **AC-006-02** `[auto]` Given a headset on and not charging going 21 → 20 → 19 %, when evaluated,
  then the low notice fires at 19 and not before.
- **AC-006-03** `[auto]` Given 19 → 20 → 19 → 24 → 19 %, when evaluated, then the low notice fires
  only once.
- **AC-006-04** `[auto]` Given 19 → 25 → 19 %, when evaluated, then the low notice fires twice.
- **AC-006-05** `[auto]` Given 19 %, then charging, then not charging at 19 %, when evaluated, then
  the low notice fires twice.
- **AC-006-06** `[auto]` Given 21 → 15 → 9 %, when evaluated, then the low notice fires at 15 and the
  critical one at 9.
- **AC-006-07** `[auto]` Given a first battery value of 8 %, when evaluated, then only the critical
  notice fires, and going down to 5 % fires nothing else.
- **AC-006-08** `[auto]` Given a low notice already shown, when the headset turns off and on, or the
  dongle is removed and replugged, at the same battery, then nothing fires again.
- **AC-006-09** `[auto]` Given a low battery while charging, full, off, checking or with no dongle,
  when evaluated, then nothing fires.
- **AC-006-10** `[auto]` Given a low battery with a charging error, when evaluated, then the notice
  fires.
- **AC-006-11** `[hw]` Given the headset on and discharging, when the battery drops under 20 %, then
  one notice appears, in Spanish, silent, with the right percentage. This happens with normal use,
  within one polling interval.
- **AC-006-12** `[hw]` Given a low notice shown, when the charging cable is plugged in and unplugged,
  then the notice appears again only once the battery is low and the cable is out. Turning the
  headset off and on shows nothing.
- **AC-006-13** `[hw]` Given CS2 in full screen with Windows' default game rule, when a notice fires,
  then the game keeps focus, nothing is drawn over it and nothing is heard, and the notice is in the
  notification center afterwards.
- **AC-006-14** `[hw]` Given other headset programs closed, when a 12-minute WPR trace runs, then
  there are still at most 3 bursts of activity.
- **AC-006-15** `[hw]` Given Explorer restarted after a notice, then no notice appears again, and the
  app still has 2 threads and at most 5 MB.
- **AC-006-16** `[hw]` Given Explorer closed and the battery under 20 %, when CloudStatus starts and
  Explorer is started again, then the notice appears once, after the icon.

## Open questions

None block this spec.

## Status history

- 2026-11-05: Draft.
- 2026-11-05: Approved.
- 2026-11-05: Implemented, `[hw]` criteria pending.
- 2026-11-05: REQ-006-08 (warning icon) replaced by [spec 013](013-ui-redesign.md#changes-to-earlier-specs).

# 007: Settings, persistence and start with Windows

**Status:** Implemented

## Context

Until now every behavior is a constant: the low-battery limit (005, 006), the polling interval (004)
and the language, picked from Windows (001). The app also has to be started by hand after each
sign-in. This spec adds a settings window, stores the settings in `HKCU\Software\CloudStatus` (D15)
and adds *Start with Windows* with the `--startup` argument.

Everything follows CursorLock: a plain Win32 window created when opened and destroyed when closed,
the same close confirmation, the same registry layout and the same `Run` entry. The rules that can be
tested (defaults, ranges, fallbacks) live in `Core/`.

## Goals

- Change the few things worth changing without editing the registry.
- Start silently in the tray at sign-in.
- No cost while the window is closed: nothing read, written or kept in memory.

## Non-goals

- Check for updates (010). The window leaves room for its button.
- `--uninstall` and `--create-desktop-shortcut` (009).
- Showing the headset status or firmware in the window. The tooltip covers the status; 011 may add
  the firmware version.
- Changing colors, the 50 % limit or the 10 % critical limit.
- Any setting that writes to the headset. CloudStatus stays read-only (000-vision).
- Settings files, import or export, or settings per user beyond `HKCU`.

## Requirements

### Settings

- **REQ-007-01** The app MUST offer these settings:

  | Setting | Values | Default | Registry value (DWORD) |
  |---|---|---|---|
  | Low-battery notice | On, off | On | `LowBatteryNotice`: 0 or 1 |
  | Low battery limit | 15–45 % | 20 % | `LowBatteryPercent` |
  | Read the battery every | 1–30 minutes | 5 | `PollMinutes` |
  | Language | Automatic (system), Español, English | Automatic | `Language`: 0, 1, 2 |
  | Start with Windows | On, off | Off | `Run` entry, see REQ-007-09 |

  The values match CursorLock where they exist: `Language` uses the same numbers.
- **REQ-007-02** The low battery limit MUST replace the constant of 005 and 006: it sets both where
  the icon turns red and where the low notice fires. Its rearm margin stays at 5 points. The range
  keeps it above the critical limit (10 %) and below the amber one (50 %).
- **REQ-007-03** The polling interval MUST replace the 5-minute constant of 004. It keeps its 30 s
  coalescing tolerance, so the shortest interval wakes the app at most once a minute.
- **REQ-007-04** Turning the notice off MUST stop new notices only. The icon colors stay, and turning
  it back on does not show a notice for a limit already crossed.

### Persistence

- **REQ-007-05** Settings MUST be read once at start-up and written only when saved. Nothing in the
  registry is read or written in between.
- **REQ-007-06** A missing value, a value of the wrong type or a value out of range MUST fall back to
  its default without touching the others. The validation MUST be pure code in `Core/`.
- **REQ-007-07** If the settings cannot be saved, the window MUST say so and keep the previous
  settings in use.

### Window

- **REQ-007-08** The window MUST open from a new tray menu item *Settings…* (*Ajustes…*), placed above
  *Exit*, and by double-clicking the icon or pressing Enter on it. If it is already open, it is brought
  to the front.
- **REQ-007-09** Starting the app by hand MUST show the window. Starting it with `--startup` MUST NOT:
  the app stays in the tray. A second instance still exits silently, as in 001.
- **REQ-007-10** The window MUST be plain Win32 with Common Controls v6, use the system message font,
  scale with the monitor's DPI, and work with the keyboard alone (Tab, Enter for *Save*, Esc for
  *Cancel*). Its title MUST show the version.
- **REQ-007-11** *Save* MUST validate, store and apply every setting without restarting, then close
  the window. An invalid number MUST be reported next to its field and nothing is saved.
- **REQ-007-12** *Cancel* and Esc MUST close the window without saving or asking. The X button MUST
  ask, as in CursorLock: *Minimize to tray* (the app keeps running) or *Close CloudStatus*, with a
  task dialog and a message box as fallback.
- **REQ-007-13** The window MUST be created when opened and destroyed when closed, releasing its font,
  controls and icons. No control is kept while it is closed.

### Applying

- **REQ-007-14** A new language MUST apply at once to the tooltip, the menu and later notices.
- **REQ-007-15** A new low battery limit MUST redraw the icon at once and be evaluated by the
  low-battery notice at once. Raising it above the current battery therefore shows the notice, which
  also gives a way to check 006 without draining the battery.
- **REQ-007-16** A new polling interval MUST restart the polling timer, if it is running, with the new
  interval. No query is sent because of the change.

### Start with Windows

- **REQ-007-17** *Start with Windows* MUST write `"<exe path>" --startup` to the `CloudStatus` value of
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, and remove it when turned off. Its state is
  read from that value, not stored elsewhere.
- **REQ-007-18** On every start, if the entry exists but points to another path or lacks `--startup`,
  the app MUST rewrite it, as CursorLock does after the exe is moved.
- **REQ-007-19** Nothing MUST need administrator rights: only `HKCU` is written.

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| CPU at rest | 0 % with the window closed | AC-007-20 |
| Wake-ups | One per polling interval, now configurable: 1 to 30 minutes | AC-007-20 |
| Threads | None created by our code. 2 at rest until the window is first opened; after that, up to 3 more idle Windows thread-pool workers ([D20](../docs/ARCHITECTURE.md#d20-runtime-threads-are-accepted)) | AC-007-21 |
| Memory | At most 5 MB with the window closed, after opening it at least once | AC-007-21 |
| Handles, GDI and USER objects | A one-time cost on the first opening; no growth on later openings | AC-007-21 |
| Start to icon | Unchanged, at most 300 ms: reading settings is a few registry values | AC-007-22 |
| Exe size | Grows; CI keeps it under 1.5 MB | CI |
| Registry access | Only at start-up and on save | Code review |
| Forbidden APIs | None added; registry, window and task dialog APIs only | CI |

The 5 MB budget is measured with the window closed. While it is open, Common Controls and its
controls add memory; that is an explicit user action, like D16.

**One-time cost of the first opening.** Showing any window with controls makes Windows load
libraries that stay loaded after it closes: `comctl32.dll`, `textinputframework.dll` (text input,
active for any focused window), `WindowsCodecs.dll` and `IconCodecService.dll` (loading the logo) and
`OLEAUT32.dll`. Measured on 2026-11-05 after 20 openings: GDI 8 → 16, USER 6 → 12, handles 175 →
194, private memory 3.33 → 3.65 MB, and up to 3 Windows thread-pool workers (`ntdll` worker start
address) waiting with no CPU time. The same numbers came out with the number fields replaced by
lists, so they do not depend on a particular control, and CursorLock behaves the same. The first
opening pays it; later openings add nothing.

## Acceptance criteria

- **AC-007-01** `[auto]` Given no stored values, when settings are loaded, then every setting has the
  default in REQ-007-01.
- **AC-007-02** `[auto]` Given each limit at 14, 15, 45 and 46 %, and intervals of 0, 1, 30 and 31
  minutes, when validated, then only the in-range values are kept.
- **AC-007-03** `[auto]` Given a language of 3 and a notice flag of 2, when validated, then both fall
  back to their defaults and the other values are kept.
- **AC-007-04** `[auto]` Given a low limit of 30 %, when the icon is described at 29 and 30 %, then it
  is red at 29 and amber at 30.
- **AC-007-05** `[auto]` Given a low limit of 30 %, when the battery goes 31 → 29 %, then the low
  notice fires at 29.
- **AC-007-06** `[auto]` Given the limit raised from 20 to 30 % at 25 %, when evaluated again, then
  the low notice fires; and given it lowered back to 20 %, then nothing fires.
- **AC-007-07** `[auto]` Given the notice turned off, when the battery crosses the limit and the
  notice is turned on again, then nothing fires until the level rearms.
- **AC-007-08** `[auto]` Given the exe path and `--startup`, when the startup command is built and
  compared, then a moved exe or a missing argument counts as different, and case does not.
- **AC-007-09** `[auto]` Given the app started with `--startup`, then no window is shown and the icon
  is there (smoke test). Started without it, the settings window appears.
- **AC-007-10** `[hw]` Given the window open, when each setting is changed and saved, then it applies
  without restarting and is still there after exiting and starting again.
- **AC-007-11** `[hw]` Given the low limit set to 5 points above the current battery and saved, then
  the icon turns red and one low notice appears.
- **AC-007-12** `[hw]` Given an interval of 1 minute, when a 5-minute WPR trace runs with other headset
  programs closed, then there are about 5 bursts; back at 5 minutes, at most 3 in 12 minutes.
- **AC-007-13** `[hw]` Given *Español* and then *English* saved, then the tooltip, the menu and the
  window follow at once.
- **AC-007-14** `[hw]` Given *Start with Windows* on, when signing out and in, then the app starts in
  the tray without a window. Turned off, it does not start.
- **AC-007-15** `[hw]` Given *Start with Windows* on and the exe moved, when it is started from the
  new place, then the `Run` entry points to the new path.
- **AC-007-16** `[hw]` Given the menu item, a double-click on the icon, and Enter on the icon selected
  with Win+B, then each opens the window, or brings it to the front if it is open. In the window, Tab
  moves through every control, Enter saves and Esc cancels.
- **AC-007-17** `[hw]` Given the X button, then the question appears, and each answer does what it
  says.
- **AC-007-18** `[hw]` Given the window at 100 %, 150 % and 200 % scaling, and moved between monitors
  with different scaling, then it stays readable and nothing is cut off.
- **AC-007-19** `[hw]` Given `LowBatteryPercent` set to 99 and `Language` to a string by hand, when the
  app starts, then those two use their defaults and the rest are kept.
- **AC-007-20** `[hw]` Given the window closed, then CPU at rest is 0 % and wake-ups are one per
  polling interval.
- **AC-007-21** `[hw]` Given the window opened and closed once, then twenty more times, then handles,
  GDI and USER objects after the twenty are the same as after the first, memory stays at most 5 MB,
  CPU at rest is 0 %, and every thread beyond the 2 at rest starts in `ntdll` (the Windows thread
  pool), not in `CloudStatus.exe`.
- **AC-007-22** `[hw]` Given `--startup`, then the icon appears within 300 ms of the process start
  (median of five).
- **AC-007-23** `[hw]` Given `HKCU\Software\CloudStatus` made read-only for the user in regedit, when
  saving, then an error is shown and the previous settings stay in use.

## Open questions

None block this spec.

## Status history

- 2026-11-05: Draft.
- 2026-11-05: Approved.
- 2026-11-05: Amended and approved: the first opening has a one-time cost and Windows thread-pool
  workers are accepted after it (AC-007-21, budget table, D20).
- 2026-11-05: Implemented, `[hw]` criteria pending.

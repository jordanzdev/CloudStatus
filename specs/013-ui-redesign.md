# 013: Windows 11 look for the whole interface

**Status:** Implemented

## Context

Every surface of CloudStatus works, but it looks like Windows 7: the settings window is grey
`COLOR_BTNFACE` with classic check boxes and edit borders, it stays light in dark mode, errors come
in balloons and message boxes, and the tray icon is a dark square that does not match the
monochrome system icons next to it.

This spec gives every surface the look of Windows 11 in light and dark mode, **without leaving
plain Win32** ([D1](../docs/ARCHITECTURE.md#d1-c-on-net-10-with-nativeaot-plain-win32)) and without
raising any budget in [PERFORMANCE.md](../docs/PERFORMANCE.md). It changes how things look and
where messages appear, not what the app does: no setting, query, timer or limit changes.

It builds on 005 (icon), 006 (notice), 007 (window), 009 (installer) and 010 (update check), and
replaces the requirements listed in [Changes to earlier specs](#changes-to-earlier-specs).

## Goals

- Look at home on Windows 11, in light and dark mode, and follow a theme change without restarting.
- A tray icon that reads like a system icon when nothing needs attention, and stands out when
  something does.
- Errors and results shown inside the window, next to what they refer to.
- No cost while nothing is open, and the same budgets as 1.0.

## Non-goals

- WinUI, WPF, XAML Islands, WebView or any NuGet package.
- Mica or acrylic behind the window's content. GDI controls cannot draw on it correctly, so the
  content has a solid background and only the title bar gets Mica.
- Dark task dialogs, message boxes and installer pages: Windows draws them and offers no dark mode
  for them. They keep the system look.
- Animations, including the toggle switches. Anything that redraws on a timer breaks the wake-up
  budget.
- Using the Windows accent color. The accent is the logo's teal.
- Showing the headset status in the settings window (007's non-goal stands; the menu gets a status
  line instead).
- New settings, new menu commands or changes to the logo and the exe icon.

## Requirements

### Theme

- **REQ-013-01** `Core/` MUST decide the theme from three inputs: the apps theme
  (`AppsUseLightTheme`), the taskbar theme (`SystemUsesLightTheme`), both DWORDs in
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`, and whether high contrast is
  on. It MUST be pure. A missing or invalid apps value means light; a missing or invalid taskbar value
  means dark (Windows 10 before 1903 has a dark taskbar and no value).
- **REQ-013-02** The two values MUST be read at start-up, and again only on `WM_SETTINGCHANGE` with
  `ImmersiveColorSet` or a high-contrast change. Nothing polls. These are Windows' values, not the
  app's, so 007's rule that the app's own settings are only read at start-up still holds.
- **REQ-013-03** A theme change MUST apply at once to the open window, the tray icon and the next
  menu, without reopening anything.
- **REQ-013-04** With high contrast on, the window MUST use system colors (`GetSysColor`) for every
  surface, and the tray icon MUST use the plate style of REQ-013-17 for every state.

### Palette

- **REQ-013-05** The window colors MUST be named constants in `Core/`:

  | Token | Light | Dark |
  |---|---|---|
  | Window background | `#F3F3F3` | `#202020` |
  | Card | `#FBFBFB` | `#2B2B2B` |
  | Card border | `#E5E5E5` | `#1D1D1D` |
  | Text | `#1A1A1A` | `#FFFFFF` |
  | Secondary text | `#5D5D5D` | `#C5C5C5` |
  | Accent: toggle on, *Save*, focus line | `#007A66`, white text | `#26C6A6`, black text |
  | Error text | `#C42B1C` | `#FF99A4` |
  | Info bar, warning | `#FFF4CE` | `#433519` |
  | Info bar warning icon | `#9D5D00` | `#FCE100` |
  | Field background | `#FFFFFF` | `#373737` |
  | Field border | `#E5E5E5` | `#454545` |
  | Field bottom line | `#868686` | `#9A9A9A` |

  A result that is not a warning uses the card color for the info bar and the accent for its icon.

- **REQ-013-06** Text MUST reach 4.5:1 against the surface it is drawn on, and the accent MUST reach
  3:1 against the card, measured with `IconDescription.ContrastRatio`.

### Settings window

- **REQ-013-07** The window MUST set, through `DwmSetWindowAttribute`: dark title bar
  (`DWMWA_USE_IMMERSIVE_DARK_MODE`) in dark mode, rounded corners (`DWMWCP_ROUND`), and Mica in the
  title bar (`DWMWA_SYSTEMBACKDROP_TYPE` = `DWMSBT_MAINWINDOW`) on build 22621 or later. Which
  attributes apply to a build MUST be decided by pure code, and a failed call MUST be ignored, so
  Windows 10 keeps a working window with a square frame.
- **REQ-013-08** The content MUST be laid out as Windows 11 settings pages:

  ```
  Battery                                      (section heading, semibold)
  ┌──────────────────────────────────────────┐
  │ Low-battery notice                 [ ●]  │
  │ Low battery below            [ 20 ] %    │
  │ 15–45 %                                  │ (secondary text)
  └──────────────────────────────────────────┘
  General
  ┌──────────────────────────────────────────┐
  │ Read the battery every       [  5 ] min  │
  │ 1–30 min                                 │
  │ Language             [Automatic     ▾]   │
  │ Start with Windows                 [● ]  │
  └──────────────────────────────────────────┘
  [Check for updates]           [Save] [Cancel]
  ```

  Cards have rounded corners and a 1 px border; rows have the label on the left and the control on
  the right. The title bar keeps the version, as in 007.
- **REQ-013-09** The two on/off settings MUST be toggle switches. They MUST stay check-box controls
  (`BS_AUTOCHECKBOX`) drawn through `NM_CUSTOMDRAW`, so Space, Tab, `BM_GETCHECK` and screen readers
  keep working. The control's text MUST stay the row label, which screen readers read; the switch
  does not draw it.
- **REQ-013-10** The number fields and the language list MUST take the theme colors through
  `WM_CTLCOLOREDIT`, `WM_CTLCOLORLISTBOX` and `SetWindowTheme`. The window MUST draw the number
  fields' rounded border, with an accent line under the field that has the focus. The language list
  keeps the border its theme draws (`DarkMode_CFD` in dark mode).
- **REQ-013-11** The buttons MUST be drawn through `NM_CUSTOMDRAW` with rounded corners: *Save* filled
  with the accent, the others with the card color and a border. Pressed and disabled states MUST be
  visible, and so MUST a focus ring whenever Windows shows keyboard cues.
- **REQ-013-12** An invalid number MUST be reported in error text under its row, and the field
  selected, instead of the balloon of 007. The text MUST disappear once the field is edited.
- **REQ-013-13** Messages that are not questions MUST appear in an info bar at the top of the
  window, which grows to make room and shrinks when it is dismissed: settings not saved, *Start with
  Windows* not changed (007), and every update-check result that needs no answer (010: up to date,
  no release yet, check failed, download or integrity failure, launch failure). Questions (download
  the update?, open the release page?) stay message boxes.
- **REQ-013-14** Every brush, pen and font the window uses MUST be created when it opens or when the
  theme or DPI changes, replacing the previous set, and released when it closes. Nothing is kept
  while it is closed (007, REQ-007-13).
- **REQ-013-15** Keyboard behavior (Tab order, Enter saves, Esc cancels), DPI scaling and the close
  question MUST stay as in 007. Corner radii, borders and the focus ring MUST scale with the DPI.
- **REQ-013-16** The close question MUST show the app logo as its main icon and a one-line
  description under each choice. It stays a task dialog, with the message box as fallback.

### Tray icon

- **REQ-013-17** The icon description of 005 MUST take the taskbar theme and become:

  | State | Shape | Color |
  |---|---|---|
  | On, 50 % or more, not charging | Number only, no plate | Taskbar foreground |
  | On, low limit to 49 % | Number on a plate | Amber `#F9A825`, as in 005 |
  | On, under the low limit | Number on a plate | Red `#E53935`, as in 005 |
  | Charging or full | Number on a plate | Teal `#26C6A6`, as in 005 |
  | Charging error | Number on a plate | Red `#E53935`, as in 005 |
  | Checking | Hollow rounded square | Taskbar foreground |
  | Headset off | Headset glyph | Taskbar foreground |
  | No dongle | Headset glyph with a slash | Taskbar foreground |

  The taskbar foreground is white on a dark taskbar and `#1A1A1A` on a light one. Plates keep 005's
  text color rule (black or white, whichever contrasts more).
- **REQ-013-18** The headset glyph MUST be the logo's band and cups, drawn in code with the same
  anti-aliased distance-field method as the square: no icon files.
- **REQ-013-19** 005's lifetime rules stay: redraw only when the description, the DPI or the theme
  changes, or Explorer restarts; one `HICON` kept; the previous icon kept if drawing fails.

### Tooltip and menu

- **REQ-013-20** The tooltip MUST have two lines: `CloudStatus`, then the state, for example
  `78 % · Charging`, `Headset off` or `No dongle`. It MUST fit `szTip` (128) without truncation in
  both languages.
- **REQ-013-21** The menu MUST start with the state line of the tooltip as a disabled item, then a
  separator, *Settings…* as the default (bold) item, a separator and *Exit*.
- **REQ-013-22** The menu MUST follow the apps theme. Windows only offers this through the
  undocumented `uxtheme.dll` exports by ordinal (135 `SetPreferredAppMode`, 136
  `FlushMenuThemes`), so they MUST be called only on build 18362 or later,
  where those ordinals are known, and the menu stays light elsewhere or if a call fails. This needs
  the new decision D21 and a CI rule that keeps ordinal imports in `Native/UxTheme.cs`.

### Low-battery notice

- **REQ-013-23** The notice MUST show the tray icon's own plate, drawn at large-icon size, instead of
  the warning icon: `NIIF_USER | NIIF_LARGE_ICON` with `hBalloonIcon`. The icon MUST be destroyed
  right after the call. Silence, quiet time, focus and texts stay as in 006.

### Installer

- **REQ-013-24** `tools/New-Branding.ps1` MUST regenerate the banner and side bitmaps in the light
  palette of REQ-013-05 with the teal accent, still with no HyperX or HP marks. The installer pages,
  their texts and their behavior stay as in 009; only the bitmaps and the title text style change.

## Changes to earlier specs

On approval, these requirements are replaced, and each spec's status history records it:

| Spec | Requirement | Becomes |
|---|---|---|
| 005 | Non-goal *Reading the Windows theme* | Removed: REQ-013-01 |
| 005 | REQ-005-03 (style table) | REQ-013-17 |
| 005 | REQ-005-06 (always a rounded square) | REQ-013-17, REQ-013-18 |
| 005 | REQ-005-13 (tooltip text) | REQ-013-20 |
| 006 | REQ-006-08 (`NIIF_WARNING`) | REQ-013-23 |
| 007 | REQ-007-10 (look), REQ-007-11 (error next to field) | REQ-013-07 to REQ-013-12 |
| 007 | REQ-007-07 (how a save failure is shown) | REQ-013-13 |
| 009 | REQ-009-09 (bitmaps) | REQ-013-24 |
| 010 | Result message boxes | REQ-013-13 |

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| CPU at rest | 0 %: a theme change is an event, not a poll | AC-013-21 |
| Wake-ups | Unchanged; `WM_SETTINGCHANGE` only arrives when something changes | AC-013-21 |
| Threads | Unchanged | AC-013-22 |
| Memory | At most 5 MB with the window closed, after opening it | AC-013-22 |
| GDI and USER objects | One set while the window is open; back to the 007 values when it closes; no growth over 20 openings or theme switches | AC-013-22 |
| Start to icon | At most 300 ms: two registry values and, on 18362+, one `uxtheme` call | AC-013-23 |
| Exe size | Grows (1.0.0 is 1.02 MB); CI keeps it under 1.5 MB | CI |
| Registry access | App settings as in 007; the two theme values at start-up and on theme change | Code review |
| Forbidden APIs | None added. `dwmapi` and `uxtheme` imports only in `Native/` | CI |

## Acceptance criteria

- **AC-013-01** `[auto]` Given each combination of apps value (0, 1, missing, wrong type), taskbar
  value (0, 1, missing) and high contrast, when the theme is decided, then it matches REQ-013-01 and
  REQ-013-04.
- **AC-013-02** `[auto]` Given every text token on every surface it is drawn on, and the accent on the
  card, in both themes, then the contrast is at least 4.5:1 and 3:1.
- **AC-013-03** `[auto]` Given builds 19045, 22000 and 22621, when the window attributes are decided,
  then Mica is only set on 22621, and dark mode and corners on all three.
- **AC-013-04** `[auto]` Given each state in REQ-013-17 on a light and a dark taskbar, when the icon
  is described, then shape and color match the table.
- **AC-013-05** `[auto]` Given two identical statuses with the same theme, when described, then the
  descriptions are equal; given a different taskbar theme, then they differ only for the states drawn
  in the taskbar foreground.
- **AC-013-06** `[auto]` Given the foreground colors on the Windows 11 taskbar colors (`#1C1C1C`
  dark, `#EEEEEE` light), then the contrast is at least 4.5:1.
- **AC-013-07** `[auto]` Given every state and both languages, when the tooltip and menu status line
  are built, then they fit 128 characters and have the two lines of REQ-013-20.
- **AC-013-08** `[auto]` Given builds 17763 and 18362, when deciding whether to call the `uxtheme`
  ordinals, then only 18362 does.
- **AC-013-09** `[auto]` Given ordinal imports outside `Native/UxTheme.cs`, when
  `Test-ForbiddenApis.ps1` runs, then it fails.
- **AC-013-10** `[auto]` Given the app started with `--startup`, then the smoke test passes as in 007.
- **AC-013-11** `[hw]` Given Windows in light and then dark mode, with the window open, then the
  window, its controls and its title bar follow without reopening, and the next menu follows too.
- **AC-013-12** `[hw]` Given the taskbar in light and dark mode, then the icon's number and glyphs
  follow and stay readable, at 100 %, 125 %, 150 % and 200 % scaling.
- **AC-013-13** `[hw]` Given the headset on above 50 %, between the limits, under the low limit,
  charging, off, and the dongle removed, then each icon in REQ-013-17 appears.
- **AC-013-14** `[hw]` Given the window with the keyboard only, then Tab reaches every control, Space
  flips each toggle, Enter saves, Esc cancels, and the focus ring is visible.
- **AC-013-15** `[hw]` Given Narrator on, when moving through the window, then each toggle is read as
  a check box with its label and state.
- **AC-013-16** `[hw]` Given an invalid number and Save, then the error text appears under the row,
  and disappears once the field is edited.
- **AC-013-17** `[hw]` Given *Check for updates* when up to date, then the result appears in the info
  bar, not in a message box.
- **AC-013-18** `[hw]` Given high contrast turned on with the window open, then it switches to system
  colors and stays readable, and the icon uses plates.
- **AC-013-19** `[hw]` Given the low limit set above the current battery and saved, then the notice
  shows the red plate with the number, silently.
- **AC-013-20** `[hw]` Given the X button, then the close question shows the logo and a description
  under each choice.
- **AC-013-21** `[hw]` Given the window closed, then CPU at rest is 0 % and wake-ups are one per
  polling interval, as in 007.
- **AC-013-22** `[hw]` Given the window opened and closed once, then twenty more times with a theme
  switch in between, then handles, GDI and USER objects after the twenty are the same as after the
  first, memory stays at most 5 MB, and threads are as in 007.
- **AC-013-23** `[hw]` Given `--startup`, then the icon appears within 300 ms of the process start
  (median of five).
- **AC-013-24** `[hw]` Given the MSI built by CI, then its pages show the new bitmaps. Checked on a test
  machine or virtual machine, never by installing on the maintainer's PC.

## Open questions

None block this spec. Points the maintainer should weigh before approving:

- **REQ-013-22** is the first undocumented API in the project. Without it the menu stays light in
  dark mode, and everything else in this spec still applies.
- **REQ-013-17** drops the dark plate for the normal state, so a healthy battery shows as a plain
  number, like a system icon. Keeping the plate is a one-line change to the table.

## Status history

- 2026-11-05: Draft.
- 2026-11-05: Approved, with REQ-013-17 and REQ-013-22 as proposed.
- 2026-11-05: Implemented, `[hw]` criteria pending. Three clarifications from implementation,
  approved by the maintainer: the field and warning-icon colors added to REQ-013-05; the
  language list keeps its theme's border (REQ-013-10), because a drawn border would double the one
  the combo box draws itself; and ordinal 133 is not needed (REQ-013-22).

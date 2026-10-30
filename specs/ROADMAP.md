# Roadmap

Specs are implemented in this order. Each one is drafted, approved and implemented on its own; a
spec is not started until the specs it depends on are at least Implemented.

| Spec | Title | Depends on | Blocked by | Status |
|---|---|---|---|---|
| [001](001-app-skeleton.md) | App skeleton, build and CI | — | — | Implemented |
| [002](002-dongle-discovery.md) | Dongle discovery, interface selection, hot plug | 001 | — | Implemented |
| [003](003-hid-transport.md) | Overlapped HID transport in the message loop | 002 | OQ-05 is settled by its own first test | Implemented |
| 004 | Status query: battery, charging, on/off, push events | 003 | OQ-02, OQ-03 | Not started |
| 005 | Tray icon with percentage and colors, tooltip | 004 | — | Not started |
| 006 | Low-battery notice | 005 | — | Not started |
| 007 | Settings and persistence in HKCU, `--startup` | 005 | — | Not started |
| 008 | Suspend and resume | 004 | — | Not started |
| 009 | MSI installer, release pipeline, `--uninstall`, `--create-desktop-shortcut` | 007 | — | Not started |
| 010 | Check for updates | 009 | — | Not started |
| 011 | Firmware version | 004 | OQ-06, OQ-07 | Not started |
| 012 | Microphone mute state (optional) | 004 | Capture of `0x05`, see protocol.md section 6 | Not started |

Open questions are listed in [protocol.md](../docs/protocol.md#8-open-questions).

## Changes from the original proposal

The original order had discovery first and start-up, CLI and suspend as spec 007, with CI at 008.
It was changed for these reasons:

- **The skeleton comes first (new 001).** Discovery needs a window for `WM_DEVICECHANGE` and the
  transport needs the message loop, so they cannot be built before the app exists.
- **CI moves to the first spec.** Every pull request must pass the forbidden-API check, the tests
  and the size budget, so these must exist before the first feature. Only the MSI and the release
  workflow stay later, in 009.
- **The old 007 is split.** Single instance goes to 001, `--startup` to 007 (with *Start with
  Windows*), `--uninstall` and `--create-desktop-shortcut` to 009 (the MSI runs them), and only
  suspend and resume stay as a spec of their own, 008.

Everything after that keeps its order and moves up one number.

## Release plan

| Version | Contents |
|---|---|
| 0.1.0 | Through 005: usable from the portable exe |
| 1.0.0 | Through 010 |
| 1.x | 011 and 012, when their open questions are resolved |

# Changelog

All notable changes to CloudStatus are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Each version's section becomes the notes of its
GitHub Release.

## [Unreleased]

### Added

- Tray app skeleton: a single instance with a tray icon and an *Exit* menu, in English and Spanish.
  It does not talk to the headset yet.
- The tooltip shows whether a supported dongle is connected, and follows it being plugged in or
  removed.
- The tooltip shows the battery, whether it is charging, and whether the headset is off. Charging
  and power changes appear as they happen; the battery is read every 5 minutes.
- The tray icon shows the battery number, with its background color showing the level, charging,
  headset off or no dongle.
- A silent Windows notice when the battery drops under 20 %, and again under 10 %. Each one is
  shown once until the headset charges or recovers 5 points.
- A *Settings* window, from the tray menu or by double-clicking the icon: low-battery notice on or
  off, the low battery limit (15–45 %), how often the battery is read (1–30 minutes), language and
  *Start with Windows*. Settings are stored in `HKCU\Software\CloudStatus`. Started by hand the app
  opens Settings; started with Windows (`--startup`) it stays in the tray.

[Unreleased]: https://github.com/jordanzdev/CloudStatus/commits/main

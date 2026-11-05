# Changelog

All notable changes to CloudStatus are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/). Each version's section becomes the notes of its
GitHub Release.

## [Unreleased]

## [1.0.0] - 2026-11-05

First release: the battery of a HyperX Cloud II Wireless (HP dongle) in the notification area, at
no cost while gaming.

### Added

- **Tray icon with the battery.** The number is the battery percentage, and its background shows
  the state: dark from 50 %, amber below 50 %, red below the low limit or on a charging error, teal
  while charging. Grey means the headset is off, and a grey outline means no dongle. The tooltip
  spells it out.
- **Live updates.** Charging and power changes appear as the headset reports them; the battery is
  read every 5 minutes. Plugging or removing the dongle, and sleep and resume, are followed with no
  replugging.
- **Low-battery notice.** A silent Windows notice under 20 % and again under 10 %, each shown once
  until the headset charges or recovers 5 points. *Do not disturb* keeps it out of full-screen games.
- **Settings.** Notice on or off, the low battery limit (15–45 %), how often the battery is read
  (1–30 minutes), language (English or Spanish) and *Start with Windows*.
- **Installer.** A per-user MSI in English and Spanish, no administrator rights needed. Upgrading
  keeps the settings; uninstalling removes them, the *Start with Windows* entry and the desktop
  shortcut. A portable exe is also provided.
- **Check for updates**, in Settings, only when clicked and only against GitHub. The download is
  verified with SHA-256 before the installer starts.

### Notes

- Supported: the HP-branded dongle `0x03F0:0x018B`. `0x03F0:0x0696` uses the same protocol but is
  untested. Kingston-era dongles are not supported yet.
- The headset is only read: CloudStatus sends two queries, battery and charging, and never changes
  a setting or touches firmware.
- The binaries are not code-signed, so SmartScreen may warn the first time.

[Unreleased]: https://github.com/jordanzdev/CloudStatus/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/jordanzdev/CloudStatus/releases/tag/v1.0.0

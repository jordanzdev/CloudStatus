# 010: Check for updates

**Status:** Implemented

## Context

The last piece of 1.0 ([ROADMAP.md](ROADMAP.md#release-plan)). CursorLock has a *Check for updates*
button that the user must click; it is the only code in that app that uses the network or starts
another process. D16 decides that CloudStatus works the same way: only on click, only against
`github.com`, SHA-256 verified, synchronous WinHTTP on the UI thread.

How it works in CursorLock, and here:

1. `https://github.com/jordanzdev/CloudStatus/releases/latest` answers with a redirect to
   `.../releases/tag/vX.Y.Z`. The version is read from that redirect, which is not followed: no API,
   no JSON, no rate limits.
2. If it is newer and the running exe is the one the MSI installed (009), the app asks, then
   downloads `CloudStatus-X.Y.Z.msi` and `SHA256SUMS.txt` from that release, checks the hash, starts
   the MSI and exits. The MSI replaces the app and offers to open it again.
3. If the running exe is a portable copy, it offers to open the release page instead.

GitHub serves release files by redirecting from `github.com` to its own download host
(`release-assets.githubusercontent.com` or similar). Every request the app makes is to `github.com`;
only those download redirects lead elsewhere, and always to GitHub. The hash check does not depend
on that host.

There is no CloudStatus release yet. Until there is one, `releases/latest` does not point to a tag,
and the check must say so rather than report a network error.

## Goals

- Update in two clicks without visiting GitHub, with the installed MSI.
- Never touch the network or start a process unless the user clicks the button.
- Never run a download that does not match its published hash.

## Non-goals

- Automatic or scheduled checks, notifications about new versions, or any background network use.
- Checking for headset firmware, or contacting HP or HyperX servers. There is no public API, and
  000-vision forbids it.
- A menu item that opens NGENUITY or the support page. The maintainer allowed it "at most"; it is not
  proposed for 1.0.
- Updating a portable copy in place.
- Pre-releases. `releases/latest` ignores them.
- Signature checks beyond SHA-256: the binaries are not signed (009).

## Requirements

### Trigger and network

- **REQ-010-01** The Settings window MUST get a *Check for updates* button (*Buscar
  actualizaciones*) in the space 007 left for it, left of *Save*. It is the only trigger.
- **REQ-010-02** Every request MUST be HTTPS to `github.com`, path `/jordanzdev/CloudStatus/releases/…`,
  built by pure code in `Core/`. Only the download requests follow redirects.
- **REQ-010-03** WinHTTP MUST be the only network API, used only in `Native/WinHttp.cs` and
  `Updates/`. `ShellExecuteW` MUST appear only in `Native/Shell32.cs` and the update check. CI MUST
  enforce both.
- **REQ-010-04** Requests MUST be synchronous on the UI thread with a 15 s timeout, with progress on
  the button itself (*Checking…*, *Downloading…*) and the wait cursor. No thread is created (D16).

### Decision

- **REQ-010-05** The version MUST be read from the `Location` header of the `releases/latest`
  redirect: the text after `/releases/tag/v`, as `major.minor.patch`. That parsing MUST be pure code
  in `Core/`.
- **REQ-010-06** A redirect with no tag, as before the first release, MUST show *No version has been
  published yet* rather than a network error.
- **REQ-010-07** The outcome MUST be decided by pure code from the current version, the latest one
  and whether the running exe is the installed one (`HKCU\Software\CloudStatus\Install\Executable`,
  009):

  | Latest | Running copy | Outcome |
  |---|---|---|
  | Not newer | Any | *You have the latest version* |
  | Newer | Installed by the MSI | Ask, then download, verify and run the MSI |
  | Newer | Portable | Ask, then open the release page |

### Download and integrity

- **REQ-010-08** The MSI and `SHA256SUMS.txt` MUST come from the release of that version, each at
  most 32 MB. The MSI's line MUST be found in `SHA256SUMS.txt` by pure code that accepts the
  `sha256sum` formats (`hash  name` and `hash *name`).
- **REQ-010-09** The SHA-256 MUST be computed with `BCryptHash` and compared before the file is ever
  written under `%TEMP%\CloudStatus-update`. A mismatch or a missing line MUST stop and say so.
- **REQ-010-10** After starting the MSI, the app MUST exit, so that the installer can replace it.
  The MSI preselects the app's language (009).
- **REQ-010-11** Every failure (network, missing release file, hash, launch) MUST show its own message
  in the chosen language, leave the app running, and restore the button.

### Documentation

- **REQ-010-12** The README MUST be completed as in CursorLock: *Usage*, *Settings*, *Updates*, *Why it
  has no impact*, *What it does NOT do*, *Robustness* and *Project structure*. A `CONTRIBUTING.md`
  MUST explain the spec process and the checks.

## Performance budget

| Metric | Effect | Verified by |
|---|---|---|
| At rest | Unchanged: nothing runs until the button is clicked | AC-010-14 |
| Network | None unless clicked; then only `github.com` and GitHub's download host | AC-010-03, CI |
| UI thread | Blocked for the length of the requests, after a click only (D16) | AC-010-11 |
| Threads | No thread of ours. WinHTTP may leave idle Windows thread-pool workers after a check, as Settings does (D20) | AC-010-14 |
| Memory | At most 5 MB at rest after one check; each further check in the same session may add a little (see below). The downloaded MSI is released | AC-010-14 |
| Exe size | Grows; CI keeps it under 1.5 MB | CI |

**Cost of a check (amended).** The first check makes Windows load its network stack (`winhttp`,
`schannel`, `ncrypt`, `dnsapi` and others), which stays loaded. Measured on 2026-11-05 with the
headset on: 3.48 MB and 175 handles before; 4.83 MB and 305 handles 90 s after one check. Each further
check in the same session adds about 50–70 KB and one handle, which Windows only partly returns over
time: 5.21 MB and 326 handles two minutes after 12 checks in a row. Forcing a garbage collection
changed nothing, and neither did disabling proxy detection, so the growth is in Windows' network
code, not in CloudStatus. Restarting the app returns it to the baseline. The maintainer accepted this
cost (option A) over running the check in a separate process.
## Acceptance criteria

- **AC-010-01** `[auto]` Given `Location` headers ending in `/releases/tag/v1.2.3`, `/releases/tag/v1.2`,
  `/releases/tag/1.2.3`, `/releases/tag/vX` and `/releases`, when parsed, then only the first two give
  a version (1.2.3 and 1.2.0), and the last one is reported as no release.
- **AC-010-02** `[auto]` Given each row of REQ-010-07, and an equal and an older latest version, when
  the outcome is decided, then it matches the table.
- **AC-010-03** `[auto]` Given a version, when the URLs are built, then every one is
  `https://github.com/jordanzdev/CloudStatus/releases/…`.
- **AC-010-04** `[auto]` Given `SHA256SUMS.txt` with LF or CRLF lines, `hash  name`, `hash *name`, a
  similar file name and no matching line, when the MSI's hash is looked up, then only the exact name
  matches.
- **AC-010-05** `[auto]` Given WinHTTP or `ShellExecuteW` outside the allowed files, when the policy
  check runs, then it fails.
- **AC-010-06** `[hw]` Given no release published, when *Check for updates* is clicked, then *No version
  has been published yet* is shown.
- **AC-010-07** `[hw]` Given a published release and an MSI built locally with an older version
  installed, when the button is clicked and the update accepted, then the MSI is downloaded, verified
  and started, CloudStatus exits, and after installing it runs the new version with the same
  settings.
- **AC-010-08** `[hw]` Given the portable exe of an older version, when the update is accepted, then the
  release page opens in the browser and nothing is downloaded.
- **AC-010-09** `[hw]` Given the current version, then *You have the latest version* is shown.
- **AC-010-10** `[hw]` Given no network, then the failure message is shown, the button comes back and
  the app keeps working.
- **AC-010-11** `[hw]` Given a check in progress with the headset on, then the icon stays correct
  afterwards and no command is left waiting.
- **AC-010-12** `[hw]` Given a resource monitor such as Resource Monitor's *Network* tab, when the
  update check runs, then CloudStatus only connects to GitHub, and it never connects at any other
  time during a day of use.
- **AC-010-13** `[hw]` Given *Español* and *English*, then every update message appears in that
  language.
- **AC-010-14** `[hw]` Given one update check done and the window closed, then CPU at rest is 0 %,
  memory at most 5 MB, and every thread beyond the 2 at rest starts in `ntdll`. Given ten more checks,
  then CPU at rest is still 0 % and the growth is no more than about 70 KB and one handle per check.
- **AC-010-15** `[hw]` Given the README and `CONTRIBUTING.md`, then they describe what the app, the
  build and the process actually do.

## Open questions

None block this spec.

## Status history

- 2026-11-05: Draft.
- 2026-11-05: Approved.
- 2026-11-05: Amended and approved: the cost of the first check and of repeated checks (budget table,
  AC-010-14).
- 2026-11-05: Implemented, `[hw]` criteria pending.

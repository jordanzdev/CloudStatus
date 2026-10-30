# Performance

CloudStatus runs while competitive games run. These budgets are requirements: every spec states
which ones it affects, and a change that exceeds one needs an approved spec change.

## Budgets

| Metric | Budget | Checked by |
|---|---|---|
| CPU at rest, between queries | 0 %: the thread sleeps in the wait | WPA |
| Threads | None created by our code: **2 at rest**, the UI thread and one from the runtime ([D20](ARCHITECTURE.md#d20-runtime-threads-are-accepted)) | `Get-Process`, CI grep for thread APIs |
| Private memory, steady state | ≤ 5 MB | `Get-Process` |
| Exe size | ≤ 1.5 MB | **CI fails above it** |
| HID command, write to response | ≤ 100 ms typical, 500 ms hard timeout | Spec 003 logging |
| Blocking the UI thread on HID | Never | Design ([D2](ARCHITECTURE.md#d2-one-thread-all-hid-io-overlapped)), CI |
| Process wake-ups | At most one per polling interval (5 min by default), plus device events | WPA |
| Leaks | Handles, GDI objects and memory stable after 24 h | Soak test |
| Start to icon visible | ≤ 300 ms | Manual timing |

**The 100 ms budget is per command, not per polling cycle.** A cycle has two commands (battery and
charging) sent one after the other, so a cycle is about 200 ms by this budget. The real latency is
unknown (OQ-04): the sources only show fixed sleeps of 50–100 ms.

## Baseline

CursorLock 1.0.2, same stack, measured on 2026-10-30 after several hours of running:

| Private memory | Working set | Threads | Handles | Exe |
|---|---|---|---|---|
| 3.00 MB | 10.77 MB | 2 | 165 | 1.13 MB |

The exe budget leaves about 370 KB over CursorLock, and CloudStatus drops its hotkey and OSD code.

## Measuring

Run the release build, not a debug build, with the headset on and connected. Let the app settle for
5 minutes first.

### Memory, threads and handles

```powershell
$p = Get-Process CloudStatus
'{0:N2} MB private, {1} threads, {2} handles' -f ($p.PrivateMemorySize64 / 1MB), $p.Threads.Count, $p.HandleCount
```

### GDI objects

In Process Explorer, add the **GDI Objects** and **USER Objects** columns (*View → Select Columns →
Process Performance / Process Memory*). In Task Manager, the *Details* tab has the same columns.

### CPU and wake-ups

From an administrator prompt, record across at least two polling intervals (12 minutes by default):

```powershell
wpr -start GeneralProfile -filemode
# wait
wpr -stop cloudstatus.etl
```

Open the file in Windows Performance Analyzer, in *Computation → CPU Usage (Precise)*. Filter by
`CloudStatus.exe` and count context switches of its threads. Expect one short burst per polling
interval and nothing between bursts.

### Start-up time

Record the screen at 60 fps and count frames from launch to icon, or use WPA's process lifetime
and the first `Shell_NotifyIconW` activity. Repeat five times and report the median.

### Soak test

1. Note memory, threads, handles and GDI objects after 5 minutes.
2. Leave the app running for 24 hours, including at least one sleep and resume, one dongle unplug
   and replug, and one headset off and on.
3. Note the same values. Handles and GDI objects must return to the starting value, within 5 %, and
   private memory must stay within budget.

### While playing

Play one CS2 competitive match with the app running. Listen for clicks or dropouts around each
polling interval (OQ-09), and confirm the microphone is never unmuted.

## Results

Filled in by the maintainer with real hardware, for each version.

| Version | Date | Exe | Private | Threads | Handles | GDI | Command latency | Wake-ups / h | Start | Soak 24 h | Audio clean |
|---|---|---|---|---|---|---|---|---|---|---|---|
| | | | | | | | | | | | |

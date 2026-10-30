# HyperX Cloud II Wireless HID protocol

What CloudStatus knows about the headset's protocol, where each fact comes from and what is
still unknown. **Every byte that CloudStatus sends must be listed here first.** Nothing in this
file was invented: each fact cites a source, and anything no source covers is an
[open question](#8-open-questions).

Last reviewed: 2026-10-30.

## 1. Sources and licenses

CloudStatus is MIT licensed. Only **facts** about the protocol are taken from these projects
(bytes, offsets, meanings). No code is copied from any of them.

| Tag | Project | Commit | License | Used for |
|---|---|---|---|---|
| **[A]** | [auto94/HyperX-Cloud-2-Battery-Monitor](https://github.com/auto94/HyperX-Cloud-2-Battery-Monitor) | `634e045` | MIT © 2023 auto94 | Battery query for both variants, device list, behavior when the headset is off |
| **[H]** | [LennardKittner/HyperHeadset](https://github.com/LennardKittner/HyperHeadset) (formerly `HyperXCloudIIWireless`, the old URL redirects) | `782cc44` | MIT © 2023 Lennard Kittner | Most complete command tables, push events, known side effects |
| **[C]** | [Sapd/HeadsetControl](https://github.com/Sapd/HeadsetControl) | `50e5ca5` | **GPL-3.0** | HP usage page and usage, battery voltage, independent confirmation of [A] and [H] |
| **[L]** | Local observation on the maintainer's dongle, 2026-10-30 | — | — | Descriptor values in [section 2](#2-devices). Read-only: nothing was sent to the headset |

Notes:

- **[C] is GPL-3.0**, which is not compatible with copying code into an MIT project. Protocol
  facts are not code, so they may be used, but no [C] source may be copied or adapted.
- **[H]'s protocol document** says it comes from decompiling NGENUITY with dnSpy and from live
  packet captures, and its README warns that reverse engineering may be restricted by the
  software's license. CloudStatus does not decompile anything: its own findings come only from
  USB captures of the maintainer's own device ([section 9](#9-capture-guide)).

## 2. Devices

The **vendor ID selects the protocol**. The two variants reuse command IDs with different
meanings (see [section 5](#5-commands)), so sending the packet of one variant to the other is
not safe.

| Variant | VID | PID | Sources | Status in CloudStatus |
|---|---|---|---|---|
| HP | `0x03F0` | `0x018B` | [A] [H] [C] [L] | Supported, target hardware |
| HP | `0x03F0` | `0x0696` | [A] [H] [C] | Supported, not tested on hardware |
| Kingston | `0x0951` | `0x1718` | [A] [H] [C] | Not in 1.0 (D9 in ARCHITECTURE.md); blocked by OQ-01 |

Listed in the sources but **not** supported, because they are other products or unconfirmed:

| VID | PID | Source | Why not |
|---|---|---|---|
| `0x03F0` | `0x0D93` | [H] | [A] identifies it as Cloud Stinger 2 Wireless |
| `0x03F0` | `0x1718` | [H] | Only [H], in a list of candidate IDs |
| `0x0951` | `0x0B92` | [H] | Only [H] |
| `0x0951` | `0x16EA`, `0x16EB` | [H] | [H] groups them with the Cloud Flight S |

### HP `0x03F0:0x018B`: observed interfaces [L]

The dongle's vendor interface is `MI_03`, with four HID collections. Revision: `bcdDevice`
`0x4107`, bus-reported name `HyperX Cloud II Wireless`.

| Collection | Usage page | Usage | Input | Output | Feature | Report IDs | Role |
|---|---|---|---|---|---|---|---|
| `COL01` | `0x000C` | `0x0001` | 5 | 0 | 0 | `0x01` | Consumer control. Not used |
| `COL02` | `0xFFC0` | `0x0001` | 2 | 0 | 0 | `0x64`, `0x65` | Unknown, input only. Not used (OQ-08) |
| **`COL03`** | **`0xFF90`** | **`0x0303`** | **20** | **20** | **0** | **`0x06`** | **The protocol below** |
| `COL04` | `0xFF00` | `0x0001` | 64 | 64 | 64 | `0x90`, `0xC4`, `0xE2`, `0xFF` | Unknown. **Must never be read or written** |

`COL04` is the only collection with feature reports and its purpose is unknown, so CloudStatus
treats it as off limits until proven otherwise. Opening a collection with access 0 to read its
descriptor is allowed on all of them, because that sends nothing to the device.

## 3. Choosing the interface

Enumerate HID interfaces for the supported VID/PID pairs and pick the one with the expected
usage page and usage. **Never probe an interface by writing to it**: [H] does that on Windows,
and its own source notes that a burst of writes per connect attempt destabilized another
HyperX dongle (Cloud III S).

| Variant | Usage page | Usage | Source |
|---|---|---|---|
| HP | `0xFF90` | `0x0303` | [C] [L] |
| Kingston | unknown | unknown | OQ-01 |

[A] picks the interface with the highest usage on both variants, which is a heuristic and not
what CloudStatus needs. [H] probes with writes. [C] sets no usage for Kingston.

## 4. Transport on Windows

- Input and output buffers are sized from `HIDP_CAPS`, not hard-coded. On `COL03` both are 20
  bytes [L]. The sources write 20 [H] or 52 [A] [C] bytes for HP and 62 [H] [C] or 52 [A] for
  Kingston, always zero padded.
- Byte 0 of every buffer is the report ID, and every offset in this file counts from there.
- **HP uses output reports.** `COL03` has no feature reports [L], so the feature-report
  fallback in [H] (for dongles where `WriteFile` fails with `ERROR_INVALID_FUNCTION`) cannot
  apply to it. Whether `WriteFile` is accepted on this dongle is confirmed by hardware testing.
- **Kingston needs a preparation step**: `GetInputReport(0x06)` before each write, ignoring
  errors [A] [H] [C]. HP does not need it [A] [H] [C].
- **No sleeps.** The sources sleep after each write and then read with a 1000 ms timeout: 50 ms
  [H], 100 ms [C]. Those are fixed delays, not measured latencies. CloudStatus keeps a read
  pending, so a response is handled as soon as it arrives.
- **Responses are matched by content**, by report ID, header and command ID. The HID stack
  gives every open handle a copy of each input report, so CloudStatus also sees responses to
  NGENUITY's queries, and a response may arrive for a command CloudStatus did not send.
- **Do not send several commands at once.** [H] reports that simultaneous commands may cause
  issues, and that frequent queries can cause instability (its default active refresh is about
  every 3 minutes). CloudStatus sends one command at a time.

## 5. Commands

The tables list what the sources document. **Listing a command here does not allow sending
it**: only the commands in [section 6](#6-what-cloudstatus-may-send) may be sent.

### HP (`0x03F0`)

```
Request:  06 FF BB <cmd> <arg> 00 ...     report ID 0x06, zero padded to the output length
Response: 06 FF BB <cmd> <data...>        report ID 0x06
```

The request header comes from [A] [H] [C]; the response header is validated the same way in
[H] and [C].

| Cmd | Name | Response data | Sources |
|---|---|---|---|
| `0x01` | Wireless status | `[4]` 1 or 4 connected, 2 pairing | [H]. **Has side effects**, see below |
| `0x02` | Battery | `[7]` percent; `[5..6]` voltage in mV, big endian | Percent [A] [H] [C]; voltage [C] |
| `0x03` | Charging | `[4]` see [section 7](#7-values) | [H] [C] |
| `0x05` | Get mic mute | `[4]` 1 muted | [H] |
| `0x06` | Get sidetone | `[4]` 1 on | [H] |
| `0x07` | Get auto shutdown | `[4]` minutes | [H] [C] |
| `0x08` | Get mic connected | `[4]` 1 connected | [H] |
| `0x09` | Get pairing info | `[4]` value | [H] |
| `0x0B` | Get sidetone volume | `[4]` volume | [H] |
| `0x20` | **Set** mic mute | `[4]` echo | [H] |
| `0x21` | **Set** sidetone | `[4]` echo | [H] |
| `0x22` | **Set** auto shutdown | `[4]` echo | [H] [C] |
| `0x23` | **Set** sidetone volume | `[4]` echo | [H] |

### Kingston (`0x0951`)

```
Request:  06 00 02 00 9A 00 00 68 4A 8E 0A 00 00 00 BB <cmd> <arg> 00 ...
Response: 0B 00 BB <cmd> <data...>        report ID 0x0B
```

The request header comes from [A] [H] [C]; the response header from [H] [C].

| Cmd | Name | Response data | Sources |
|---|---|---|---|
| `0x01` | Status (also returns mute) | `[4]` 1 or 4 connected, 2 pairing | [H]. **Suspected side effects**, see below |
| `0x02` | Battery | `[7]` percent | [A] [H] [C] |
| `0x03` | Charging | `[4]` see [section 7](#7-values) | [H] [C] |
| `0x04` | Unknown, unsolicited | Unknown | [H], which recommends ignoring it |
| `0x08` | Mute event, unsolicited | `[4]` 1 muted | [H] |
| `0x09` | Used during initialization | Unknown | [H] |
| `0x11` | Firmware version | `[4..7]` four numbers, e.g. `4.1.0.1` | [H] |
| `0x18` | **Set** auto shutdown | Minutes | [H] [C] |
| `0x19` | **Set** sidetone | `[4]` 1 on | [H] [C] |
| `0x1A` | Get auto shutdown | `[4]` minutes | [H] |
| `0x1D` | Used during initialization | Unknown | [H] |

[H] also documents a special surround query with a different header on Kingston:
`06 00 00 00 FF 00 00 68 4A 8E 00 ...`, answered with report ID `0x0A`.

### Known side effects

- **`0x01` on HP resets the headset's state**: "works but causes state to reset e.g. unmutes
  the headset" (comment in [H], which disables the query for this reason). Unmuting a microphone
  in the middle of a match is exactly the kind of interference CloudStatus must never cause.
- **`0x01` on Kingston is suspect**: [H] sends it, but also keeps a disabled initialization
  sequence that starts with `0x01`, commented "I think this unmutes the headset". That sequence
  also contains `0x11`, `0x1D` and `0x09`, so the culprit is not known.

## 6. What CloudStatus may send

This is the complete list. `Device/Commands.cs` must hold exactly these reports, each one citing
this section.

| Variant | Cmd | Purpose | Status |
|---|---|---|---|
| HP | `0x02` | Battery | Allowed |
| HP | `0x03` | Charging | Allowed |
| Kingston | `0x02` | Battery | Allowed once OQ-01 is resolved |
| Kingston | `0x03` | Charging | Allowed once OQ-01 is resolved |

**Conditional**, each one only with its own approved spec:

| Variant | Cmd | Purpose | Condition |
|---|---|---|---|
| HP | `0x05` | Mute state (spec 011) | Confirmed free of side effects by a capture of NGENUITY sending it |
| Kingston | `0x11` | Firmware (spec 010) | Confirmed free of side effects on Kingston hardware |

**Never sent**, on any variant:

- `0x01`, because of the side effects above.
- Every command that writes a setting: HP `0x20`–`0x23`, Kingston `0x18` and `0x19`.
- Kingston `0x09` and `0x1D`, whose purpose is unknown.
- Anything related to firmware updates. **Flashing firmware is forbidden.**
- Any report not listed in this section, and any report to a collection other than the selected
  one.

## 7. Values

### Battery

Percent, 0–100, at byte 7 on both variants [A] [H] [C]. [A] treats values above 100 as 0.
CloudStatus treats a value above 100 as invalid and keeps the last good one.

A value of **0 does not reliably mean an empty battery**: [A] uses it to mean "headset off, dongle
plugged in" (OQ-03).

### Charging

| Value | Meaning | Sources |
|---|---|---|
| `0` | Not charging | [H] [C] |
| `1` | Charging | [H] [C] |
| `2` | Fully charged | [H] |
| other | Charge error | [H] |

[C] only distinguishes `1` from everything else.

### Connection

From status reports: `1` or `4` connected, `2` pairing [H]. Inbound reports are safe to parse;
what is forbidden is sending the `0x01` query.

### Push events

CloudStatus keeps a read pending, so these arrive at no CPU cost. The list is what the sources
state, and nothing here has been verified on the target hardware (OQ-02).

| Variant | Cmd | Event | Sources |
|---|---|---|---|
| HP | `0x01` | Connection changes, including headset power on | [H] |
| HP | `0x20` | Mic mute changes | [H] |
| HP | `0x21` | Sidetone changes | [H] |
| Kingston | `0x08` | Mute button | [H] |
| Kingston | `0x04` | Sporadic, unknown. [H] says it does **not** fire on charger or battery changes | [H] |

According to [H], the battery level is **not** pushed and must be queried. No source says that
charging changes are pushed on either variant.

## 8. Open questions

| ID | Question | Blocks | How to answer |
|---|---|---|---|
| **OQ-01** | Usage page and usage of the Kingston vendor interface. No public source gives them. | Kingston support | Device Manager or a PowerShell dump from someone who owns one (`HID_DEVICE_UP:xxxx_U:xxxx` in Hardware Ids) |
| **OQ-02** | Which push events does `0x03F0:0x018B` really send? Above all: charging changes and power on/off. | Polling interval, spec 003 | Capture scenarios S1–S4 |
| **OQ-03** | What happens when the dongle is plugged in and the headset is off: no response, or a response with battery 0? | Off detection, spec 003 | Capture scenario S3 |
| **OQ-04** | Real latency of a command on this dongle. The sources only show fixed sleeps. | 100 ms budget | Capture timestamps, or logging in the spec 002 tests |
| **OQ-05** | Does `WriteFile` work on `COL03`? The descriptor allows it; [H] saw failures on other dongles. | Spec 002 | First hardware test of spec 002 |
| **OQ-06** | Headset firmware on HP. **Only Kingston has a documented command** (`0x11`). | Spec 010 | Capture scenario S5 |
| **OQ-07** | Is the dongle's `bcdDevice` a firmware version? `0x4107` reads as `4.1.0.7`, the same shape as the `4.1.0.1` in [H]. This is an unverified hypothesis. If true, the dongle's firmware can be shown with no HID traffic at all. It would still say nothing about the headset's own firmware. | Spec 010 | Compare with what NGENUITY displays (scenario S5) |
| **OQ-08** | What is `COL02` for? It is input only and carries report IDs `0x64` and `0x65`. | Nothing yet | Capture scenarios S1–S4 |
| **OQ-09** | Do queries cause audible clicks or dropouts? | Performance requirement | Listening test during the spec 003 hardware test |

## 9. Capture guide

Everything in [section 8](#8-open-questions) that needs a capture is answered this way. Capturing
only observes traffic: it sends nothing to the headset.

### Setup

1. Install [Wireshark](https://www.wireshark.org/), selecting **USBPcap** in its installer, and
   reboot.
2. Install HyperX NGENUITY from the Microsoft Store (scenario S5 needs it).
3. Run Wireshark as administrator. Capture on the `USBPcap` interface for the bus the dongle is
   on: try each one and look for traffic while pressing the mute button.
4. Find the device with `usb.idVendor == 0x03f0 && usb.idProduct == 0x018b`. That filter only
   matches the descriptor request, so reconnect the dongle during the capture to see it, and note
   the address, for example `3.5.0`.
5. Filter by that address: `usb.src matches "^3\.5\." || usb.dst matches "^3\.5\."`.
6. Look at interrupt transfers and at the HID data. Protocol traffic starts with `06 FF BB`.

The [liquidctl guide](https://github.com/liquidctl/liquidctl/blob/main/docs/developer/capturing-usb-traffic.md)
covers this in more detail. Capture only this bus, and save each scenario as its own `.pcapng`.

### Scenarios

Do S1–S4 with NGENUITY **closed**, because then nothing is sending queries and any `06 FF BB`
report is a real push event.

| ID | Do this | Answers |
|---|---|---|
| S1 | Headset on, idle for 10 minutes | Unsolicited reports at rest, OQ-08 |
| S2 | Plug the charging cable in, wait 1 minute, unplug it | Are charging changes pushed? OQ-02 |
| S3 | Turn the headset off, wait 1 minute, turn it on | Power events, OQ-02. Then, with NGENUITY open and the headset off, what the battery query returns, OQ-03 |
| S4 | Press the mute button a few times, change sidetone, leave the battery draining for an hour | Mute and battery events, OQ-02 |
| S5 | Open NGENUITY and go to the screen that shows the firmware version; write down the version it displays | Firmware query, OQ-06 and OQ-07. Check whether mute was reset, to see which command does it |
| S6 | With NGENUITY open, note the time between each request and its response | Real latency, OQ-04 |

Attach the `.pcapng` files, or just the `06 FF BB` lines with their timestamps, and the version
NGENUITY displayed.

## 10. Credits

Thanks to auto94, Lennard Kittner and the HeadsetControl contributors, whose reverse engineering
makes this project possible.

CloudStatus is not affiliated with or endorsed by HyperX or HP.

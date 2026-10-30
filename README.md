# CloudStatus

A Windows 10/11 tray app that shows the status of a **HyperX Cloud II Wireless** headset: battery,
charging, whether it is on, and firmware version. Built to cost nothing while playing
Counter-Strike 2 competitively.

> **Not usable yet.** The app runs but does not talk to the headset yet. The project follows Spec
> Driven Development: code only comes after an approved spec.

Not affiliated with or endorsed by HyperX or HP.

## Status

- [Vision](specs/000-vision.md): what this is and what it is not.
- [Roadmap](specs/ROADMAP.md): the order of the work.
- [How specs work](specs/README.md).
- [Architecture](docs/ARCHITECTURE.md), [performance budgets](docs/PERFORMANCE.md) and the
  [HID protocol](docs/protocol.md).

## Credits

The protocol facts come from the reverse engineering done by
[auto94/HyperX-Cloud-2-Battery-Monitor](https://github.com/auto94/HyperX-Cloud-2-Battery-Monitor),
[LennardKittner/HyperHeadset](https://github.com/LennardKittner/HyperHeadset) and
[Sapd/HeadsetControl](https://github.com/Sapd/HeadsetControl). See
[docs/protocol.md](docs/protocol.md) for details and licenses.

## License

[MIT](LICENSE) © jordanzdev

# Receive CODEC2 Voice Messages

Some satellites send short recorded voice messages down as data rather than as FM audio: the speech
is compressed with the **Codec2** vocoder to a few hundred bits per second and packed into ordinary
telemetry frames. SkyRoof decodes these messages back to audio with its built-in decoder, and each
message appears in the [Telemetry](telemetry_panel.md) panel, ready to play.

At the moment **HADES-SA** is the satellite that sends them, in Codec2 **700C** mode, interleaved with
its telemetry frames and [SSDV image packets](receive_ssdv.md) on the same downlink. Nothing extra has
to be turned on: if you are decoding HADES-SA telemetry, the voice messages are decoded too.

## Receiving a Message

The steps are the same as for [decoding telemetry](telemetry_panel.md#decoding-a-pass) — open the
[Telemetry](telemetry_panel.md) panel, select the satellite and its telemetry transmitter, and make
sure the SDR is running and tuned to it.

Each message appears as a node in the tree, under the pass node, labeled with the time it was first
heard, how many sub-frames arrived, and how long the recording plays:

```text
04:15:02  Voice  27 sub-frames, 10.8 s
```

The node updates in place as the message comes in. A gap of a few seconds with no sub-frames ends the
message; the next sub-frame after that starts a new one.

Select the node to see the details, and **click anywhere in the detail pane to play the message**.
You can play it while it is still arriving — what plays is always what has been received so far.

The detail pane shows:

- **Sat** and **Tx** — the satellite and the transmitter the message came from;
- **Duration** — the length of the reconstructed recording;
- **Sub-frames** — how many were received, and how many the message spans. There is no "of N": nothing
  in the transmission says how long a message is, so the second number is the span of what arrived,
  not the length of what was sent;
- **Numbered** — the range of sub-frame numbers received;
- **Gaps** — `none`, or how many sub-frames are missing. Missing sub-frames are played as silence, so
  a message with holes still plays at its right length and the words around the holes stay in place;
- **Status** — `receiving...` or `complete`;
- **Saved** — the file it was written to, once it has been saved.

A message the pass ended in the middle of is normal rather than exceptional, and there is no way to
tell how much of it was missed — the downlink does not say.

## About the Audio Quality

Codec2 700C compresses speech to 700 bits per second, which is roughly a hundredth of the rate of a
telephone call. Even a message received without a single lost sub-frame sounds robotic and is often
hard to make out. That is the vocoder's own limit at this bit rate, not a fault in the reception or
in SkyRoof — a perfect decode still sounds like this. Comparing several receptions of the same
message often helps more than trying to clean up one of them.

## Saved Messages

Each finalized message is saved automatically as a WAV file, with a JSON sidecar holding its
metadata, in the **Codec2Voice** subfolder of the [data folder](data_folder.md). Voice sub-frames
carry no checksum of any kind, so a message of fewer than three sub-frames is not written to disk —
too little to tell a real transmission from a misread frame. Such a message is still shown in the
tree and still plays.

Right-click a message in the tree to:

- **Play** — play the message, the same as clicking in the detail pane;
- **Save As...** — save the WAV file to a location of your choice;
- **Open in Player** — open the automatically saved WAV file in the program that Windows associates
  with WAV files. This command is available only after the message has been finalized and saved, so
  it stays grayed out while a message is still being received.

## See Also

- [Telemetry Panel](telemetry_panel.md)
- [Receive SSDV Images](receive_ssdv.md)

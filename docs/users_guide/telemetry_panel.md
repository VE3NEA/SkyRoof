# Telemetry Panel

The Telemetry panel shows the telemetry frames that SkyRoof decodes from the satellite signal.
SkyRoof can decode the telemetry transmitted by many satellites without any external software: the
built-in decoder demodulates and deframes the signal directly from the SDR, right inside the program.
Unlike the [external decoder](decode_telemetry.md) and [sound modem](fsk_afsk_telemetry.md) workflows,
it needs no separate program, no Virtual Audio Cable, and no output stream — SkyRoof feeds the
Doppler-corrected receiver passband straight to the decoder.

The panel is opened from the **View / Telemetry** menu. Its optional outputs — saving to a file,
sharing over a KISS server, and uploading to SatNOGS — are configured as described in
[Setting Up Telemetry Decoding](setting_up_telemetry_decoding.md).

![Telemetry panel](../images/telemetry_panel.png)

## Supported Signals

The decoder supports the **FSK**, **GFSK**, **MSK**, **GMSK**, **AFSK**, **BPSK** and **DBPSK** modulations with the framing
formats used by the supported satellites. The modulation, baud rate, and framing are looked up
automatically from the transmitter description in the satellite database, so there is nothing to
configure for the signal itself — you only have to select the right transmitter.

The panel also decodes **SSTV** images from satellites that transmit them over an FM downlink. This
is covered separately in [Receive SSTV Images](recevie_sstv.md); the rest of this page describes
telemetry frame decoding.

If the selected transmitter uses an unsupported modulation, or its parameters are unknown, the panel
reports `format not supported`, and no decoding takes place.

## Decoding a Pass

1. Open the Telemetry panel from the **View / Telemetry** menu.

2. Select the satellite in the [Satellite Selector](satellite_selector.md) on the toolbar.

3. Select a data transmitter for that satellite. The decoder follows the transmitter selection, so
   pick the transmitter whose telemetry you want to decode. You can select it in the
   [Satellite Transmitters](satellite_transmitters_panel.md) panel, or by clicking on its label on the
   [frequency scale](frequency_scale.md). The panel header shows the satellite name and the selected
   transmitter; hover over it to see the resolved signal parameters.

   Many satellites have several transmitters on the same frequency, and only some of them may be
   supported by the decoder - for exmple, a CW beacon and a GMSK telemetry. The label you click on the frequency scale is not necessarily the one
   that gets selected, so after clicking check that the right transmitter is highlighted in the
   [Satellite Transmitters](satellite_transmitters_panel.md) panel.

   Some satellites can transmit their telemetry at different Baud rates, selected by the ground station. Such satellites have multiple transmitters in the database, one per Baud rate. To switch to a different rate, just select the right transmitter.

4. Make sure the SDR is running and tuned to the satellite. The decoder uses the same
   Doppler-corrected passband as the receiver, so the satellite's signal must be visible on the
   [waterfall](waterfall_display.md) at the tuned frequency.

That is all that is needed to start decoding. When the satellite is above the horizon and a
supported transmitter is selected, frames are decoded automatically and appear in the panel.

## Layout

- The **header** at the top shows the selected satellite and transmitter. Hover over it to see the
  resolved signal parameters (modulation, baud rate, framing). The **gear button** to its right opens
  the [Signal Details](#signal-details) dialog.

- The **status line** below the header shows the current state of the decoder, in color:

  - **satellite below horizon** — the format is supported and the decoder is waiting for AOS;
    decoding starts when the satellite rises above the horizon;
  - **ready to decode** — the satellite is up and the decoder is listening;
  - **DECODING...** in green — a burst is being processed;
  - **format not supported** in red — the transmitter's modulation or framing is not supported;
  - **terrestrial, not decoded** in red — a terrestrial link is selected, which the decoder does not
    handle.

- The **tree** on the left lists the decoded passes and frames.

- The **detail pane** on the right shows the contents of the pass or frame selected in the tree.

## Signal Details

For most satellites the signal parameters resolved from the database are correct and there is nothing
to do here. Occasionally a transmitter is described incorrectly in the database, or its description is
incomplete, and the decoder needs a hand. The **gear button** in the header opens the **Signal Details**
dialog, which shows the parameters actually in use for the selected transmitter and lets you override
any of them:

![Signal Details dialog](../images/signal_details.png)

- **Modulation** and **Framing** — the modulation and framing format of the signal;
- **Baud rate** — the symbol rate;
- **Deviation, Hz** — the FSK deviation;
- **AF carrier, Hz** — the audio carrier frequency for AFSK downlinks;
- **Manchester** and **Precoding (diff.)** — the line coding and the differential precoding. These are
  tri-state: **Auto** leaves the decision to the decoder, **On** and **Off** force it;
- **Telemetry format** — the definition used to parse the decoded frames into named telemetry values.
  It is normally chosen from the satellite's NORAD number; select a different one here when the
  frames decode but their values do not. The field is empty, as in the picture above, when SkyRoof has
  no telemetry definition for the satellite — its frames are then shown without a PAYLOAD section.

To undo a single override, right-click the field and choose **Reset to database value** — the field
goes back to the value the database gave it, leaving your other overrides in place. **Cancel** discards
every edit you made since opening the dialog.

Click **OK** to apply your changes. A change to any of the demodulator fields rebuilds the decoder
immediately, so it takes effect on the next burst. A change to the telemetry format applies to frames
decoded from that point on and does not re-parse the frames already in the tree.

Overrides are remembered per transmitter for the current session only. They are discarded when you
select a different transmitter, and they are not saved between runs.

### Where a Value Came From

The dot to the right of each field shows where its value came from:

- **no dot** — the value from the satellite database, unchanged;
- **orange** — a value you edited that has not yet produced a frame;
- **green** — an override that has produced a valid frame, or a value the decoder itself discovered
  at run time (it locks the baud rate, deviation, and precoding as it decodes).

An orange dot that turns green is the confirmation that your override was right. One that stays orange
means no frame has decoded with it yet.

The gear button's own color mirrors the dots, so you can see the state of the overrides without
opening the dialog: gray when there is nothing to show, orange while an override is waiting for a
confirming frame, and green once every override has produced one.

## Passes and Frames

The frames are grouped by pass. Each top-level node in the tree is one pass of one transmitter,
labeled with the start time, satellite name, and transmitter description. A pass node is created as
soon as the first burst is detected and stays grayed out until the first valid frame is decoded.

Each child node is a single decoded frame, labeled with the time of arrival, the frame length in
bytes, and, for AX.25 frames, the source and destination addresses. The newest pass is expanded
automatically, and the view scrolls to follow new frames as long as the latest frame is selected.

Select a **pass** node to see a summary in the detail pane: the start time, satellite, transmitter,
orbit number, the number of bursts, frames, and images decoded, and the signal parameters.

Select a **frame** node to see its full contents:

- **TELEMETRY** — the named telemetry values decoded from the frame (battery voltage, temperatures,
  and so on), shown only for satellites that SkyRoof has a telemetry definition for;
- **ASCII** — the frame bytes rendered as text;
- **HEX** — a hex dump of the frame bytes;
- **META** — the carrier frequency offset (CFO), signal-to-noise ratio (SNR), CRC check result, and
  the number of corrected bits and erased bytes from forward error correction.

## Sharing the Frames

As each frame is decoded it is also, depending on the
[decoder settings](setting_up_telemetry_decoding.md#decoder-settings):

- appended to a log file in the `TelemetryDecodes` subfolder of the [data folder](data_folder.md);
- sent to any client connected to the KISS-over-TCP server;
- uploaded to the [SatNOGS DB](https://db.satnogs.org/).

## Clearing the List

Right-click anywhere in the tree and choose **Clear All** to remove all passes and frames from the
panel. This clears only the display; frames already saved to file or uploaded are not affected.

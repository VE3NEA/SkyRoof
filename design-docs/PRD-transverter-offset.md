# PRD: Transverter Offset Support for SkyRoof

## Problem Statement

Users running HF transceivers connected to VHF/UHF transverters (e.g., K3S + Elecraft XV series,
IC-735 + any transverter) want to use SkyRoof's SDR as the receiver via the transverter's IF output
(28–30 MHz range). Currently SkyRoof always tunes the SDR to the actual RF frequency (144/435 MHz),
which requires the SDR to cover VHF/UHF. With a transverter setup, the SDR should be fixed to the
IF frequency band while SkyRoof continues to display and process everything at the correct RF frequency.

A secondary requirement applies to "dumb" HF rigs without built-in transverter configuration
(e.g., IC-735): SkyRoof must send the IF frequency (e.g., 29.950 MHz) to the radio via CAT instead
of the RF frequency (145.950 MHz), because the rig cannot interpret VHF/UHF frequencies.

---

## Goals

1. Fix the SDR center frequency to the IF band when transverter mode is active; track the
   Doppler-corrected signal by moving only the slicer offset, preserving waterfall history.
2. Optionally subtract the LO offset from CAT-commanded frequencies for rigs that have no built-in
   XVRT configuration.
3. Support independently enabling/disabling the offset for the SDR and each CAT channel (RX CAT,
   TX CAT).
4. Allow separate LO offset values for the SDR path and the CAT path per band, since the SDR
   transverter and the TX transverter may use different local oscillators.
5. Handle multiple transverter bands automatically based on frequency range, including the AO-7 UHF
   uplink sub-band exception.
6. Non-ham satellite frequencies (400 MHz telemetry band, etc.) receive zero offset — no behavior
   change.

## Non-Goals

- Changing what frequencies are *displayed* in the VFO widget or waterfall (always show RF).
- Automated switching between transverter profiles mid-pass (user sets configuration, software
  applies it).

---

## Architecture

### How Frequencies Flow Today

```
RadioLink.ComputeFrequencies()
  → CorrectedDownlinkFrequency  (RF, Doppler-adjusted)
  → CorrectedUplinkFrequency    (RF, Doppler-adjusted)

FrequencyWidget.RadioLinkToRadio()
  → ctx.Sdr.Frequency           = RF center  (tunes SDR hardware, moves waterfall)
  → ctx.Slicer.SetOffset()      = RF - SDR center (audio demodulator offset)
  → ctx.CatControl.Rx.SetRxFrequency(CorrectedDownlinkFrequency)   ← RF to radio
  → ctx.CatControl.Tx.SetTxFrequency(CorrectedUplinkFrequency)     ← RF to radio
```

### Proposed Change

Helpers on `TransverterSettings`:
- `GetSdrLoOffset(rfFrequency)` — returns the SDR LO for the matching SDR band (or 0)
- `GetCatLoOffset(rfFrequency)` — returns the CAT LO for the matching CAT band (or 0)
- `GetActiveSdrBand()` — returns the band the SDR is currently centered on (or null if transverter
  is disabled / no band active). Used by callers that need to convert the SDR's IF center back to
  RF for display (waterfall scale, ham-band detection, satellite-pass visibility).

The SDR's "active band" is the band selected the last time `SetSlicerFrequency()` retuned the SDR;
it changes only when the SDR is retuned, so a single reference is sufficient.

**SDR center frequency behavior depends on bandwidth constraints.**

When `SdrOffsetEnabled` is true and the active downlink frequency falls into a different SDR transverter
band than the SDR is currently covering, the SDR is retuned to the IF center of that band (midpoint
of `RfLow - LoOffset` … `RfHigh - LoOffset`). 

- **If SDR bandwidth ≥ transverter IF span:** The entire transverter passband fits in the SDR's view.
  SDR center is set once per band and never moves. Only the slicer offset tracks Doppler-corrected 
  IF signals as they drift within the SDR passband.

- **If SDR bandwidth < transverter IF span:** The SDR can only view part of the transverter passband.
  The slicer offset tracks the Doppler-corrected signal normally. When the signal drifts far enough 
  that it would move outside the SDR passband, `BringToPassband()` is called to move the SDR center 
  to recapture the signal—but the move is constrained so the entire SDR passband stays within the 
  transverter IF band limits. This keeps the signal visible while respecting the transverter boundaries.

```
On band change (SdrOffsetEnabled, current band ≠ previous band):
  ifSpanHz           = band.RfHigh - band.RfLow
  ifCenter           = (band.RfLow + band.RfHigh) / 2 - band.LoOffset
  ctx.Sdr.Frequency  = ifCenter            // one retune, waterfall resets here

Per Doppler tick:
  ifFrequency   = CorrectedDownlinkFrequency - GetSdrLoOffset(CorrectedDownlinkFrequency)
  ctx.Slicer.SetOffset(ifFrequency - ctx.Sdr.Frequency)
  
  // If SDR bandwidth is less than transverter IF span, track signal within passband
  if GetSdrBandwidth() < ifSpanHz:
    // Check if signal is still within SDR passband
    if ifFrequency outside [ctx.Sdr.Frequency ± GetSdrBandwidth()/2]:
      BringToPassband()  // move SDR center to keep signal visible, constrained by IF band
  // else: SDR passband encompasses entire IF band; slicer offset is sufficient

CAT (per Doppler tick, when offset enabled):
  RX CAT freq = CorrectedDownlinkFrequency - GetCatLoOffset(CorrectedDownlinkFrequency)
  TX CAT freq = CorrectedUplinkFrequency   - GetCatLoOffset(CorrectedUplinkFrequency)
```

Because Doppler is already baked into `CorrectedDownlinkFrequency` before the LO is subtracted,
the IF frequency sent to the radio inherits the same Doppler correction automatically. The LO
itself is fixed, so Doppler rate is unaffected.

**Passband coverage:** when SDR bandwidth ≥ transverter IF span, the SDR center stays fixed and the
entire IF band is always visible. When SDR bandwidth < transverter IF span (the default UHF 70cm
band has a 6 MHz IF span and VHF 2m has 4 MHz — both exceed the 3.1 MHz SDR maximum),
`BringToPassband()` repositions the SDR center as needed to keep the signal visible while ensuring
the SDR passband stays within the transverter band.

---

## Data Model

### New `TransverterBand`

Defines RF frequency range and LO offsets for both SDR and CAT paths.

```csharp
public class TransverterBand
{
  public string Name { get; set; }   // "VHF 2m", "UHF 70cm", "UHF AO-7 uplink"
  public long RfLow { get; set; }    // Hz, inclusive
  public long RfHigh { get; set; }   // Hz, inclusive
  public long LoOffset { get; set; } // Hz, subtracted from RF 
}
```

### New `TransverterSettings`

Maintains separate band lists for SDR and CAT paths. The SDR is constrained only by the transverter 
bandwidth; the CAT bandwidth is further constrained by the radio's IF receiver tuning range.

```csharp
public class TransverterSettings
{
  public bool SdrOffsetEnabled   { get; set; } = false;
  public bool RxCatOffsetEnabled { get; set; } = false;
  public bool TxCatOffsetEnabled { get; set; } = false;
  public List<TransverterBand> SdrBands { get; set; } = new();
  public List<TransverterBand> CatBands { get; set; } = new();

  internal void SetDefaults()
  {
    // null means the field was absent from JSON (pre-feature install) — seed defaults.
    // An empty list is a deliberate user choice and must be preserved.
    if (SdrBands == null) SdrBands = BuildDefaultSdrBands();
    if (CatBands == null) CatBands = BuildDefaultCatBands();
  }

  // Invoked from the PropertyGrid "Reset" context-menu command on the band lists.
  public void ResetSdrBands() => SdrBands = BuildDefaultSdrBands();
  public void ResetCatBands() => CatBands = BuildDefaultCatBands();
}
```

Defaults are seeded by `Settings.SetDefaults()` (see [Settings.cs:76](SkyRoof/Settings/Settings.cs#L76)) after JSON load, following the existing pattern used for `Ui.DockingLayoutString` and `Satellites.Sanitize()`:

```csharp
private void SetDefaults()
{
  if (Ui.DockingLayoutString == null)
    Ui.DockingLayoutString = Ui.DefaultDockingString;

  Satellites.Sanitize(true);
  Transverter.SetDefaults();   // NEW
}
```

Existing `Settings.json` files predating this feature lack the `Transverter` field; deserialization leaves the band-list properties as `null`, and `SetDefaults()` seeds them with the default tables below. An **empty list** (the user deleted every band via the PropertyGrid) is a deliberate user choice and is preserved across sessions — `SetDefaults()` only fills `null`, not empty. To re-populate after a manual clear, the user invokes the **Reset** command from the band-list context menu in the PropertyGrid (see UI section).

### Default SDR Band Table

| Name                  | RF Low (MHz) | RF High (MHz) |   LO (MHz)   | IF Range         |
|-----------------------|--------------|---------------|--------------|------------------|
| VHF 2m                | 144.000      | 148.000       | 116.000      | 28.0–32.0 MHz    |
| UHF 70cm              | 432.000      | 438.000       | 407.000      | 25.0–31.0 MHz    |

The SDR band table covers the full ham VHF allocation (144–148 MHz) and the satellite portion of
the UHF allocation (432–438 MHz). Even when the transverter IF span exceeds the SDR bandwidth,
`BringToPassband()` repositions the SDR center as needed to keep the active satellite signal visible
while staying within the transverter band. The AO-7 mode U-V uplink (432–434 MHz) falls within the
UHF entry and no longer needs its own row.

### Alternative SDR Band Table (10.7 MHz IF)

For receivers with a 10.7 MHz IF output used as transverters, e.g., the IC-R7000 wideband communications receiver
(IF = 10.7 MHz, IF bandwidth ≈ 6 MHz):

| Name                  | RF Low (MHz) | RF High (MHz) |   LO (MHz)   | IF Range         |
|-----------------------|--------------|---------------|--------------|------------------|
| VHF 2m                | 144.000      | 148.000       | 135.300      | 8.7–12.7 MHz     |
| UHF 70cm              | 432.000      | 438.000       | 424.300      | 7.7–13.7 MHz     |

Both bands are centered on 10.7 MHz IF. The IC-R7000's 6 MHz IF bandwidth comfortably covers the
VHF 2m IF span (4 MHz) and exactly matches the UHF 70cm IF span (6 MHz). Users select between this
and the 28–30 MHz IF default by editing the `LoOffset` values in the PropertyGrid.

### Default CAT Band Table

| Name                  | RF Low (MHz) | RF High (MHz) |   LO (MHz)   | IF Range         |
|-----------------------|--------------|---------------|--------------|------------------|
| VHF 2m                | 145.800      | 146.000       | 116.000      | 29.8–30.0 MHz    |
| UHF 70cm AO-7 uplink  | 432.000      | 434.000       | 404.000      | 28.0–30.0 MHz    |
| UHF 70cm              | 435.000      | 437.000       | 407.000      | 28.0–30.0 MHz    |

CAT RF ranges reflect actual satellite allocations: 145.8–146.0 MHz for VHF and 435–437 MHz for UHF
(the 437–438 MHz range is still covered by the SDR but is omitted from CAT to stay within the 28–30
MHz IF tuning range of typical HF rigs).
The AO-7 mode U-V uplink (432–434 MHz) uses a different LO (404 vs 407 MHz) and is split into its own
CAT entry so frequency-range lookup resolves it automatically.

**SDR passband constraints:** The SDR only has the transverter bandwidth as a constraint (no receiver 
tuning range limit). With the wide default SDR bands:
- VHF 2m: 4 MHz IF span — when SDR bandwidth ≥ 4 MHz, the entire band is always visible; when smaller, 
  `BringToPassband()` keeps the signal on-screen
- UHF 70cm: 6 MHz IF span — when SDR bandwidth ≥ 6 MHz, the entire band is always visible; when smaller,
  `BringToPassband()` repositions the SDR center as the satellite frequency drifts, keeping the signal
  visible within the transverter band

**CAT passband constraints:** The CAT bandwidth is the lower of the transverter IF bandwidth and the 
radio's receiver tuning range, which varies by equipment. The default CAT bands use narrow RF ranges 
so the resulting IF lands inside a typical HF-rig IF tuning range, independent of the wider SDR bands.

Non-ham frequencies (e.g., 400 MHz band) fall outside all bands → both `GetSdrLoOffset()` and
`GetCatLoOffset()` return 0 → SDR and CAT behave exactly as today.

---

## Behavior Guarantees

When `SdrOffsetEnabled` is true:
- The SDR center is determined solely by the **downlink** RF; uplink-only operations (cross-band
  TX, key-down with no active downlink) do not retune the SDR.
- The entire SDR passband always stays within the transverter IF band limits.
- Doppler-corrected satellite signals always remain visible on the waterfall as long as they are 
  within the transverter band (no off-screen loss due to SDR bandwidth constraints).
- Waterfall history is preserved as long as the SDR center is unchanged; it resets only when the SDR
  is retuned (initial band entry or `BringToPassband()` reposition).
- The waterfall scale always displays RF frequencies, regardless of the SDR's actual IF tuning, by
  routing all SDR-center reads through `FrequencyWidget.GetSdrRfCenter()`.
- If `RxCatOffsetEnabled` or `TxCatOffsetEnabled` is set and the active RF doesn't match any CAT
  band, the corresponding CAT frequency command is skipped and a warning is logged — raw RF is
  never sent to a rig that cannot interpret it.

---

## UI: PropertyGrid Configuration

All transverter settings are edited via the main Settings PropertyGrid (alongside CAT, Audio, etc.),
not in a dedicated dialog. The PropertyGrid displays:

- `TransverterSettings.SdrOffsetEnabled` — checkbox to enable SDR offset
- `TransverterSettings.RxCatOffsetEnabled` — checkbox to enable RX CAT offset
- `TransverterSettings.TxCatOffsetEnabled` — checkbox to enable TX CAT offset
- `TransverterSettings.SdrBands` — collection of `TransverterBand` entries for SDR path
- `TransverterSettings.CatBands` — collection of `TransverterBand` entries for CAT path

The `SdrBands` and `CatBands` properties open a collection editor (e.g., the built-in
`System.ComponentModel.Design.CollectionEditor`, attached via `[Editor(typeof(CollectionEditor), ...)]`)
that lets the user **add new bands, edit existing ones, and delete bands** — including all
default-seeded entries. The lists are not fixed; a user might prefer to keep only the bands relevant
to their station, or define additional bands for non-standard transverter ranges (e.g., 1296 MHz
with a 1268 MHz LO).

Each band entry in the collection editor allows editing:
- `Name` — band identifier (string)
- `RfLow` / `RfHigh` — RF frequency range in Hz (or displayed in MHz via custom formatter)
- `LoOffset` — LO offset in Hz

An emptied list is preserved across sessions — `Settings.SetDefaults()` only seeds the lists when
they are `null` (i.e., absent from JSON on first run after upgrade), not when they are empty. To
restore the default entries after deleting them, the user opens the **context (right-click) menu**
on the `SdrBands` or `CatBands` property in the PropertyGrid and selects **Reset**, which calls
`TransverterSettings.ResetSdrBands()` / `ResetCatBands()`. The Reset command is also available when
the list is non-empty; it replaces the entire list with the default entries.

---

## Behavior by Use Case

### Case 1: K3S with XVRT config + SDR on transverter IF
- `SdrOffsetEnabled = true`, `RxCatOffsetEnabled = false`, `TxCatOffsetEnabled = false`
- On first use of the UHF 70cm band: SDR center tuned to the IF band center (≈ 28 MHz, midpoint of 
  25–31 MHz). If SDR bandwidth is less than the 6 MHz IF span, `BringToPassband()` repositions the 
  SDR center to follow the satellite frequency as needed. Switching to a VHF satellite triggers a 
  retune to the VHF IF center (≈ 30 MHz, midpoint of 28–32 MHz). Doppler shifts the slicer offset 
  within the passband.
- K3S receives 435.835 MHz from SkyRoof (unchanged) and applies its own XVRT conversion internally.

### Case 2: IC-7300 (no transverter awareness) + SDR on transverter IF
- `SdrOffsetEnabled = true`, `RxCatOffsetEnabled = true`, `TxCatOffsetEnabled = true`
- Example FO-29 (Doppler off for clarity):
  - Downlink 435.835 MHz → SDR IF ≈ 28.835 MHz, repositioned within UHF SDR band as needed;
    IC-7300 RX commanded to 28.835 MHz (using CAT UHF 70cm entry, LO = 407 MHz)
  - Uplink 145.965 MHz → IC-7300 TX commanded to 29.965 MHz (using CAT VHF 2m entry, LO = 116 MHz)
  - The IC-7300 covers 1.8–54 MHz continuously, comfortably handling both IF frequencies.

### Case 3: SDR directly on VHF/UHF antenna, smart rig
- All offsets disabled. No change in behavior.

### Case 4: AO-7 mode U-V
- Uplink 432.xxx MHz → matches CAT "UHF 70cm AO-7 uplink" band → CAT LO = 404 MHz → IF ≈ 28.xxx MHz
- Downlink 145.xxx MHz → SDR matches "VHF 2m" band → SDR LO = 116 MHz → IF ≈ 29.xxx MHz
- The CAT table preserves the AO-7-specific LO; the SDR table's wide UHF entry covers 432–434 MHz 
  with LO = 407 MHz (acceptable since SDR reception is not used for the uplink frequency).

### Case 5: Non-ham telemetry satellite (e.g., 400.550 MHz)
- No matching band → offsets = 0 → SDR tunes to 400.550 MHz directly → no change.

---

## Code Changes

| File | Change |
|------|--------|
| `Settings/TransverterSettings.cs` | **New.** `TransverterBand` and `TransverterSettings` with separate `SdrBands` and `CatBands` lists; helpers `GetSdrLoOffset(rfFrequency)`, `GetCatLoOffset(rfFrequency)`, `GetSdrBand(rfFrequency)`, and `GetActiveSdrBand()` |
| `Settings/Settings.cs` | Add `public TransverterSettings Transverter { get; set; } = new();` |
| `RadioLink.cs` | No change needed — offset is applied downstream |
| `Widgets/FrequencyWidget.cs` — `SettingsToRadioLink()` | When `SdrOffsetEnabled` and the downlink band has changed (or first activation), retune SDR center to `(band.RfLow + band.RfHigh) / 2 - band.LoOffset` |
| `Widgets/FrequencyWidget.cs` — `ClockTick()` | Add the same band-change check on every Doppler tick: if `SdrOffsetEnabled` and the corrected downlink RF resolves to a different SDR band than the SDR is currently centered on, retune (covers in-pass band crossings that `SettingsToRadioLink` would miss) |
| `Widgets/FrequencyWidget.cs` — `RadioLinkToRadio()` | When `RxCatOffsetEnabled`/`TxCatOffsetEnabled`, apply `GetCatLoOffset()` before calling `SetRxFrequency` / `SetTxFrequency`. If the RF doesn't match any CAT band, log a warning and skip the CAT call (avoid sending raw RF to a rig that can't tune it) |
| `Widgets/FrequencyWidget.cs` — `SetSlicerFrequency()` | When `SdrOffsetEnabled`, compute `ifFrequency = CorrectedDownlinkFrequency - GetSdrLoOffset(...)`; update slicer offset. If SDR bandwidth < transverter IF span and signal drifts outside SDR passband, call `BringToPassband(ifFrequency, bandLow - LoOffset, bandHigh - LoOffset)` to reposition |
| `Widgets/FrequencyWidget.cs` — `BringToPassband()` | **Signature change.** Generalize to `BringToPassband(double frequency, double bandLow, double bandHigh)`; existing callers pass `(freq, SdrConst.VHF_CENTER_FREQUENCY - MAX_BANDWIDTH/2, ... + MAX_BANDWIDTH/2)` and the UHF equivalent; transverter callers pass the IF band limits derived from the active `TransverterBand`. The hardcoded VHF/UHF branch dispatch is removed |
| `Widgets/FrequencyWidget.cs` — `GetSdrRfCenter()` | **New helper.** Returns `ctx.Sdr.Info.Frequency + activeBand.LoOffset` when transverter is active (active band tracked by the widget that owns SDR retuning), otherwise `ctx.Sdr.Info.Frequency`. Lives on `FrequencyWidget` next to the other frequency-math members (`SetSlicerFrequency`, `BringToPassband`), not on `Context` |
| `Widgets/FrequencyScale.cs` | Replace direct reads of `ctx.Sdr.Info.Frequency` (line 21 default, `CenterFrequency` updates) with `ctx.FrequencyControl.GetSdrRfCenter()` so the scale displays RF when transverter is active. `hamBand` test (line 321) likewise uses RF center |
| `Widgets/FrequencyScale.cs` — `BuildLabels()` | The `hamBand` check must use RF, not IF, so the correct `passes` collection (Ham vs Sdr) is selected |
| `Panels/WaterfallPanel.cs` | `SdrCenterFrequency` property (line 9) and `SetPassband()` / `SetCenterFrequency()` callers should use `ctx.FrequencyControl.GetSdrRfCenter()` for the scale center while the OpenGL pan/zoom still uses the raw IF `Sdr.Info.Frequency` |
| `Satellites/SatellitePasses.cs` | `UpdateFrequencyRange` (≈ line 98) uses `ctx.Sdr.Info.Frequency` to filter visible passes; convert via `ctx.FrequencyControl.GetSdrRfCenter()` so labels match the displayed RF axis |
| `Settings/Settings.cs` — `SetDefaults()` | Add `Transverter.SetDefaults()` call after `Satellites.Sanitize(true)` so empty band lists from new installs (or upgraded JSON without the field) are populated with the default tables |
| `Settings/SettingsDialog.cs` | Integrate `TransverterSettings` into main PropertyGrid via `ExpandableObjectConverter`; for the `SdrBands` / `CatBands` lists, provide a collection-editor (e.g., `System.ComponentModel.Design.CollectionEditor`) so users can **add, edit, and delete** band entries. Hook the PropertyGrid's `PropertyValueChanged` / context-menu event so a right-click on `SdrBands` or `CatBands` shows a **Reset** item that calls `ResetSdrBands()` / `ResetCatBands()` and refreshes the grid |

---

## Waterfall Display

When transverter offset is enabled, the waterfall frequency scale continues to show RF frequencies
(e.g., 144–148 MHz, 432–438 MHz), not IF frequencies. The SDR hardware is tuned to the IF band
internally, but the displayed scale is offset by the active band's `LoOffset` so the user always
sees the actual satellite RF frequency. This matches the VFO widget, which also always shows RF.

The frequency axis labels and any per-band ticks are computed via `FrequencyWidget.GetSdrRfCenter()`,
which adds the active band's `LoOffset` back to the SDR's IF center frequency before rendering.

---

## Open Questions / Decisions Deferred

1. **Waterfall indicator:** A small transverter-active indicator on the waterfall could be useful
   (similar to the Doppler correction indicator). Low priority; can be added later.

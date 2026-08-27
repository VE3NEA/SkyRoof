using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VE3NEA;

namespace SkyRoof
{
  public partial class FrequencyWidget : UserControl
  {
    public Context ctx;
    public RadioLink RadioLink = new();
    private bool Changing;
    private readonly FrequencyEntryForm FrequencyDialog = new();
    private readonly Slicer.Mode[] LsbModes = [Slicer.Mode.LSB, Slicer.Mode.LSB_D];
    private readonly Slicer.Mode[] FmModes = [Slicer.Mode.FM, Slicer.Mode.FM_D];

    // Transverter band the SDR is currently centered on (null when transverter is disabled
    // or no SDR band matches the active RF). Owned by XverterSetSlicerFrequency, which uses it
    // to detect band changes and retune the SDR to the center of the new band.
    private TransverterBand? XverterBand;

    // cached so the Terrestrial/normal toggle does not allocate (and leak) a new font each update
    private readonly Font DownlinkRegularFont, DownlinkBoldFont;

    public FrequencyWidget()
    {
      InitializeComponent();
      DownlinkRegularFont = DownlinkLabel.Font;
      DownlinkBoldFont = new Font(DownlinkLabel.Font, FontStyle.Bold);
      Changing = true;
      DownlinkModeCombobox.DataSource = Enum.GetValues(typeof(Slicer.Mode));
      UplinkModeCombobox.DataSource = Enum.GetValues(typeof(Slicer.Mode));
      DownlinkModeCombobox.SelectedIndex = 0;
      UplinkModeCombobox.SelectedIndex = 0;
      Changing = false;
      BuildCtcssMenu();
    }

    internal string GetBandName(bool uplink)
    {
      var freq = uplink ? RadioLink.CorrectedUplinkFrequency : RadioLink.CorrectedDownlinkFrequency;
      if (SatnogsDbTransmitter.IsUhfFrequency(freq))
        return "70cm";
      else if (SatnogsDbTransmitter.IsVhfFrequency(freq))
        return "2m";
      else
        return string.Empty;
    }




    //----------------------------------------------------------------------------------------------
    //                                 set from outside
    //----------------------------------------------------------------------------------------------
    public void SetTransmitter()
    {
      SettingsToRadioLink(false);
      RadioLinkToUi();
      ctx.CatControl.ApplyTune();
      ctx.RotatorControl.SetSatellite(ctx.SatelliteSelector.SelectedSatellite);
      RadioLinkToRadio();
      UpdateTxButton();
    }

    public void SetTerrestrialFrequency(double frequency)
    {
      SettingsToRadioLink(true, frequency);
      RadioLinkToUi();
      ctx.CatControl.ApplyTune();
      ctx.RotatorControl.SetSatellite(null);
      RadioLinkToRadio();
      UpdateTxButton();
      // the telemetry panel decodes terrestrial signals from manually entered parameters, so it must learn
      // that the radio has moved: nothing else tells it, and the tuned frequency is the pass identity there
      ctx.TelemetryPanel?.SetTransmitter();
    }

    internal void SetTransponderOffset(SatnogsDbTransmitter transponder, double offset)
    {
      // set the offset first
      var transponderCust = ctx.Settings.Satellites.GetOrCreateTransmitterCustomization(transponder);
      Debug.Assert(offset >= 0 && offset <= transponder.uplink_high - transponder.uplink_low);
      transponderCust.TransponderOffset = offset;

      // if same TX, just force its settings in case we were in terrestrial mode and changed them
      if (transponder == RadioLink.Tx) SetTransmitter();

      // if different TX, select it for all panels in the app
      else ctx.SatelliteSelector.SetSelectedTransmitter(transponder);
    }

    internal void IncrementDownlinkFrequency(int delta)
    {
      //Ctrl-mousewheel-spin enables RIT
      RadioLink.RitEnabled = ModifierKeys.HasFlag(Keys.Control);
      RadioLink.IncrementDownlinkFrequency(delta);
      RadioLinkToRadio();
      RadioLinkToUi();
    }

    internal double GetDraggableFrequency()
    {
      return RadioLink.GetDraggableFrequency();
    }

    internal void SetDraggableFrequency(double freq)
    {
      RadioLink.SetDraggableFrequency(freq);
      RadioLinkToRadio();
      RadioLinkToUi();
    }

    internal void ClockTick()
    {
      if (RadioLink.IsTerrestrial) return;
      RadioLink.ObserveSatellite(ctx.SdrPasses);
      RadioLink.ComputeFrequencies();
      RadioLinkToRadio();
      FrequenciesToUi();
    }

    internal void RxTuned()
    {
      int delta = (int)(ctx.CatControl.Rx!.LastReadRxFrequency - RadioLink.CorrectedDownlinkFrequency);
      RadioLink.IncrementDownlinkFrequency(delta);
      RadioLinkToRadio();
      BeginInvoke(RadioLinkToUi);
    }

    internal void TxTuned()
    {
      // when FT4 XIT is on, ignore dial knob
      if (RadioLink.XitOffset != 0) return;

      int delta = (int)(ctx.CatControl.Tx!.LastReadTxFrequency - RadioLink.CorrectedUplinkFrequency);
      RadioLink.IncrementUplinkFrequency(delta);
      RadioLinkToRadio();
      BeginInvoke(RadioLinkToUi);
    }

    internal void ToggleRit()
    {
      RadioLink.RitEnabled = !RadioLink.RitEnabled;
      RadioLink.ComputeFrequencies();
      RadioLinkToRadio();
      RadioLinkToUi();
    }

    internal void SetRitFrequency(double frequency)
    {
      var currentFrequency = RadioLink.CorrectedDownlinkFrequency;
      if (RadioLink.RitEnabled) currentFrequency -= RadioLink.RitOffset;
      var delta = frequency - currentFrequency;

      if (Math.Abs(delta) > 25000) return;

      RadioLink.RitEnabled = true;
      RadioLink.RitOffset = delta;
      RadioLink.ComputeFrequencies();

      RadioLinkToRadio();
      RadioLinkToUi();
    }

    internal void SetPtt(bool ptt)
    {
      if (ctx.CatControl.Tx == null) return;

      ctx.CatControl.Tx!.SetPtt(ptt);
      UpdateTxButton();
    }

    // from FT4 self-decode
    internal void AdjustUplinkOffset(double error)
    {
      bool invert = RadioLink.Tx?.invert == true ^ LsbModes.Contains(RadioLink.DownlinkMode); 
      RadioLink.UplinkManualCorrection -= RadioLink.Tx?.invert == true ? -error : error;
      RadioLink.ComputeFrequencies();
      RadioLinkToRadio();
      FrequenciesToUi();
    }

    // FT4 XIT
    internal void SetXit(double xit)
    {
      RadioLink.XitOffset = RadioLink.Tx?.invert == true ? -xit : xit;
      RadioLink.ComputeFrequencies();
      RadioLinkToRadio();
      FrequenciesToUi();
    }





    //----------------------------------------------------------------------------------------------
    //                                     internal set
    //----------------------------------------------------------------------------------------------
    public void SettingsToRadioLink(bool isTerrestrial, double? frequency = null)
    {
      RadioLink.IsTerrestrial = isTerrestrial;
      if (isTerrestrial) RadioLink.DownlinkFrequency = frequency!.Value;

      RadioLink.RitEnabled = false;
      RadioLink.RitOffset = 0;

      if (!isTerrestrial)
      {
        RadioLink.Sat = ctx.SatelliteSelector.SelectedSatellite;
        RadioLink.Tx = ctx.SatelliteSelector.SelectedTransmitter;
        RadioLink.SatCust = ctx.Settings.Satellites.SatelliteCustomizations.GetOrCreate(RadioLink.Sat.sat_id);
        RadioLink.TxCust = ctx.Settings.Satellites.GetOrCreateTransmitterCustomization(RadioLink.Tx);
        RadioLink.ObserveSatellite(ctx.SdrPasses);
        isTerrestrial = RadioLink.IsTerrestrial; 

      }

      if (!isTerrestrial)
      {
        RadioLink.DownlinkDopplerCorrectionEnabled = true;
        RadioLink.DownlinkManualCorrectionEnabled = true;
        RadioLink.UplinkDopplerCorrectionEnabled = true;
        RadioLink.UplinkManualCorrectionEnabled = true;
      }

      RadioLink.ComputeFrequencies();
    }

    private void RadioLinkToRadio()
    {
      // mode in slicer
      if (ctx.Slicer != null) ctx.Slicer.NewMode = RadioLink.DownlinkMode;

      // mode in external radio
      ctx.CatControl.Rx?.SetRxMode(RadioLink.DownlinkMode);
      ctx.CatControl.Tx?.SetTxMode(RadioLink.UplinkMode);

      // ctcss tone of this transmitter in external radio
      ctx.CatControl.Tx?.SetCtcssTone(RadioLink.CtcssTone, RadioLink.CtcssEnabled);

      // freq in slicer
      if (ctx.Settings.Transverter.SdrOffsetEnabled)
        XverterSetSlicerFrequency();
      else
        SetSlicerFrequency();

      // freq in external radio (with optional transverter CAT offset)
      if (ctx.CatControl.Rx != null)
        SendCatRxFrequency(RadioLink.CorrectedDownlinkFrequency);

      if (RadioLink.CorrectedUplinkFrequency != 0 && ctx.CatControl.Tx != null)
        SendCatTxFrequency(RadioLink.CorrectedUplinkFrequency);

      // refresh the LED labels so the yellow "no matching CAT band" state tracks the active RF
      var transverter = ctx.Settings.Transverter;
      if (transverter.RxCatOffsetEnabled || transverter.TxCatOffsetEnabled)
        ctx.MainForm?.ShowCatStatus();
    }

    private void SendCatRxFrequency(double rxRf)
    {
      var transverter = ctx.Settings.Transverter;
      if (transverter.RxCatOffsetEnabled)
      {
        var band = transverter.GetCatBand(rxRf);
        if (band == null)
          Serilog.Log.Warning($"RX CAT transverter offset enabled but no CAT band matches RF {rxRf:n0} Hz — skipping CAT command");
        else
          ctx.CatControl.Rx!.SetRxFrequency((long)Math.Truncate(rxRf - band.LoOffset));
      }
      else
        ctx.CatControl.Rx!.SetRxFrequency((long)Math.Truncate(rxRf));
    }

    private void SendCatTxFrequency(double txRf)
    {
      var transverter = ctx.Settings.Transverter;
      if (transverter.TxCatOffsetEnabled)
      {
        var band = transverter.GetCatBand(txRf);
        if (band == null)
          Serilog.Log.Warning($"TX CAT transverter offset enabled but no CAT band matches RF {txRf:n0} Hz — skipping CAT command");
        else
          ctx.CatControl.Tx!.SetTxFrequency((long)Math.Truncate(txRf - band.LoOffset));
      }
      else
        ctx.CatControl.Tx!.SetTxFrequency((long)Math.Truncate(txRf));
    }

    // True when RX CAT transverter offset is enabled but the current downlink RF falls outside
    // every configured CAT band — no CAT frequency is being sent in this state.
    public bool IsRxCatTransverterOutOfBand =>
      ctx.Settings.Transverter.RxCatOffsetEnabled &&
      ctx.Settings.Transverter.GetCatBand(RadioLink.CorrectedDownlinkFrequency) == null;

    // True when TX CAT transverter offset is enabled but the current uplink RF falls outside
    // every configured CAT band — no CAT frequency is being sent in this state.
    public bool IsTxCatTransverterOutOfBand =>
      ctx.Settings.Transverter.TxCatOffsetEnabled &&
      ctx.Settings.Transverter.GetCatBand(RadioLink.CorrectedUplinkFrequency) == null;

    public string GetRxCatTransverterOutOfBandMessage() =>
      $"RX CAT transverter offset is enabled, but no CAT transverter band covers the downlink frequency ({RadioLink.CorrectedDownlinkFrequency:n0} Hz). No frequency is being sent to the radio.";

    public string GetTxCatTransverterOutOfBandMessage() =>
      $"TX CAT transverter offset is enabled, but no CAT transverter band covers the uplink frequency ({RadioLink.CorrectedUplinkFrequency:n0} Hz). No frequency is being sent to the radio.";

    private void SetSlicerFrequency()
    {
      if (ctx.Sdr?.Enabled != true) return;

      double bandwidth = ctx.Sdr.Info.MaxBandwidth;
      double low = ctx.Sdr.Frequency - bandwidth / 2;
      double high = ctx.Sdr.Frequency + bandwidth / 2;

      double targetFrequency = RadioLink.CorrectedDownlinkFrequency!;

      if (targetFrequency < low || targetFrequency > high)
        if (ctx.Sdr.IsFrequencySupported(targetFrequency))
        {
          BringToPassband(targetFrequency);
          ctx.WaterfallPanel?.SetCenterFrequency(ctx.Sdr.Info.Frequency);
        }
        else
          return;

      low = ctx.Sdr.Frequency - bandwidth / 2;
      high = ctx.Sdr.Frequency + bandwidth / 2;

      if (targetFrequency >= low && targetFrequency <= high)
        if (ctx.Slicer?.Enabled == true)
        {
          // terrestrial has no doppler ramp; otherwise extrapolate at the last estimated rate
          double rate = RadioLink.IsTerrestrial ? 0 : RadioLink.DownlinkDopplerRate;
          ctx.Slicer.SetOffset(targetFrequency - ctx.Sdr.Frequency, rate);
        }
    }

    private bool BringToPassband(double frequency)
    {
      double bandWing = SdrConst.MAX_BANDWIDTH / 2;
      double sdrWing = ctx.Sdr!.Info.MaxBandwidth / 2;

      bool uhf = frequency >= SdrConst.UHF_CENTER_FREQUENCY - bandWing &&
        frequency <= SdrConst.UHF_CENTER_FREQUENCY + bandWing;

      if (uhf)
      {
        double minCenter = SdrConst.UHF_CENTER_FREQUENCY - bandWing + sdrWing;
        double maxCenter = SdrConst.UHF_CENTER_FREQUENCY + bandWing - sdrWing;
        frequency = Math.Max(minCenter, Math.Min(maxCenter, frequency));
      }

      bool vhf = frequency >= SdrConst.VHF_CENTER_FREQUENCY - bandWing &&
        frequency <= SdrConst.VHF_CENTER_FREQUENCY + bandWing;

      if (vhf)
      {
        double minCenter = SdrConst.VHF_CENTER_FREQUENCY - bandWing + sdrWing;
        double maxCenter = SdrConst.VHF_CENTER_FREQUENCY + bandWing - sdrWing;
        frequency = Math.Max(minCenter, Math.Min(maxCenter, frequency));
      }

      if (ctx.Sdr.IsFrequencySupported(frequency))
      {
        ctx.Sdr.Frequency = frequency;
        return true;
      }
      else return false;
    }




    //----------------------------------------------------------------------------------------------
    //                                       transverter
    //----------------------------------------------------------------------------------------------
    // Slicer tuning when a transverter feeds the SDR: the SDR is tuned to the IF, which is the
    // RF minus the LO offset of the transverter band. When no band covers the RF, no offset
    // applies and the no-transverter code does the tuning.
    private void XverterSetSlicerFrequency()
    {
      if (ctx.Sdr?.Enabled != true) return;

      double rfFrequency = RadioLink.CorrectedDownlinkFrequency!;
      var band = ctx.Settings.Transverter.GetSdrBand(rfFrequency);

      if (band == null)
      {
        XverterBand = null;
        SetSlicerFrequency();
        return;
      }

      var prevBand = XverterBand;
      XverterBand = band;

      double bandwidth = ctx.Sdr.Info.MaxBandwidth;
      double ifFrequency = rfFrequency - band.LoOffset;
      double bandIfLow = band.RfLow - band.LoOffset;
      double bandIfHigh = band.RfHigh - band.LoOffset;

      // on first activation or band crossing, retune the SDR to the IF center of the new band
      // so that the entire IF band is in view (when bandwidth allows)
      if (band != prevBand)
      {
        double ifCenter = (bandIfLow + bandIfHigh) / 2;
        if (ctx.Sdr.IsFrequencySupported(ifCenter))
        {
          ctx.Sdr.Frequency = ifCenter;
          ctx.WaterfallPanel?.SetCenterFrequency(GetSdrRfCenter());
        }
      }

      double low = ctx.Sdr.Frequency - bandwidth / 2;
      double high = ctx.Sdr.Frequency + bandwidth / 2;

      if (ifFrequency < low || ifFrequency > high)
        if (ctx.Sdr.IsFrequencySupported(ifFrequency))
        {
          XverterBringToPassband(ifFrequency, band);
          ctx.WaterfallPanel?.SetCenterFrequency(GetSdrRfCenter());
        }
        else
          return;

      low = ctx.Sdr.Frequency - bandwidth / 2;
      high = ctx.Sdr.Frequency + bandwidth / 2;

      if (ifFrequency >= low && ifFrequency <= high)
        if (ctx.Slicer?.Enabled == true)
        {
          // terrestrial has no doppler ramp; otherwise extrapolate at the last estimated rate
          double rate = RadioLink.IsTerrestrial ? 0 : RadioLink.DownlinkDopplerRate;
          ctx.Slicer.SetOffset(ifFrequency - ctx.Sdr.Frequency, rate);
        }
    }

    private bool XverterBringToPassband(double ifFrequency, TransverterBand band)
    {
      double sdrWing = ctx.Sdr!.Info.MaxBandwidth / 2;
      double bandIfLow = band.RfLow - band.LoOffset;
      double bandIfHigh = band.RfHigh - band.LoOffset;

      // constrain the new SDR center so that the passband stays within the transverter band
      if (bandIfHigh - bandIfLow >= 2 * sdrWing)
      {
        double minCenter = bandIfLow + sdrWing;
        double maxCenter = bandIfHigh - sdrWing;
        ifFrequency = Math.Max(minCenter, Math.Min(maxCenter, ifFrequency));
      }

      if (ctx.Sdr.IsFrequencySupported(ifFrequency))
      {
        ctx.Sdr.Frequency = ifFrequency;
        return true;
      }
      else return false;
    }

    // Returns the RF center frequency of the SDR.
    // When the transverter is active on a band, adds the LO offset back to the SDR's IF center
    // so callers see the displayed RF (e.g., for waterfall scale and ham-band detection).
    public double GetSdrRfCenter()
    {
      if (ctx?.Sdr == null) return 0;

      var xverter = ctx.Settings.Transverter;
      if (!xverter.SdrOffsetEnabled) return ctx.Sdr.Info.Frequency;

      var band = xverter.GetSdrBand(RadioLink.CorrectedDownlinkFrequency);
      return ctx.Sdr.Info.Frequency + (band?.LoOffset ?? 0);
    }





    //----------------------------------------------------------------------------------------------
    //                                settings to/from UI
    //----------------------------------------------------------------------------------------------
    private void RadioLinkToUi()
    {
      Changing = true;

      DownlinkModeCombobox.SelectedItem = RadioLink.DownlinkMode;
      UplinkModeCombobox.SelectedItem = RadioLink.UplinkMode;

      RitCheckbox.Checked = RadioLink.RitEnabled;
      RitSpinner.Value = (decimal)(RadioLink.RitOffset / 1000d);

      // downlink
      if (RadioLink.IsTerrestrial)
      {
        DownlinkManualSpinner.Value = 0;

        DownlinkLabel.Text = "Terrestrial";
        DownlinkLabel.ForeColor = Color.Red;
        DownlinkLabel.Font = DownlinkBoldFont;

        DownlinkDopplerCheckbox.Visible = false;
        DownlinkDopplerLabel.BackColor = SystemColors.Control;

        DownlinkManualCheckbox.Visible = false;
        DownlinkManualSpinner.BackColor = SystemColors.Control;

        DownlinkManualSpinner.Enabled = false;
        label3.Enabled = label4.Enabled = false;
      }
      else
      {
        DownlinkDopplerCheckbox.Checked = RadioLink.DownlinkDopplerCorrectionEnabled;
        DownlinkManualCheckbox.Checked = RadioLink.DownlinkManualCorrectionEnabled;
        DownlinkManualSpinner.Value = (decimal)(RadioLink.DownlinkManualCorrection / 1000d);

        DownlinkLabel.Text = "Downlink";
        DownlinkLabel.ForeColor = SystemColors.ControlText;
        DownlinkLabel.Font = DownlinkRegularFont;

        DownlinkDopplerCheckbox.Visible = true;
        DownlinkDopplerLabel.BackColor = SystemColors.Window;

        DownlinkManualCheckbox.Visible = true;
        DownlinkManualSpinner.BackColor = SystemColors.Window;


        DownlinkManualSpinner.Enabled = true;
        label3.Enabled = label4.Enabled = true;
      }

      // uplink
      if (!RadioLink.HasUplink)
      {
        UplinkManualSpinner.Value = 0;

        UplinkLabel.Text = "No Uplink";

        UplinkDopplerCheckbox.Visible = false;
        UplinkDopplerLabel.BackColor = SystemColors.Control;

        UplinkManualCheckbox.Visible = false;
        UplinkManualSpinner.BackColor = SystemColors.Control;

        UplinkManualSpinner.Enabled = UplinkModeCombobox.Enabled = false;
        label5.Enabled = label6.Enabled = false;
      }
      else
      {
        UplinkDopplerCheckbox.Checked = RadioLink.UplinkDopplerCorrectionEnabled;
        UplinkManualCheckbox.Checked = RadioLink.UplinkManualCorrectionEnabled;
        UplinkManualSpinner.Value = (decimal)(RadioLink.UplinkManualCorrection / 1000d);

        UplinkLabel.Text = "Uplink";

        UplinkDopplerCheckbox.Visible = true;
        UplinkDopplerLabel.BackColor = SystemColors.Window;

        UplinkManualCheckbox.Visible = true;
        UplinkManualSpinner.BackColor = SystemColors.Window;

        UplinkManualSpinner.Enabled = UplinkModeCombobox.Enabled = true;
        label5.Enabled = label6.Enabled = true;
      }

      Changing = false;

      FrequenciesToUi();
      ctx.WaterfallPanel?.ScaleControl.Refresh();
      ctx.WaterfallPanel?.WaterfallControl.OpenglControl.Refresh();
    }

    private void FrequenciesToUi()
    {
      if (ctx.Settings.Cat.RxCat.ShowCorrectedFrequency)
        DownlinkFrequencyLabel.Text = $"{RadioLink.CorrectedDownlinkFrequency:n0}*";
      else
        DownlinkFrequencyLabel.Text = $"{RadioLink.DownlinkFrequency:n0}";

      if (RadioLink.UplinkFrequency == 0)
        UplinkFrequencyLabel.Text = "000,000,000";
      else if (ctx.Settings.Cat.TxCat.ShowCorrectedFrequency)
        UplinkFrequencyLabel.Text = $"{RadioLink.CorrectedUplinkFrequency:n0}*";
      else
        UplinkFrequencyLabel.Text = $"{RadioLink.UplinkFrequency:n0}";

      string sign = RadioLink.DopplerFactor >= 0 ? "" : "+";
      double offset = -RadioLink.DownlinkFrequency * RadioLink.DopplerFactor;
      DownlinkDopplerLabel.Text = offset == 0 ? "0,000" : $"{sign}{offset:n0}";

      sign = RadioLink.DopplerFactor > 0 ? "+" : "";
      offset = RadioLink.UplinkFrequency * RadioLink.DopplerFactor;
      UplinkDopplerLabel.Text = offset == 0 ? "0,000" : $"{sign}{offset:n0}";

      // downlink
      bool bright = ctx.CatControl.Rx?.IsRunning ?? false;
      if (SatnogsDbTransmitter.IsUhfFrequency(RadioLink.DownlinkFrequency))
        DownlinkFrequencyLabel.ForeColor = bright ? Color.Cyan : Color.Teal;
      else if (SatnogsDbTransmitter.IsVhfFrequency(RadioLink.DownlinkFrequency))
        DownlinkFrequencyLabel.ForeColor = bright ? Color.Yellow : Color.Olive;
      else
        DownlinkFrequencyLabel.ForeColor = bright ? Color.White : Color.Gray;
      toolTip1.SetToolTip(DownlinkFrequencyLabel, MakeDownlinkTooltip());

      // uplink
      bright = ctx.CatControl.Tx?.IsRunning ?? false;
      if (!RadioLink.HasUplink)
        UplinkFrequencyLabel.ForeColor = Color.Gray;
      else if (RadioLink.Tx!.IsUhf((long)RadioLink.UplinkFrequency))
        UplinkFrequencyLabel.ForeColor = bright ? Color.Cyan : Color.Teal;
      else if (RadioLink.Tx!.IsVhf((long)RadioLink.UplinkFrequency))
        UplinkFrequencyLabel.ForeColor = bright ? Color.Yellow : Color.Olive;
      else
        UplinkFrequencyLabel.ForeColor = bright ? Color.White : Color.Gray;
      toolTip1.SetToolTip(UplinkFrequencyLabel, MakeUplinkTooltip());

      UpdateTxButton();    
    }

    private string MakeUplinkTooltip()
    {
      if (RadioLink.UplinkFrequency == 0) return "No Uplink Frequency";

      string tooltip = $"Nominal frequency:   {RadioLink.UplinkFrequency:n0} Hz\n";

      if (!RadioLink.IsTerrestrial)
        tooltip += $"Corrected frequency: {RadioLink.CorrectedUplinkFrequency:n0} Hz\n";

      if (RadioLink.IsTransponder)
        tooltip += $"Transponder offset:     {RadioLink.TransponderOffset:n0} Hz\n";

      var transverter = ctx.Settings.Transverter;
      if (transverter.TxCatOffsetEnabled)
      {
        var catBand = transverter.GetCatBand(RadioLink.CorrectedUplinkFrequency);
        if (catBand != null)
          tooltip += $"CAT Transverter IF: {(RadioLink.CorrectedUplinkFrequency - catBand.LoOffset) / 1e6:F6} MHz\n";
      }

      return tooltip + "\nRight-click for options";
    }

    private string MakeDownlinkTooltip()
    {
      string tooltip = $"Nominal frequency:   {RadioLink.DownlinkFrequency:n0} Hz\n";

      if (!RadioLink.IsTerrestrial)
        tooltip += $"Corrected frequency: {RadioLink.CorrectedDownlinkFrequency:n0} Hz\n";

      if (RadioLink.IsTransponder)
        tooltip += $"Transponder offset:     {RadioLink.TransponderOffset:n0} Hz\n";

      if (RadioLink.XitOffset != 0)
        tooltip += $"XIT offset:     {RadioLink.XitOffset:n0} Hz\n";

      var transverter = ctx.Settings.Transverter;
      if (transverter.SdrOffsetEnabled)
      {
        var sdrBand = transverter.GetSdrBand(RadioLink.CorrectedDownlinkFrequency);
        if (sdrBand != null)
          tooltip += $"SDR Transverter IF: {(RadioLink.CorrectedDownlinkFrequency - sdrBand.LoOffset) / 1e6:F6} MHz\n";
      }
      if (transverter.RxCatOffsetEnabled)
      {
        var catBand = transverter.GetCatBand(RadioLink.CorrectedDownlinkFrequency);
        if (catBand != null)
          tooltip += $"CAT Transverter IF: {(RadioLink.CorrectedDownlinkFrequency - catBand.LoOffset) / 1e6:F6} MHz\n";
      }

      return tooltip + "\nClick for manual entry\nRight-click for options";
    }

    public void UiToRadioLink()
    {
      RadioLink.DownlinkMode = (Slicer.Mode)DownlinkModeCombobox.SelectedItem!;
      RadioLink.UplinkMode = (Slicer.Mode)UplinkModeCombobox.SelectedItem!;

      RadioLink.RitEnabled = RitCheckbox.Checked;
      RadioLink.RitOffset = (double)RitSpinner.Value * 1000;

      RadioLink.DownlinkDopplerCorrectionEnabled = DownlinkDopplerCheckbox.Checked;
      RadioLink.DownlinkManualCorrectionEnabled = DownlinkManualCheckbox.Checked;
      RadioLink.DownlinkManualCorrection = (double)DownlinkManualSpinner.Value * 1000;

      RadioLink.UplinkDopplerCorrectionEnabled = UplinkDopplerCheckbox.Checked;
      RadioLink.UplinkManualCorrectionEnabled = UplinkManualCheckbox.Checked;
      RadioLink.UplinkManualCorrection = (double)UplinkManualSpinner.Value * 1000;

      RadioLink.ComputeFrequencies();
    }

    private void UiControl_Changed(object sender, EventArgs e)
    {
      if (Changing) return;
      UiToRadioLink();
      RadioLink.ComputeFrequencies();
      RadioLinkToRadio();
      FrequenciesToUi();

      ctx.QsoEntryPanel?.SetMode();
    }

    //----------------------------------------------------------------------------------------------
    //                                      menu
    //----------------------------------------------------------------------------------------------
    private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
    {
      bool showCorrected = GetClickedControl(sender) == DownlinkFrequencyLabel ?
        ctx.Settings.Cat.RxCat.ShowCorrectedFrequency : ctx.Settings.Cat.TxCat.ShowCorrectedFrequency;

      ShowCorrectedFrequencyMNU.Checked = showCorrected;
      ShowNominalFrequencyMNU.Checked = !showCorrected;
    }

    private void ShowNominalFrequencyMNU_Click(object sender, EventArgs e)
    {
      if (GetClickedControl(sender) == DownlinkFrequencyLabel)
        ctx.Settings.Cat.RxCat.ShowCorrectedFrequency = false;
      else
        ctx.Settings.Cat.TxCat.ShowCorrectedFrequency = false;

      FrequenciesToUi();
    }

    private void ShowCorrectedFrequencyMNU_Click(object sender, EventArgs e)
    {
      if (GetClickedControl(sender) == DownlinkFrequencyLabel)
        ctx.Settings.Cat.RxCat.ShowCorrectedFrequency = true;
      else
        ctx.Settings.Cat.TxCat.ShowCorrectedFrequency = true;
      FrequenciesToUi();
    }

    private Label GetClickedControl(object sender)
    {
      if (sender is ToolStripDropDownItem menuItem)
        sender = ((ToolStripDropDownItem)sender).Owner!;
      return (Label)((ContextMenuStrip)sender).SourceControl!;
    }

    private void DownlinkFrequencyLabel_Click(object sender, EventArgs e)
    {
      FrequencyDialog.Location = Cursor.Position;
      FrequencyDialog.ShowDialog();
      if (FrequencyDialog.EnteredFrequency > 0)
        SetTerrestrialFrequency(FrequencyDialog.EnteredFrequency);
    }

    private void TxBtn_Click(object sender, EventArgs e)
    {
      var ptt = ctx.CatControl.Tx!.Ptt == true;
      SetPtt(!ptt);
    }

    private void UpdateTxButton()
    {
      TxBtn.Visible = ctx.CatControl.Tx?.CanPtt() == true;
      var ptt = ctx.CatControl.Tx?.Ptt == true;
      TxBtn.Text = ptt ? "Stop Transmitting" : "Transmit";

      // the tone applies to an FM uplink only, and both commands need the encoder on/off switch
      CtcssBtn.Visible = RadioLink.HasUplink &&
        RadioLink.TxCust != null && FmModes.Contains(RadioLink.UplinkMode) &&
        ctx.CatControl.Tx?.CanEnableCtcss() == true;
    }




    //----------------------------------------------------------------------------------------------
    //                                      ctcss menu
    //----------------------------------------------------------------------------------------------
    // the tone entries of both commands are created here, the fixed items are in the designer.
    // the arming tone is a one-shot action and has no check mark, the default tone is shown in bold
    private void BuildCtcssMenu()
    {
      var boldFont = new Font(CtcssMenu.Font, FontStyle.Bold);

      foreach (double tone in CtcssTones.All)
      {
        SendToneMnu.DropDownItems.Add(MakeToneMenuItem(tone, CtcssToneMnu_Click));

        var armingItem = MakeToneMenuItem(tone, ArmingToneMnu_Click);
        if (tone == CtcssTones.ARMING_TONE) armingItem.Font = boldFont;
        SendArmingToneMnu.DropDownItems.Add(armingItem);
      }
    }

    private ToolStripMenuItem MakeToneMenuItem(double tone, EventHandler handler)
    {
      var item = new ToolStripMenuItem(CtcssTones.Format(tone)) { Tag = tone };
      item.Click += handler;
      return item;
    }

    private void CtcssBtn_MouseDown(object sender, MouseEventArgs e)
    {
      CtcssMenu.Show(CtcssBtn, new Point(CtcssBtn.Width, CtcssBtn.Height), ToolStripDropDownDirection.BelowLeft);
    }

    // reflect the state of the active transmitter, and what the connected radio can do.
    // unavailable commands are disabled with an explanation, never hidden
    private void CtcssMenu_Opening(object sender, CancelEventArgs e)
    {
      var tx = ctx.CatControl.Tx;
      bool canSetTone = tx?.CanSetCtcssTone() == true;
      bool hasTransmitter = RadioLink.TxCust != null;

      SendToneMnu.Enabled = hasTransmitter && tx?.CanEnableCtcss() == true;
      SendToneMnu.ToolTipText = tx?.CanEnableCtcss() == true ? string.Empty :
        "The radio does not support switching the CTCSS encoder over CAT";

      SendArmingToneMnu.Enabled = tx?.CanSendArmingTone() == true;
      SendArmingToneMnu.ToolTipText = SendArmingToneMnu.Enabled ? string.Empty :
        "The radio cannot select the tone or key the transmitter over CAT, send the arming tone from the front panel";

      CtcssEnabledMnu.Checked = hasTransmitter && RadioLink.CtcssEnabled;

      foreach (ToolStripItem item in SendToneMnu.DropDownItems)
      {
        if (item.Tag is not double tone) continue;
        item.Enabled = canSetTone;
        item.ToolTipText = canSetTone ? string.Empty :
          "The radio has no CAT command for the tone frequency, select the tone on the front panel";
        ((ToolStripMenuItem)item).Checked = hasTransmitter && tone == RadioLink.CtcssTone;
      }
    }

    // turns the encoder on and off without changing the tone selected for this transmitter
    private void CtcssEnabledMnu_Click(object sender, EventArgs e)
    {
      if (RadioLink.TxCust == null) return;

      RadioLink.CtcssEnabled = !RadioLink.CtcssEnabled;
      ctx.CatControl.Tx?.SetCtcssTone(RadioLink.CtcssTone, RadioLink.CtcssEnabled);
    }

    private void CtcssToneMnu_Click(object? sender, EventArgs e)
    {
      if (RadioLink.TxCust == null) return;

      RadioLink.CtcssTone = (double)((ToolStripMenuItem)sender!).Tag!;
      ctx.CatControl.Tx?.SetCtcssTone(RadioLink.CtcssTone, RadioLink.CtcssEnabled);
    }

    private void ArmingToneMnu_Click(object? sender, EventArgs e)
    {
      ctx.CatControl.Tx?.SendArmingTone((double)((ToolStripMenuItem)sender!).Tag!);
    }
  }
}

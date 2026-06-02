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

    // Transverter band the SDR is currently centered on (null when transverter is disabled
    // or no SDR band matches the active RF). Updated by SetSlicerFrequency on band changes.
    private TransverterBand? ActiveSdrBand;

    public FrequencyWidget()
    {
      InitializeComponent();
      Changing = true;
      DownlinkModeCombobox.DataSource = Enum.GetValues(typeof(Slicer.Mode));
      UplinkModeCombobox.DataSource = Enum.GetValues(typeof(Slicer.Mode));
      DownlinkModeCombobox.SelectedIndex = 0;
      UplinkModeCombobox.SelectedIndex = 0;
      Changing = false;
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
    }

    internal void SetTransponderOffset(SatnogsDbTransmitter transponder, double offset)
    {
      // set the offset first
      var transponderCust = ctx.Settings.Satellites.TransmitterCustomizations.GetOrCreate(transponder.uuid);
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
        RadioLink.TxCust = ctx.Settings.Satellites.TransmitterCustomizations.GetOrCreate(RadioLink.Tx.uuid);
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

      // freq in slicer
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
      double rfFrequency = RadioLink.CorrectedDownlinkFrequency!;
      var transverter = ctx.Settings.Transverter;

      // Resolve transverter band (null if disabled or RF doesn't match any band)
      TransverterBand? prevBand = ActiveSdrBand;
      TransverterBand? band = transverter.SdrOffsetEnabled ? transverter.GetSdrBand(rfFrequency) : null;
      ActiveSdrBand = band;
      bool bandChanged = band != prevBand;

      // IF frequency the SDR sees (= RF when no transverter offset applies)
      long loOffset = band?.LoOffset ?? 0;
      double ifFrequency = rfFrequency - loOffset;

      // Determine the band-limit constraints for the SDR center
      double bandIfLow, bandIfHigh;
      if (band != null)
      {
        bandIfLow = band.RfLow - band.LoOffset;
        bandIfHigh = band.RfHigh - band.LoOffset;
      }
      else if (SatnogsDbTransmitter.IsVhfFrequency(rfFrequency))
      {
        bandIfLow = SdrConst.VHF_CENTER_FREQUENCY - SdrConst.MAX_BANDWIDTH / 2;
        bandIfHigh = SdrConst.VHF_CENTER_FREQUENCY + SdrConst.MAX_BANDWIDTH / 2;
      }
      else if (SatnogsDbTransmitter.IsUhfFrequency(rfFrequency))
      {
        bandIfLow = SdrConst.UHF_CENTER_FREQUENCY - SdrConst.MAX_BANDWIDTH / 2;
        bandIfHigh = SdrConst.UHF_CENTER_FREQUENCY + SdrConst.MAX_BANDWIDTH / 2;
      }
      else
      {
        // unknown / non-ham band: no constraint, let the SDR tune directly
        bandIfLow = ifFrequency - bandwidth;
        bandIfHigh = ifFrequency + bandwidth;
      }

      // Transverter mode: on first activation or band crossing, retune SDR to the IF center
      // of the new band so the entire IF band is in view (when bandwidth allows).
      if (band != null && bandChanged)
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

      // Signal outside current SDR passband: reposition SDR center (constrained by band)
      if (ifFrequency < low || ifFrequency > high)
        if (ctx.Sdr.IsFrequencySupported(ifFrequency))
        {
          BringToPassband(ifFrequency, bandIfLow, bandIfHigh);
          ctx.WaterfallPanel?.SetCenterFrequency(GetSdrRfCenter());
        }
        else
          return;

      low = ctx.Sdr.Frequency - bandwidth / 2;
      high = ctx.Sdr.Frequency + bandwidth / 2;

      if (ifFrequency >= low && ifFrequency <= high)
        if (ctx.Slicer?.Enabled == true)
          ctx.Slicer.SetOffset(ifFrequency - ctx.Sdr.Frequency);
    }

    private bool BringToPassband(double frequency, double bandLow, double bandHigh)
    {
      double sdrWing = ctx.Sdr!.Info.MaxBandwidth / 2;

      // constrain the new SDR center so the passband stays within [bandLow, bandHigh]
      if (bandHigh - bandLow >= 2 * sdrWing)
      {
        double minCenter = bandLow + sdrWing;
        double maxCenter = bandHigh - sdrWing;
        frequency = Math.Max(minCenter, Math.Min(maxCenter, frequency));
      }

      if (ctx.Sdr.IsFrequencySupported(frequency))
      {
        ctx.Sdr.Frequency = frequency;
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
      return ctx.Sdr.Info.Frequency + (ActiveSdrBand?.LoOffset ?? 0);
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
        DownlinkLabel.Font = new(DownlinkLabel.Font, FontStyle.Bold);

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
        DownlinkLabel.Font = new(DownlinkLabel.Font, FontStyle.Regular);

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
    }
  }
}

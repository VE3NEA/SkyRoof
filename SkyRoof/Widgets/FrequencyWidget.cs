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
      ctx.CatControl.ApplySettings();
      ctx.RotatorControl.SetSatellite(ctx.SatelliteSelector.SelectedSatellite);
      RadioLinkToRadio();
      UpdateTxButton();
    }

    public void SetTerrestrialFrequency(double frequency)
    {
      SettingsToRadioLink(true, frequency);
      RadioLinkToUi();
      ctx.CatControl.ApplySettings();
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

      // freq in external radio
      ctx.CatControl.Rx?.SetRxFrequency((long)Math.Truncate(RadioLink.CorrectedDownlinkFrequency));
      if (RadioLink.CorrectedUplinkFrequency != 0)
        ctx.CatControl.Tx?.SetTxFrequency((long)Math.Truncate(RadioLink.CorrectedUplinkFrequency));
    }

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

      if (targetFrequency >= low && targetFrequency <= high)
        if (ctx.Slicer?.Enabled == true)
          ctx.Slicer.SetOffset(targetFrequency - ctx.Sdr.Frequency);
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

      return tooltip + "\nRight-click for options";
    }

    private string MakeDownlinkTooltip()
    {
      string tooltip = $"Nominal frequency:   {RadioLink.DownlinkFrequency:n0} Hz\n";

      if (!RadioLink.IsTerrestrial)
        tooltip += $"Corrected frequency: {RadioLink.CorrectedDownlinkFrequency:n0} Hz\n";

      if (RadioLink.IsTransponder)
        tooltip += $"Transponder offset:     {RadioLink.TransponderOffset:n0} Hz\n";

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
      ctx.CatControl.Tx!.SetPtt(!ptt);
      UpdateTxButton();
    }

    private void UpdateTxButton()
    {
      TxBtn.Visible = ctx.CatControl.Tx?.CanPtt() == true;
      var ptt = ctx.CatControl.Tx?.Ptt == true;
      TxBtn.Text = ptt ? "Stop Transmitting" : "Transmit";
    }
  }
}

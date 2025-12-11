using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Serilog;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class QsoEntryPanel : DockContent
  {
    private const string States = "AL,AK,AZ,AR,CA,CO,CT,DE,FL,GA,HI,ID,IL,IN,IA,KS,KY,LA,ME,MD,MA,MI,MN,MS,MO,MT,NE,NV,NH,NJ,NM,NY,NC,ND,OH,OK,OR,PA,RI,SC,SD,TN,TX,UT,VT,VA,WA,WV,WI,WY";
    private Context ctx;
    private bool Changing;

    public Slicer.Mode? LastSetMode = null;

    public QsoEntryPanel()
    {
      InitializeComponent();
    }

    public QsoEntryPanel(Context ctx)
    {
      this.ctx = ctx;
      Log.Information("Creating QsoEntryPanel");
      InitializeComponent();

      ApplySettings();

      ctx.QsoEntryPanel = this;
      ctx.MainForm.QsoEntryMNU.Checked = true;

      Changing = true;
      BandComboBox.DataSource = new string[] { "2m", "70cm", "23cm", "13cm" };
      ModeComboBox.DataSource = new string[] { "CW", "SSB", "FM", "FT4" };
      StateComboBox.DataSource = States.Split(',');
      SatComboBox.DataSource = new SatelliteNames().Lotw.Values.ToArray();

      BandComboBox.SelectedIndex = -1;
      ModeComboBox.SelectedIndex = -1;
      StateComboBox.SelectedIndex = -1;
      SatComboBox.SelectedIndex = -1;

      Changing = false;

      ClearFields();
    }

    private void ClearFields()
    {
      ClearFrames();

      SetUtc();

      Changing = true;

      SetSatellite();
      SetBand();
      SetMode();

      CallEdit.Text = GridEdit.Text = NameEdit.Text = NotesEdit.Text = string.Empty;
      CallEdit.BackColor = SystemColors.Window;
      CallEdit.ForeColor = SystemColors.WindowText;
      StateComboBox.SelectedIndex = -1;

      Changing = false;
    }

    private void ClearFrames()
    {
      UtcFrame.BackColor = Color.LightSkyBlue;
      BandFrame.BackColor = Color.LightSkyBlue;
      ModeFrame.BackColor = Color.LightSkyBlue;
      SatFrame.BackColor = Color.LightSkyBlue;
      CallFrame.BackColor = Color.LightSkyBlue;
      GridFrame.BackColor = Color.LightSkyBlue;
      StateFrame.BackColor = Color.LightSkyBlue;
      SentFrame.BackColor = Color.LightSkyBlue;
      RecvFrame.BackColor = Color.LightSkyBlue;
      NameFrame.BackColor = Color.LightSkyBlue;
      NotesFrame.BackColor = Color.LightSkyBlue;
    }

    private void QsoEntryPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing QsoEntryPanel");
      ctx.QsoEntryPanel = null;
      ctx.MainForm.QsoEntryMNU.Checked = false;
    }

    internal void ApplySettings()
    {
      ShowHideFields();
    }

    private void ShowHideFields()
    {
      var visibleFields = ctx.Settings.QsoEntry.Fields;

      foreach (var control in flowLayoutPanel1.Controls)
      {
        if (control is Panel panel)
          panel.Visible = panel.Name == "ButtonsPanel" || visibleFields.HasFlag((QsoFields)(1 << panel.TabIndex));
      }
    }

    internal void SetUtc()
    {
      if (UtcFrame.BackColor == Color.LightSkyBlue)
      {
        Changing = true;
        UtcPicker.Value = DateTime.UtcNow;
        Changing = false;
      }
    }

    internal void SetSatellite()
    {
      if (ctx.SatelliteSelector.SelectedSatellite != null)
      {
        string? sat = ctx.SatelliteSelector.SelectedSatellite?.LotwName;
        if (string.IsNullOrEmpty(sat)) SatComboBox.SelectedIndex = -1;
        else SatComboBox.SelectedIndex = SatComboBox.FindStringExact(sat);
      }
      else
        SatComboBox.SelectedIndex = -1;

      if (SatComboBox.SelectedIndex == -1) SatComboBox.Text = string.Empty;

      SatFrame.BackColor = Color.LightSkyBlue;

    }

    internal void SetBand()
    {
      string bandName = ctx.FrequencyControl.GetBandName(true);
      BandComboBox.Text = bandName;

      if (string.IsNullOrEmpty(bandName)) BandComboBox.SelectedIndex = -1;

      BandFrame.BackColor = Color.LightSkyBlue;
    }

    internal void SetMode()
    {
      Slicer.Mode? mode = ctx.FrequencyControl.RadioLink.HasUplink ? ctx.FrequencyControl.RadioLink.UplinkMode : null;
      if (mode == LastSetMode) return;
      LastSetMode = mode;

      string newMode;

      if (mode == Slicer.Mode.USB || mode == Slicer.Mode.LSB)
        newMode = "SSB";
      else if (mode == Slicer.Mode.CW)
        newMode = "CW";
      else if (mode == Slicer.Mode.FM || mode == Slicer.Mode.FM_D)
        newMode = "FM";
      else if (mode == Slicer.Mode.USB_D || mode == Slicer.Mode.LSB_D)
        newMode = "FT4";
      else
        newMode = string.Empty;


      ModeComboBox.Text = newMode;
      if (string.IsNullOrEmpty(newMode)) ModeComboBox.SelectedIndex = -1;

      ModeFrame.BackColor = Color.LightSkyBlue;
      SetReport();
    }

    private void SetReport()
    {
      string defaultReport;

      if (ModeComboBox.Text == "CW") defaultReport = RecvEdit.Text = "599";
      else if (ModeComboBox.Text == "SSB") defaultReport = RecvEdit.Text = "59";
      else if (ModeComboBox.Text == "FM") defaultReport = RecvEdit.Text = "59";
      else defaultReport = RecvEdit.Text = string.Empty;

      SentEdit.Text = RecvEdit.Text = defaultReport;
      SentFrame.BackColor = RecvFrame.BackColor = Color.LightSkyBlue;
    }

    private void ClearBtn_Click(object sender, EventArgs e)
    {
      ClearFields();
    }

    private void Field_Changed(object sender, EventArgs e)
    {
      if (sender == ModeComboBox) SetReport();
      if (Changing) return;

      // blue frame indicates that the value was entered manually
      var control = (Control)sender;
      control.Parent!.BackColor = control.Text == "" ? Color.LightSkyBlue : Color.Blue;

      var qso = FieldsToQsoInfo(true);

      // call changed, look up grid and state
      if (sender == CallEdit)
      {
        qso = ctx.LoggerInterface.Augment(qso);
        QsoInfoToFields(qso);
      }

      // any field changed, update status
      qso = ctx.LoggerInterface.GetStatus(qso);
      QsoInfoToStatus(qso);
    }

    private void LogBtn_Click(object sender, EventArgs e)
    {
      var qso = FieldsToQsoInfo();

      if (!Utils.CallsignRegex.IsMatch(qso.Call)) { ErrBox("Invalid callsign"); return; }
      if (qso.Band == string.Empty) { ErrBox("Invalid band"); return; }
      if (qso.Mode == string.Empty) { ErrBox("Invalid mode"); return; }

      if (!Utils.GridSquare4Regex.IsMatch(qso.Grid) && !Ask("Invalid or empty grid square")) return; 
      if (qso.Sat == string.Empty && !Ask("Satellite not specified")) return;
      if (qso.Sent == string.Empty && !Ask("Sent report not specified")) return;
      if (qso.Recv == string.Empty && !Ask("Received report not specified")) return;

      ctx.LoggerInterface.SaveQso(qso);
      ClearFields();
    }


    private QsoInfo FieldsToQsoInfo(bool onlyEdited = false)
    {
      QsoInfo info = new();
      info.StationCallsign = ctx.Settings.User.Call;
      info.MyGridSquare = ctx.Settings.User.Square;

      if (onlyEdited)
      {
        if (UtcFrame.BackColor == Color.Blue) info.Utc = UtcPicker.Value;
        if (BandFrame.BackColor == Color.Blue) info.Band = BandComboBox.Text.ToUpper();
        if (ModeFrame.BackColor == Color.Blue) info.Mode = ModeComboBox.Text.ToUpper();
        if (SatFrame.BackColor == Color.Blue) info.Sat = SatComboBox.Text.Trim();
        if (CallFrame.BackColor == Color.Blue) info.Call = CallEdit.Text.ToUpper();
        if (GridFrame.BackColor == Color.Blue) info.Grid = GridEdit.Text.ToUpper();
        if (StateFrame.BackColor == Color.Blue) info.State = StateComboBox.Text.ToUpper();
        if (SentFrame.BackColor == Color.Blue) info.Sent = SentEdit.Text;
        if (RecvFrame.BackColor == Color.Blue) info.Recv = RecvEdit.Text;
        if (NameFrame.BackColor == Color.Blue) info.Name = NameEdit.Text;
        if (NotesFrame.BackColor == Color.Blue) info.Notes = NotesEdit.Text;
      }
      else
      {
        info.Utc = UtcPicker.Value;
        info.Band = BandComboBox.Text.ToLower();
        info.Mode = ModeComboBox.Text.ToUpper();
        info.Sat = SatComboBox.Text.Trim();
        info.Call = CallEdit.Text.ToUpper();
        info.Grid = GridEdit.Text.ToUpper();
        info.State = StateComboBox.Text.ToUpper();
        info.Sent = SentEdit.Text;
        info.Recv = RecvEdit.Text;
        info.Name = NameEdit.Text;
        info.Notes = NotesEdit.Text;
      }

      return info;
    }

    public void QsoInfoToFields(QsoInfo qso)
    {
      Changing = true;

      if (GridFrame.BackColor == Color.LightSkyBlue) GridEdit.Text = qso.Grid;
      if (NameFrame.BackColor == Color.LightSkyBlue) NameEdit.Text = qso.Name;

      if (StateFrame.BackColor == Color.LightSkyBlue)
      {
        if (qso.State == "") StateComboBox.SelectedIndex = -1;
        StateComboBox.SelectedItem = qso.State;
      }

      Changing = false;
    }

    public void QsoInfoToStatus(QsoInfo qso)
    {
      CallEdit.BackColor = ColorTranslator.FromHtml(qso.BackColor);
      CallEdit.ForeColor = ColorTranslator.FromHtml(qso.ForeColor);
      toolTip1.SetToolTip(CallEdit, qso.StatusString);
    }

    private void ErrBox(string message)
    {
      MessageBox.Show(message, "Invalid Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private bool Ask(string question)
    {
      return MessageBox.Show(question + ". Save anyway?", "Invalid Data",
        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private void UtcPicker_MouseDown(object sender, MouseEventArgs e)
    {
      UtcFrame.BackColor = UtcFrame.BackColor == Color.LightSkyBlue ? Color.Blue : Color.LightSkyBlue;
    }
  }
}


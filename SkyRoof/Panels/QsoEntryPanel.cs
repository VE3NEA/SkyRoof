using Serilog;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class QsoEntryPanel : DockContent
  {
    private const string States = "AL,AK,AZ,AR,CA,CO,CT,DE,FL,GA,HI,ID,IL,IN,IA,KS,KY,LA,ME,MD," +
      "MA,MI,MN,MS,MO,MT,NE,NV,NH,NJ,NM,NY,NC,ND,OH,OK,OR,PA,RI,SC,SD,TN,TX,UT,VT,VA,WA,WV,WI,WY";
    private Context ctx;
    private bool Changing;

    public Slicer.Mode? LastSetMode = null;

    public bool ShouldClose = false;

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
      ModeComboBox.DataSource = new string[] { "CW", "SSB", "FM", "MFSK" };
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
      SetReport();

      CallEdit.Text = GridEdit.Text = NameEdit.Text = NotesEdit.Text = string.Empty;
      CallEdit.BackColor = SystemColors.Window;
      CallEdit.ForeColor = SystemColors.WindowText;
      StateComboBox.SelectedIndex = -1;

      Changing = false;
    }

    private void ClearFrames()
    {
      UtcFrame.BackColor = Theme.QsoCard;
      BandFrame.BackColor = Theme.QsoCard;
      ModeFrame.BackColor = Theme.QsoCard;
      SatFrame.BackColor = Theme.QsoCard;
      CallFrame.BackColor = Theme.QsoCard;
      GridFrame.BackColor = Theme.QsoCard;
      StateFrame.BackColor = Theme.QsoCard;
      SentFrame.BackColor = Theme.QsoCard;
      RecvFrame.BackColor = Theme.QsoCard;
      NameFrame.BackColor = Theme.QsoCard;
      NotesFrame.BackColor = Theme.QsoCard;
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
      if (UtcFrame.BackColor == Theme.QsoCard)
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

      SatFrame.BackColor = Theme.QsoCard;

    }

    internal void SetBand()
    {
      string bandName = ctx.FrequencyControl.GetBandName(true);
      BandComboBox.Text = bandName;

      if (string.IsNullOrEmpty(bandName)) BandComboBox.SelectedIndex = -1;

      BandFrame.BackColor = Theme.QsoCard;
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
        newMode = "MFSK";
      else
        newMode = string.Empty;


      ModeComboBox.Text = newMode;
      if (string.IsNullOrEmpty(newMode)) ModeComboBox.SelectedIndex = -1;

      ModeFrame.BackColor = Theme.QsoCard;
      SetReport();
    }

    private void SetReport()
    {
      string defaultReport;

      if (ModeComboBox.Text == "CW") defaultReport = RecvEdit.Text = "599";
      else if (ModeComboBox.Text == "SSB") defaultReport = RecvEdit.Text = "59";
      else if (ModeComboBox.Text == "FM") defaultReport = RecvEdit.Text = "59";
      else defaultReport = string.Empty;

      SentEdit.Text = RecvEdit.Text = defaultReport;
      SentFrame.BackColor = RecvFrame.BackColor = Theme.QsoCard;
    }

    private void ClearBtn_Click(object sender, EventArgs e)
    {
      ClearFields();
    }

    private void Field_Changed(object sender, EventArgs e)
    {
      if (sender == ModeComboBox) SetReport(); // report is mode-specific
      if (Changing) return;

      // dark blue frame indicates that the value was entered manually

      var control = (Control)sender;
      control.Parent!.BackColor = control.Text == "" ? Theme.QsoCard : Theme.QsoFieldEdited;

      var qso = FieldsToQsoInfo(true);

      // call changed, look up grid and state
      if (sender == CallEdit)
      {
        qso = ctx.LoggerInterface.Augment(qso);
        AugmentedInfoToFields(qso);
      }

      // any field changed, update status
      qso = ctx.LoggerInterface.GetStatus(qso);
      QsoInfoToStatus(qso);
    }

    private void UtcPicker_KeyDown(object sender, KeyEventArgs e)
    {
      UtcFrame.BackColor = Theme.QsoFieldEdited;
    }

    private void Utclabel_MouseClick(object sender, MouseEventArgs e)
    {
      UtcFrame.BackColor = UtcFrame.BackColor == Theme.QsoCard ? Theme.QsoFieldEdited : Theme.QsoCard;
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

      if (qso.TxFreq == 0) qso.TxFreq = FrequencyOfBand(qso.Band);

      ctx.Ft4ConsolePanel?.WsjtxUdpSender?.SendLogQsoMessage(qso);

      ctx.LoggerInterface.SaveQso(qso);
      ClearFields();
      if (DockState == DockState.Float && ShouldClose) Close();
    }

    private ulong FrequencyOfBand(string band)
    {
      switch (band)
      {
        case "2m": return 145_800_000;
        case "70cm": return 435_000_000;
        default: return 1_240_000_000;
      }
    }

    private QsoInfo FieldsToQsoInfo(bool onlyEdited = false)
    {
      QsoInfo info = new();
      info.StationCallsign = ctx.Settings.User.Call;
      info.MyGridSquare = ctx.Settings.User.Square;

      if (onlyEdited)
      {
        if (UtcFrame.BackColor == Theme.QsoFieldEdited) info.Utc = UtcPicker.Value;
        if (BandFrame.BackColor == Theme.QsoFieldEdited) info.Band = BandComboBox.Text.ToUpper();
        if (ModeFrame.BackColor == Theme.QsoFieldEdited) info.Mode = ModeComboBox.Text.ToUpper();
        if (SatFrame.BackColor == Theme.QsoFieldEdited) info.Sat = SatComboBox.Text.Trim();
        if (CallFrame.BackColor == Theme.QsoFieldEdited) info.Call = CallEdit.Text.ToUpper();
        if (GridFrame.BackColor == Theme.QsoFieldEdited) info.Grid = GridEdit.Text.ToUpper();
        if (StateFrame.BackColor == Theme.QsoFieldEdited) info.State = StateComboBox.Text.ToUpper();
        if (SentFrame.BackColor == Theme.QsoFieldEdited) info.Sent = SentEdit.Text;
        if (RecvFrame.BackColor == Theme.QsoFieldEdited) info.Recv = RecvEdit.Text;
        if (NameFrame.BackColor == Theme.QsoFieldEdited) info.Name = NameEdit.Text;
        if (NotesFrame.BackColor == Theme.QsoFieldEdited) info.Notes = NotesEdit.Text;
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

    public void AugmentedInfoToFields(QsoInfo qso)
    {
      Changing = true;

      if (GridFrame.BackColor == Theme.QsoCard) GridEdit.Text = qso.Grid;
      if (NameFrame.BackColor == Theme.QsoCard) NameEdit.Text = qso.Name;

      if (StateFrame.BackColor == Theme.QsoCard)
      {
        if (qso.State == "") StateComboBox.SelectedIndex = -1;
        StateComboBox.SelectedItem = qso.State;
      }

      Changing = false;
    }

    public void QsoInfoToStatus(QsoInfo qso)
    {
      // the logger returns white on black when it has nothing to say about the call, i.e. when the
      // field is empty. That is "unstyled", not a status color, so let the theme paint it instead
      bool unstyled = string.Equals(qso.BackColor, "#FFFFFF", StringComparison.OrdinalIgnoreCase);

      CallEdit.BackColor = unstyled ? SystemColors.Window : ColorTranslator.FromHtml(qso.BackColor);
      CallEdit.ForeColor = unstyled ? SystemColors.WindowText : ColorTranslator.FromHtml(qso.ForeColor);
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

    internal void SetQsoInfo(QsoInfo qso)
    {
      Changing = true;

      UtcPicker.Value = qso.Utc;
      UtcFrame.BackColor = Theme.QsoFieldEdited; // i.e. modified

      BandComboBox.Text = qso.Band;
      ModeComboBox.Text = qso.Mode;
      SatComboBox.Text = qso.Sat;
      CallEdit.Text = qso.Call;
      GridEdit.Text = qso.Grid;
      StateComboBox.Text = qso.State;
      SentEdit.Text = qso.Sent;
      RecvEdit.Text = qso.Recv;
      NameEdit.Text = qso.Name;
      NotesEdit.Text = qso.Notes;

      Changing = false;

      qso = ctx.LoggerInterface.Augment(qso);
      AugmentedInfoToFields(qso);
    }
  }
}


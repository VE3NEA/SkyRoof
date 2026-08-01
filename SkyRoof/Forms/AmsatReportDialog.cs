namespace SkyRoof
{
  public partial class AmsatReportDialog : Form, IMessageFilter
  {
    private const int AutoCloseMs = 30000;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_NCLBUTTONDOWN = 0x00A1;

    private Context ctx;
    private SatnogsDbSatellite Satellite;
    // when set, the AMSAT entry whose name contains this token is preselected instead of the first one
    private string? PreferredEntryToken;
    private System.Windows.Forms.Timer? AutoCloseTimer;

    public AmsatReportDialog()
    {
      InitializeComponent();
    }

    public static void SendReport(Context ctx, SatnogsDbSatellite satellite, string? preferredEntryToken = null)
    {
      var dialog = new AmsatReportDialog();
      dialog.ctx = ctx;
      dialog.Satellite = satellite;
      dialog.PreferredEntryToken = preferredEntryToken;
      dialog.ShowDialog();
    }

    private void AmsatReportDialog_Load(object sender, EventArgs e)
    {
      comboBox1.Items.AddRange(Satellite.AmsatEntries.ToArray());
      // Status-page entries are shaped "Name_[Mode]" (see AmsatStatusDownloader.tupleRx), so a satellite
      // carrying several of them — ISS_[SSTV] beside ISS_[FM Voice] — must not default to the first when the
      // caller knows which mode was actually heard; the token matches the bracketed mode. No match falls
      // back to the first entry: the single-entry case, and every caller that names no token.
      int preferred = PreferredEntryToken == null ? -1 : Satellite.AmsatEntries
        .FindIndex(entry => entry.Contains(PreferredEntryToken, StringComparison.OrdinalIgnoreCase));
      comboBox1.SelectedIndex = Math.Max(0, preferred);

      if (Satellite.norad_cat_id == 25544)
        comboBox2.Items.AddRange(["Heard", "Telemetry Only", "Not Heard", "Crew Active"]);
      else
        comboBox2.Items.AddRange(["Heard", "Telemetry Only", "Not Heard"]);

      comboBox2.SelectedIndex = 0;

      StartAutoClose();
    }

    private void okBtn_Click(object sender, EventArgs e)
    {
      string? errorMessage = ctx.AmsatStatusLoader.SendAmsatStatus(comboBox1.Text, comboBox2.Text);
      if (errorMessage != null)
        MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

      Close();
    }




    // ----------------------------------------------------------------------------------------------------
    //                                             auto close
    // ----------------------------------------------------------------------------------------------------
    // an unattended dialog dismisses itself, but the first mouse click anywhere means the operator is here,
    // so the timer is cancelled for good and the dialog then waits for OK or Cancel as usual
    private void StartAutoClose()
    {
      AutoCloseTimer = new System.Windows.Forms.Timer { Interval = AutoCloseMs };
      AutoCloseTimer.Tick += AutoCloseTimer_Tick;
      AutoCloseTimer.Start();

      Application.AddMessageFilter(this);
    }

    private void CancelAutoClose()
    {
      if (AutoCloseTimer == null) return;

      Application.RemoveMessageFilter(this);
      AutoCloseTimer.Stop();
      AutoCloseTimer.Dispose();
      AutoCloseTimer = null;
    }

    private void AutoCloseTimer_Tick(object? sender, EventArgs e)
    {
      CancelAutoClose();
      DialogResult = DialogResult.Cancel;
      Close();
    }

    // clicks go to the combo boxes and to the dropdown windows, not to the form, so they are seen here
    public bool PreFilterMessage(ref Message m)
    {
      if (m.Msg is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_NCLBUTTONDOWN)
        CancelAutoClose();

      return false;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
      CancelAutoClose();
      base.OnFormClosed(e);
    }
  }
}

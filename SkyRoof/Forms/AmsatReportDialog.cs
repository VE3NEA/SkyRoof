namespace SkyRoof
{
  public partial class AmsatReportDialog : Form
  {
    private Context ctx;
    private SatnogsDbSatellite Satellite;
    // when set, the AMSAT entry whose name contains this token is preselected instead of the first one
    private string? PreferredEntryToken;

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
    }

    private void okBtn_Click(object sender, EventArgs e)
    {
      string? errorMessage = ctx.AmsatStatusLoader.SendAmsatStatus(comboBox1.Text, comboBox2.Text);
      if (errorMessage != null) 
        MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
 
      Close();
    }
  }
}

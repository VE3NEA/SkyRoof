using SkyRoof;
using VE3NEA;

namespace SkyRoof
{

  public partial class RotatorWidget : UserControl
  {
    public Context ctx;
    private RotatorControlEngine? engine;
    private AzElEntryDialog Dialog = new();
    private OptimizedRotationPath? Path;
    private Bearing? SatBearing;
    public Bearing? AntBearing { get => engine?.LastReadBearing; }

    // PathOptimizerForm instance is created once and reused
    private PathOptimizerForm dialog = new();

    public RotatorWidget()
    {
      InitializeComponent();

      // Create the PathOptimizerForm on startup, but do not show it yet
      dialog.FormClosing += (s, e) => { if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true; dialog.Hide(); };
    }

    //----------------------------------------------------------------------------------------------
    //                               public interface
    //----------------------------------------------------------------------------------------------
    public void ApplySettings(bool restoreTracking = false)
    {
      bool track = restoreTracking && TrackCheckbox.Checked;

      if (engine != null) StopRotation();
      engine?.Dispose();
      engine = null;

      if (ctx.Settings.Rotator.Enabled)
      {
        engine = new RotatorControlEngine(ctx.Settings.Rotator);
        engine.StatusChanged += Engine_StatusChanged;
        engine.BearingChanged += Engine_BearingChanged;
      }

      ResetUi();

      SetSatellite(ctx.SatelliteSelector.SelectedSatellite);

      TrackCheckbox.Checked = track;
      Advance();

      ctx.MainForm.ShowRotatorStatus();
    }

    public void SetSatellite(SatnogsDbSatellite? sat)
    {
      if (sat == Path?.Satellite) return;

      engine?.StopRotation();

      if (sat == null)
        Path = null;
      else
        SetPass(ctx.HamPasses.GetNextPass(sat));
    }

    public void SetPass(SatellitePass? pass)
    {
      Path = pass == null ? null : Path = new(pass, ctx.Settings.Rotator, AntBearing);

      ResetUi();
      Advance();
      // show black LED if no satellite
      ctx.MainForm.ShowRotatorStatus();

      UpdatePathOptimizerForm();
    }

    public void Park()
    {
      if (engine == null || !ctx.Settings.Rotator.Enabled) return;

      var sett = ctx.Settings.Rotator;
      var bearing = new Bearing(sett.ParkAzimuth * Trig.RinD, sett.ParkElevation * Trig.RinD);
      RotateTo(bearing);
    }

    internal void Advance()
    {
      if (Path == null) return;

      SatBearing = Path.GetSatelliteBearing()?.Normalize();
      if (SatBearing == null) StopRotation();

      BearingToUi();
      ctx.Announcer.AnnouncePosition(SatBearing);

      if (SatBearing != null && engine != null && TrackCheckbox.Checked)
      {
        var maxError = 0.5 * ctx.Settings.Rotator.StepSize * Geo.RinD;
        var bearing = Sanitize(SatBearing);
        if (AntBearing == null || AngleBetween(bearing, AntBearing) >= maxError)
          RotateTo(Path.GetNextAntennaBearing());
      }
    }

    public void Retry()
    {
      engine?.Retry();
    }

    public bool IsRunning()
    {
      return engine != null && engine.IsRunning;
    }

    public void RotateTo(Bearing? bearing)
    {
      if (engine == null || bearing == null) return;

      var sanitizedBearing = Sanitize(bearing);
      engine.RotateTo(sanitizedBearing);
    }

    public void StopRotation()
    {
      TrackCheckbox.Checked = false;
      engine?.StopRotation();
    }

    public void ToggleTracking()
    {
      if (!TrackCheckbox.Enabled) return;
      TrackCheckbox.Checked = !TrackCheckbox.Checked;
      TrackCheckbox_CheckedChanged(StopBtn, EventArgs.Empty);
    }

    public string? GetStatusString()
    {
      if (!ctx.Settings.Rotator.Enabled) return "Rotator control disabled";
      else if (!IsRunning()) return "No connection";
      else if (!TrackCheckbox.Checked) return "Connected, tracking disabled";
      else return "Connected and tracking";
    }

    //----------------------------------------------------------------------------------------------
    //                                        UI
    //----------------------------------------------------------------------------------------------
    private void AzEl_Click(object sender, EventArgs e)
    {
      if (ModifierKeys == (Keys.Control | Keys.Shift))  ShowRotatorDebugInfo();
      else Dialog.Open(ctx);
    }

    private void TrackCheckbox_CheckedChanged(object sender, EventArgs e)
    {

      if (TrackCheckbox.Checked)
      {
        if (Path != null)
        {
          // re-compute path
          var pass = ctx.HamPasses.GetNextPass(Path!.Satellite);
          var sett = ctx.Settings.Rotator;
          Path = new(pass, sett, AntBearing);
          UpdatePathOptimizerForm();
          RotateTo(Path?.GetNextAntennaBearing());
        }
      }
      else
        StopRotation();

      // update color
      BearingToUi();

      ctx.MainForm.ShowRotatorStatus();
    }

    private void StopBtn_Click(object sender, EventArgs e)
    {
      StopRotation();
    }

    private void ResetUi()
    {
      SatelliteAzimuthLabel.ForeColor = Color.Gray;
      SatelliteElevationLabel.ForeColor = Color.Gray;

      SatelliteAzimuthLabel.Text = "0°";
      SatelliteElevationLabel.Text = "0°";
      AntennaAzimuthLabel.Text = "---";
      AntennaElevationLabel.Text = "---";

      TrackCheckbox.Checked = false;
      TrackCheckbox.Enabled = ctx.Settings.Rotator.Enabled && Path != null;
    }

    private void BearingToUi()
    {
      var realSatBearing = Path?.GetRealSatelliteBearing();
      if (realSatBearing == null || SatBearing == null) { ResetUi(); return; }

      Color satColor = TrackCheckbox.Checked ? Color.Aqua : Color.Teal;

      bool trackError = TrackCheckbox.Checked && (!IsRunning() ||
        AntBearing == null ||
        AngleBetween(SatBearing, AntBearing!) > 1.5 * ctx.Settings.Rotator.StepSize * Geo.RinD);

      Color antColor = trackError ? Color.LightCoral : Color.Transparent;

      SatelliteAzimuthLabel.ForeColor = satColor;
      SatelliteElevationLabel.ForeColor = satColor;
      SatelliteAzimuthLabel.Text = $"{realSatBearing.AzDeg:F0}°";
      SatelliteElevationLabel.Text = $"{realSatBearing.ElDeg:F0}°";

      AntennaAzimuthLabel.BackColor = antColor;
      AntennaElevationLabel.BackColor = antColor;

      if (IsRunning() && AntBearing != null)
      {
        AntennaAzimuthLabel.Text = $"{AntBearing.AzDeg:F1}°";
        AntennaElevationLabel.Text = $"{AntBearing.ElDeg:F1}°";
      }
      else
      {
        AntennaAzimuthLabel.Text = "---";
        AntennaElevationLabel.Text = "---";
      }
    }

    private void Engine_StatusChanged(object? sender, EventArgs e)
    {
      // ant bearing color
      BearingToUi();

      ctx.MainForm.ShowRotatorStatus();
    }

    private void Engine_BearingChanged(object? sender, EventArgs e)
    {
      BearingToUi();
      ctx.SkyViewPanel?.Refresh();
    }

    //----------------------------------------------------------------------------------------------
    //                                   helper functions
    //----------------------------------------------------------------------------------------------
    private Bearing Sanitize(Bearing bearing)
    {
      var sett = ctx.Settings.Rotator;

      var sanitizedBearing = new Bearing(bearing.Az, bearing.El);
      sanitizedBearing.Az += sett.AzimuthOffset * Trig.RinD;
      sanitizedBearing.El += sett.ElevationOffset * Trig.RinD;

      var bounds = new RectangleF(
        sett.MinAzimuth * Trig.RinD,
        sett.MinElevation * Trig.RinD,
        (sett.MaxAzimuth - sett.MinAzimuth) * Trig.RinD,
        (sett.MaxElevation - sett.MinElevation) * Trig.RinD
      );
      sanitizedBearing = sanitizedBearing.Clamp(bounds);

      return sanitizedBearing;
    }

    private double AngleBetween(Bearing bearing1, Bearing bearing2)
    {
      bool azOnly = ctx.Settings.Rotator.MinElevation == ctx.Settings.Rotator.MaxElevation;
      return bearing1.AngleFrom(bearing2, azOnly);
    }

    private void ShowRotatorDebugInfo()
    {
      UpdatePathOptimizerForm();

      if (!dialog.Visible) dialog.Show();
      else dialog.BringToFront();
    }
    private void UpdatePathOptimizerForm()
    {
      dialog.UpdateContents(Path);
    }
  }
}
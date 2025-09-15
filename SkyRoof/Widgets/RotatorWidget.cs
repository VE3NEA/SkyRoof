﻿using SkyRoof;
using VE3NEA;

namespace SkyRoof
{

  public partial class RotatorWidget : UserControl
  {
    public Context ctx;
    private RotatorControlEngine? engine;
    private AzElEntryDialog Dialog = new();
    private SatnogsDbSatellite? Satellite;
    private Bearing SatBearing, LastWrittenBearing;
    private bool WasAboveHorizon = false;
    public Bearing? AntBearing { get => engine?.LastReadBearing; }

    public RotatorWidget()
    {
      InitializeComponent();
    }




    //----------------------------------------------------------------------------------------------
    //                                        engine
    //----------------------------------------------------------------------------------------------
    public void ApplySettings(bool restoreTracking = false)
    {
      if (engine != null) StopRotation();

      engine?.Dispose();
      engine = null;
      bool track = restoreTracking && TrackCheckbox.Checked;

      if (ctx.Settings.Rotator.Enabled)
      {
        engine = new RotatorControlEngine(ctx.Settings.Rotator);
        engine.StatusChanged += Engine_StatusChanged;
        engine.BearingChanged += Engine_BearingChanged;
      }

      ResetUi();
      TrackCheckbox.Checked = track;

      Advance();
      ctx.MainForm.ShowRotatorStatus();
    }

    internal void Retry()
    {
      engine?.Retry();
    }

    public bool IsRunning()
    {
      return engine != null && engine.IsRunning;
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

    public void RotateTo(Bearing bearing)
    {
      if (engine == null) return;

      var sanitizedBearing = Sanitize(bearing);
      engine.RotateTo(sanitizedBearing);
      LastWrittenBearing = sanitizedBearing;
    }

    public void StopRotation()
    {
      TrackCheckbox.Checked = false;
      WasAboveHorizon = false;
      engine?.StopRotation();
    }

    private Bearing Sanitize(Bearing bearing)
    {
      var sett = ctx.Settings.Rotator;
      var sanitizedBearing = new Bearing(bearing.Azimuth, bearing.Elevation);

      sanitizedBearing.Azimuth += sett.AzimuthOffset;
      sanitizedBearing.Elevation += sett.ElevationOffset;

      // todo: normalize before clamping?
      sanitizedBearing.Azimuth = Math.Max(sett.MinAzimuth, Math.Min(sanitizedBearing.Azimuth, sett.MaxAzimuth));
      sanitizedBearing.Elevation = Math.Max(sett.MinElevation, Math.Min(sanitizedBearing.Elevation, sett.MaxElevation));

      return sanitizedBearing;
    }

    public void SetSatellite(SatnogsDbSatellite? sat)
    {
      if (sat == Satellite) return;

      Satellite = sat;
      engine?.StopRotation();

      ResetUi();
      Advance();

      // show black LED if no satellite
      ctx.MainForm.ShowRotatorStatus();
    }





    //----------------------------------------------------------------------------------------------
    //                                        UI
    //----------------------------------------------------------------------------------------------
    private void AzEl_Click(object sender, EventArgs e)
    {
      Dialog.Open(ctx);
    }

    public void TrackCheckbox_CheckedChanged(object sender, EventArgs e)
    {
      if (TrackCheckbox.Checked)
        RotateTo(SatBearing);
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

    internal string? GetStatusString()
    {
      if (!ctx.Settings.Rotator.Enabled) return "Rotator control disabled";
      else if (!IsRunning()) return "No connection";
      else if (!TrackCheckbox.Checked) return "Connected, tracking disabled";
      else return "Connected and tracking";
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
      TrackCheckbox.Enabled = ctx.Settings.Rotator.Enabled && Satellite != null;
    }

    internal void Advance()
    {
      if (Satellite == null) return;

      var obs = ctx.SdrPasses.ObserveSatellite(Satellite, DateTime.UtcNow);
      if (obs == null || obs?.Azimuth == null || obs?.Elevation == null)
      {
        ResetUi();
        return;
      }

      SatBearing = new Bearing(obs.Azimuth.Degrees, obs.Elevation.Degrees);

      WasAboveHorizon = WasAboveHorizon || SatBearing.Elevation > 0;
      if (WasAboveHorizon && SatBearing.Elevation < -3) StopRotation();

      if (engine != null && TrackCheckbox.Checked)
      {
        var bearing = Sanitize(SatBearing);
        var diff = AngleBetween(bearing, LastWrittenBearing);
        if (diff >= ctx.Settings.Rotator.StepSize) RotateTo(SatBearing);
      }

      BearingToUi();

      ctx.Announcer.AnnouncePosition(SatBearing);
    }


    private void BearingToUi()
    {
      if (SatBearing == null) { ResetUi(); return; }

      Color satColor = TrackCheckbox.Checked ? Color.Aqua : Color.Teal;

      bool trackError = TrackCheckbox.Checked && (!IsRunning() || AntBearing == null || AngleBetween(SatBearing, AntBearing!) > 1.5 * ctx.Settings.Rotator.StepSize);
      Color antColor = trackError ? Color.LightCoral : Color.Transparent;

      SatelliteAzimuthLabel.ForeColor = satColor;
      SatelliteElevationLabel.ForeColor = satColor;
      SatelliteAzimuthLabel.Text = $"{SatBearing.Azimuth:F0}°";
      SatelliteElevationLabel.Text = $"{SatBearing.Elevation:F0}°";

      AntennaAzimuthLabel.BackColor = antColor;
      AntennaElevationLabel.BackColor = antColor;

      if (IsRunning() && AntBearing != null)
      {
        AntennaAzimuthLabel.Text = $"{AntBearing.Azimuth:F1}°";
        AntennaElevationLabel.Text = $"{AntBearing.Elevation:F1}°";
      }
      else
      {
        AntennaAzimuthLabel.Text = "---";
        AntennaElevationLabel.Text = "---";
      }
    }

    private double AngleBetween(Bearing bearing1, Bearing bearing2)
    {
      if (ctx.Settings.Rotator.MinElevation == ctx.Settings.Rotator.MaxElevation)
        // rotator is not elevation capable, so we only check azimuth
        return Bearing.AzimuthDifference(bearing1, bearing2);
      else
        return Bearing.AngleBetween(bearing1, bearing2);
    }

    public void ToggleTracking()
    {
      if (!TrackCheckbox.Enabled) return;
      TrackCheckbox.Checked = !TrackCheckbox.Checked;
      TrackCheckbox_CheckedChanged(StopBtn, EventArgs.Empty);
    }
  }
}

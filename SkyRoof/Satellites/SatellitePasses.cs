using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using CSCore.Win32;
using Serilog;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Observation;
using SGPdotNET.TLE;
using SGPdotNET.Util;
using SharpGL;
using VE3NEA;
using static SkyRoof.GroupSatellitePasses;

namespace SkyRoof
{
  public class GroupSatellitePasses : SatellitePasses
  {
    //----------------------------------------------------------------------------------------------
    //                                 GroupSatellitePasses
    //----------------------------------------------------------------------------------------------
    public GroupSatellitePasses(Context ctx) : base(ctx) 
    {
      PredictionTimeSpan = TimeSpan.FromDays(2);
    }

    public override void FullRebuild()
    {
      base.FullRebuild();
      ctx.Announcer.RebuildQueue();
    }

    public override void Rebuild()
    {
      base.Rebuild();
      ctx.Announcer.RebuildQueue();
    }

    public override SatellitePass[] PredictMorePasses()
    {
      var newPasses = base.PredictMorePasses();
      ctx.Announcer.AddToQueue(newPasses);
      return newPasses;
    }

    protected override IEnumerable<SatnogsDbSatellite> ListSatellites()
    {
        return ctx.SatelliteSelector.GroupSatellites;
    }
  }




  //----------------------------------------------------------------------------------------------
  //                                 HamSatellitePasses
  //----------------------------------------------------------------------------------------------
  public class HamSatellitePasses : SatellitePasses
  {
    public HamSatellitePasses(Context ctx) : base(ctx)
    {
      PredictionTimeSpan = TimeSpan.FromHours(2);
    }

    protected override IEnumerable<SatnogsDbSatellite> ListSatellites()
    {
      return ctx.SatnogsDb.Satellites.Where(
        sat => sat.Flags.HasFlag(SatelliteFlags.Vhf) || sat.Flags.HasFlag(SatelliteFlags.Uhf) ||
          sat.Transmitters.Any(tx => tx.IsHf()));
    }
  }




  //----------------------------------------------------------------------------------------------
  //                                 SdrSatellitePasses
  //----------------------------------------------------------------------------------------------
  public class SdrSatellitePasses : SatellitePasses
  {
    private double StartFrequency, EndFrequency;

    public SdrSatellitePasses(Context ctx) : base(ctx)
    {
      PredictionTimeSpan = TimeSpan.FromMinutes(6);
      UpdateFrequencyRange();
    }

    public void UpdateFrequencyRange()
    {
      double startFrequency = 0;
      double endFrequency = 0;

      if (ctx.WaterfallPanel != null && ctx.Sdr != null &&
        !SatnogsDbTransmitter.IsHamFrequency(ctx.FrequencyControl.GetSdrRfCenter()))
      {
        double centerFrequency = ctx.FrequencyControl.GetSdrRfCenter();
        double wing = ctx.Sdr.Info.MaxBandwidth / 2;
        startFrequency = centerFrequency - wing;
        endFrequency = centerFrequency + wing;
      }

      if (startFrequency == StartFrequency && endFrequency == EndFrequency) return;

      StartFrequency = startFrequency;
      EndFrequency = endFrequency;

      FullRebuild();
    }

    protected override IEnumerable<SatnogsDbSatellite> ListSatellites()
    {
      if (StartFrequency == 0 || EndFrequency == 0) return Array.Empty<SatnogsDbSatellite>();

      // the same transmitter must be within the SDR range, not one above the start and another below the end
      return ctx.SatnogsDb.Satellites.Where(sat =>
        sat.Transmitters.Any(tx => tx.DownlinkLow >= StartFrequency && tx.DownlinkLow <= EndFrequency));
    }
}




  //----------------------------------------------------------------------------------------------
  //                                     base class
  //----------------------------------------------------------------------------------------------
  public abstract class SatellitePasses
  {
    protected List<SatnogsDbSatellite> Satellites = new();
    private GroundStation GroundStation;
    protected readonly Context ctx;
    protected TimeSpan PredictionTimeSpan;
    private readonly TimeSpan HistoryTimeSpan = TimeSpan.FromMinutes(30);
    // longer than any LEO pass, so a search that starts this far back cannot begin inside a pass in progress
    private readonly TimeSpan MaxPassDuration = TimeSpan.FromMinutes(30);
    // how long after LOS an event still belongs to the pass that just ended. well short of the gap to the
    // next pass of the same satellite, so the two can never be confused
    internal static readonly TimeSpan LosGracePeriod = TimeSpan.FromMinutes(2);

    private DateTime LastPredictionTime = DateTime.MinValue;

    public List<SatellitePass> Passes = new();

    public SatellitePasses(Context ctx)
    {
      this.ctx = ctx;

      CreateGroundStation(ctx.Settings.User.Square, ctx.Settings.User.Altitude);
    }

    // call when satellite list or group changes
    // rebuilds list of valid satellites, recomputes all passes
    public virtual void FullRebuild()
    {            
      Satellites = ListSatellites().Where(sat => sat.Tle != null && sat.IsAlive()).ToList();

      var now = DateTime.UtcNow;
      Passes = ComputePasses(now, now + PredictionTimeSpan).ToList();
    }

    // call when TLE changes
    // recomputes all active and future passes      
    public virtual void Rebuild()
    {
      // keep historical passes
      var now = DateTime.UtcNow;
      var historyStartTime = now - HistoryTimeSpan;
      Passes = Passes.Where(p => p.EndTime > historyStartTime && p.EndTime < now).ToList();

      // recompute all current and future passes
      Passes.AddRange(ComputePasses(now, now + PredictionTimeSpan));
    }

    // call every minute
    public virtual SatellitePass[] PredictMorePasses()
    {
      DeleteOld();

      if (LastPredictionTime == DateTime.MinValue) return Array.Empty<SatellitePass>();

      // add new
      var startTime = LastPredictionTime + PredictionTimeSpan;
      var endTime = DateTime.UtcNow + PredictionTimeSpan;

      var newPasses = ComputePasses(startTime, endTime).Where(p => p.StartTime > startTime).ToArray();

      Passes.AddRange(newPasses);
      return newPasses;
    }

    // delete old passes but keep history
    private void DeleteOld()
    {
      var now = DateTime.UtcNow;
      var historyStartTime = now - HistoryTimeSpan;
      Passes = Passes.Where(p => p.EndTime > historyStartTime).ToList();
    }

    private IEnumerable<SatellitePass> ComputePasses(DateTime startTime, DateTime endTime)
    {
      LastPredictionTime = DateTime.UtcNow;

      return Satellites
        .SelectMany(sat => ComputePassesFor(sat, startTime, endTime))
        .OrderBy(p => p.StartTime)
        .ToArray(); // ToArray => computes passes now
    }

    public IEnumerable<SatellitePass> ComputePassesFor(SatnogsDbSatellite satellite, DateTime startTime, DateTime endTime)
    {
      if (!satellite.Tracker.Enabled) return new List<SatellitePass>();

      List<SatelliteVisibilityPeriod> passes;
      if (satellite.Tracker.IsGeoStationary())
        passes = satellite.Tracker.ComputeGeostationaryPasses(GroundStation);
      else
        passes = satellite.Tracker.ComputePasses(GroundStation, startTime, endTime);

      return passes.Select(p => new SatellitePass(GroundStation, satellite, p));
    }

    protected abstract IEnumerable<SatnogsDbSatellite> ListSatellites();

    private void CreateGroundStation(string gridSquare, double altitude)
    {
      var pos = GridSquare.ToGeoPoint(ctx.Settings.User.Square);
      var myLocation = new GeodeticCoordinate(Angle.FromRadians(pos.LatitudeRad), Angle.FromRadians(pos.LongitudeRad), altitude / 1000d);
      GroundStation = new GroundStation(myLocation);
    }

    internal TopocentricObservation? ObserveSatellite(SatnogsDbSatellite? satellite, DateTime utcNow)
    {
      return satellite?.Tracker.Observe(GroundStation, utcNow);
    }

    internal SatellitePass? GetNextPass(SatnogsDbSatellite? satellite)
    {
      if (satellite == null) return null;

      var now = DateTime.UtcNow;
      return ComputePassesFor(satellite, now, now.AddDays(1)).OrderBy(pass => pass.StartTime).FirstOrDefault();
    }

    // the pass in progress if there is one, otherwise the next one. GetNextPass cannot answer this near LOS:
    // its search starts at "now", and SGP.NET needs two consecutive above-horizon samples to open a visibility
    // period, so a pass with less than one time step left is never seen and the search rolls to the next orbit.
    // starting the search before the earliest possible AOS of a pass in progress keeps that pass visible for
    // its whole duration.
    // grace keeps a pass that has just ended current for that much longer, so that an event which arrives
    // seconds after LOS - a logged qso, a decoder's final flush - is attributed to the orbit it belongs to
    // rather than to the next one, an hour or more ahead
    internal SatellitePass? GetCurrentOrNextPass(SatnogsDbSatellite? satellite, TimeSpan grace = default)
    {
      if (satellite == null) return null;

      var now = DateTime.UtcNow;
      // a pass that ended "grace" ago may have opened that much earlier too, so the search starts back far
      // enough to see all of it
      return ComputePassesFor(satellite, now - MaxPassDuration - grace, now.AddDays(1))
        .Where(pass => pass.EndTime > now - grace)
        .OrderBy(pass => pass.StartTime)
        .FirstOrDefault();
    }
  }
}

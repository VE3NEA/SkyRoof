using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using SGPdotNET.Observation;
using static SkyRoof.SatnogsDbSatellite;
using Newtonsoft.Json;
using Serilog;

namespace SkyRoof
{
  public class SatnogsDbSatellite
  {
    public class Telemetry
    {
      public string decoder;
      public override string ToString() => decoder;
    }

    public class Telemetries : List<Telemetry>
    {
      public override string ToString() => string.Join(", ", this.Select(t => t.decoder));
    }



    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public string sat_id { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public int? norad_cat_id { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public int? norad_follow_id { get; set; }

    [ReadOnly(true)]
    [Category("Names")]
    [DisplayName("SatNOGS")]
    [Description("Name in the SatNOGS DB")]
    public string name { get; set; }

    [ReadOnly(true)]
    [Category("Names")]
    [DisplayName("SatNOGS Alt")]
    [Description("Alternative names in the SatNOGS DB")]
    public string names { get; set; }

    [Browsable(false)]
    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public string image { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public string status { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public DateTime? decayed { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public DateTime? launched { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public DateTime? deployed { get; set; }

    [Browsable(false)]
    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public string website { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public string @operator { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public string countries { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    [TypeConverter(typeof(TelemetriesTypeConverter))]
    public Telemetries telemetries { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public DateTime? updated { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public string citation { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    public bool is_frequency_violator { get; set; }

    [ReadOnly(true)]
    [Category("SatNOGS Database")]
    [TypeConverter(typeof(CsvTypeConverter))]
    public List<string> associated_satellites { get; set; }



    // names

    [ReadOnly(true)]
    [Category("Names")]
    [DisplayName("JE9PEL")]
    [Description("Name in the JE9PEL List")]
    [TypeConverter(typeof(CsvTypeConverter))]
    public List<string> JE9PEL_Names { get; set; } = new();

    [ReadOnly(true)]
    [Category("Names")]
    [DisplayName("Callsigns")]
    [Description("Callsigns in the JE9PEL List")]
    [TypeConverter(typeof(CsvTypeConverter))]
    public List<string> JE9PEL_Callsigns { get; set; } = new();

    [ReadOnly(true)]
    [Category("Names")]
    [DisplayName("LoTW")]
    [Description("Name recognized by LoTW")]
    public string? LotwName { get; set; }

    [ReadOnly(true)]
    [Category("Names")]
    [DisplayName("AMSAT")]
    [Description("Names on the AMSAT Satellite Status Page")]
    [TypeConverter(typeof(CsvTypeConverter))]
    public List<string> AmsatEntries { get; set; } = new();




    // orbit


    [ReadOnly(true)]
    [Category("Orbit")]
    [DisplayName("TLE")]
    public string? TleInfo {get; set; }

    [ReadOnly(true)]
    [Category("Orbit")]
    [DisplayName("Period, min")]
    [JsonIgnore]
    public int? Period { get; set; }

    [ReadOnly(true)]
    [Category("Orbit")]
    [DisplayName("Inclination, deg.")]
    public int? Inclination { get; set; }

    [ReadOnly(true)]
    [Category("Orbit")]
    [DisplayName("Elevation, km")]
    public int? Altitude { get; set; }

    [ReadOnly(true)]
    [Category("Orbit")]
    [DisplayName("Footprint, km")]
    public int? Footprint { get; set; }



    // non-browsable

    [Browsable(false)]
    public List<SatnogsDbTransmitter> Transmitters = new();

    [Browsable(false)]
    public SatnogsDbTle Tle;

    [Browsable(false)]
    public string SearchText;

    [Browsable(false)]
    public List<string> AllNames = new();

    [Browsable(false)]
    public SatelliteFlags Flags;

    [Browsable(false)]
    public List<JE9PELtransmitter> JE9PELtransmitters { get; set; } = new();

    [Browsable(false)]    
    public List<string> TransmittersHint { get => transmittersHint ??= FormatTransmitters(); } // build on demand
    private List<string>? transmittersHint;

    [Browsable(false)]
    internal SatelliteTracker Tracker { get => tracker ??= new SatelliteTracker(Tle); }
    private SatelliteTracker? tracker;


    private List<string> FormatTransmitters()
    {
      return Transmitters
        .Where(t => (t.IsVhf() || t.IsUhf()) && t.alive)
        .GroupBy(t => t.downlink_low)
        .Select(d => $"{d.Key / 1e6:F3}   {string.Join(", ", d.Select(r => r.mode).Distinct())}")
        .Order().ToList();
    }

    internal void BuildAllNames()
    {
      AllNames.Clear();
      AllNames.Add(name);
      AllNames.AddRange(names.Replace("\r\n", ",").Split(",").Select(n => n.Trim()));

      AllNames.AddRange(AmsatEntries);

      if (LotwName != null) { name = LotwName; AllNames.Add(LotwName); }
      
      if (Tle != null) AllNames.Add(Tle.tle0.StartsWith("0 ") ? Tle.tle0.Substring(2) : Tle.tle0);

      AllNames.AddRange(JE9PEL_Names);
      AllNames.AddRange(JE9PEL_Callsigns);

      AllNames = AllNames.Distinct().Where(n => n != "").ToList();
      SearchText = $"|{string.Join("|", AllNames.Select(MakeSearchText))}|{norad_cat_id}|";
    }

    internal void SetFlags()
    {
      // status
      if (IsAlive()) Flags = SatelliteFlags.Alive;
      else if (status == "future") Flags = SatelliteFlags.Future;
      else Flags = SatelliteFlags.ReEntered;

      // ham
      if (Transmitters.Any(t => t.service == "Amateur")) Flags |= SatelliteFlags.Ham;
      else Flags |= SatelliteFlags.NonHam;

      // band
      if (Transmitters.Any(t => t.IsVhf())) Flags |= SatelliteFlags.Vhf;
      if (Transmitters.Any(t => t.IsUhf())) Flags |= SatelliteFlags.Uhf;
      if ((Flags & (SatelliteFlags.Vhf | SatelliteFlags.Uhf)) == SatelliteFlags.None)
        Flags |= SatelliteFlags.OtherBands;

      // tx
      if (Transmitters.Any(t => t.type == "Transponder")) Flags |= SatelliteFlags.Transponder;
      if (Transmitters.Any(t => t.type == "Transceiver")) Flags |= SatelliteFlags.Transceiver;
      if ((Flags & (SatelliteFlags.Transponder | SatelliteFlags.Transceiver)) == SatelliteFlags.None)
        Flags |= SatelliteFlags.Transmitter;
    }

    // status vocabulary changed in SatNOGS DB 1.76: the old future/alive/dead/re-entered became
    // future/in-orbit/launch failed/re-entered, so "alive" is gone and the former "dead" sats are now
    // reported as in orbit. The release notes spell the new value "in-orbit" while the API returns it
    // with a space, so accept either, plus the old "alive" for a Satellites.json cached before 1.76.
    public bool IsAlive()
    {
      return status == "in orbit" || status == "in-orbit" || status == "alive";
    }

    public static string MakeSearchText(string s) { return Regex.Replace(s, "[^a-zA-Z0-9|]", "").ToLower(); }

    public string GetTooltipText()
    {
      string names = string.Join(", ", AllNames);
      string radio = "Transmitter";
      if (Flags.HasFlag(SatelliteFlags.Transponder)) radio = "Transponder";
      else if (Flags.HasFlag(SatelliteFlags.Transceiver)) radio = "Transceiver";
      ComputeOrbitDetails();

      string tooltipText = $"{names}\nNORAD: {norad_cat_id}\nstatus: {status}\ncountries: {countries}";
      if (Tle != null) tooltipText +=
          $"\nTLE: {TleInfo}\nperiod: {Period} min.\ninclination: {Inclination}°\n" +
          $"footprint: {Footprint} km\naltitude: {Altitude}";
      tooltipText += $"\nradio: {radio}";
      if (!string.IsNullOrEmpty(LotwName)) tooltipText += "\nAccepted by LoTW";
      tooltipText += $"\nupdated: {updated:yyyy-MM-dd}";

      return tooltipText;
    }

    internal void ComputeOrbitDetails()
    {
      if (Tle == null) return;

      TleInfo = $"{Tle.updated:yyyy-MM-dd HH:mm}Z ({ Tle.tle_source})";

      // inclination
      string s = Tle.tle2.Substring(8, 8);
      if (float.TryParse(s, CultureInfo.InvariantCulture, out float v)) Inclination = (int)v;

      // period
      s = Tle.tle2.Substring(52, 10);
      if (float.TryParse(s, CultureInfo.InvariantCulture, out v)) Period = (int)(1440f / v);

      try
      {
        var prediction = Tracker.Predict();
        if (prediction == null) return;

        Footprint = (int)(2 * prediction.GetFootprint()); // diameter in km
        Altitude = (int)prediction.Altitude;
      }
      catch { }
    }
  }



  public class SatnogsDbSatelliteList : List<SatnogsDbSatellite> { }

  public class CsvTypeConverter : StringConverter
  {
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      return string.Join(", ", (List<string>)value);
    }
  }

  public class TelemetriesTypeConverter : StringConverter
  {
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      return ((Telemetries)value).ToString();
    }
  }
}
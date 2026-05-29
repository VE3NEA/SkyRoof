using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using Serilog;
using VE3NEA;

namespace SkyRoof
{
  public class Announcer
  {
    // 'break time 1s' is needed because on Windows 11 the first part of the message is sometimes cut off
    private const string SsmlMessage = 
      "<?xml version=\"1.0\"?><speak version=\"1.0\" " +
      "xmlns=\"http://www.w3.org/2001/10/synthesis\" " +
      "xml:lang=\"{0}\"><break time='1s'/>{1}</speak>";

    private const string NameFormat = "<say-as interpret-as=\"characters\">{0}</say-as> {1}";

    public Context? ctx;

    private readonly SpeechSynthesizer Synth = new();
    public readonly InstalledVoice[] Voices;
    private List<SatellitePass> Queue1 = new();
    private List<SatellitePass> Queue2 = new();
    private Bearing LastBearing = new(0,0);

    public Announcer() {
      Synth = new SpeechSynthesizer();
      Synth.SetOutputToDefaultAudioDevice();

      try
      {
        SpeechApiReflectionHelper.InjectOneCoreVoices(Synth);
      }
      catch (Exception ex)
      {
        Log.Warning(ex, "Failed to load extra voices for speech syntheizer");
      }

      Voices = Synth.GetInstalledVoices().ToArray();
    }




    //----------------------------------------------------------------------------------------------
    //                                       queue
    //----------------------------------------------------------------------------------------------
    public void RebuildQueue()
    {
      Queue1.Clear();
      Queue2.Clear();

      var sett = ctx!.Settings.Announcements;

      var groupPasses = ctx.GroupPasses.GetPassesSnapshot();
      var minStartTime = DateTime.UtcNow.AddMinutes(sett.Announcement1.Minutes).AddSeconds(1);
      if (sett.Announcement1.Enabled)
        Queue1 = groupPasses.Where(p => p.StartTime > minStartTime).ToList();

      minStartTime = DateTime.UtcNow.AddMinutes(sett.Announcement2.Minutes).AddSeconds(1);
      if (sett.Announcement2.Enabled)
        Queue2 = groupPasses.Where(p => p.StartTime > minStartTime).ToList();
    }

    public void AddToQueue(IEnumerable<SatellitePass> passes)
    {
      var sett = ctx!.Settings.Announcements;
      if (sett.Announcement1.Enabled) Queue1.AddRange(passes);
      if (sett.Announcement2.Enabled) Queue2.AddRange(passes);
    }

    public void AnnouncePasses()
    {
      var sett = ctx!.Settings.Announcements;

      var announceTime = DateTime.UtcNow.AddMinutes(sett.Announcement1.Minutes).AddSeconds(1);
      for (int i = Queue1.Count - 1; i >= 0; i--)
        if (Queue1[i].StartTime < announceTime)
        {
          Announce(Queue1[i].Satellite.name, sett.Announcement1);
          Queue1.RemoveAt(i);
        }

      announceTime = DateTime.UtcNow.AddMinutes(sett.Announcement2.Minutes).AddSeconds(1);
      for (int i = Queue2.Count - 1; i >= 0; i--)
        if (Queue2[i].StartTime < announceTime)
        {
          Announce(Queue2[i].Satellite.name, sett.Announcement2);
          Queue2.RemoveAt(i);
        }
    }




    //----------------------------------------------------------------------------------------------
    //                                       announce
    //----------------------------------------------------------------------------------------------
    public void AnnouncePosition(Bearing? bearing)
    {
      if (bearing == null) return;  
      if (!ctx!.Settings.Announcements.PositionAnnouncement.Enabled) return;
      if (bearing.ElDeg < 0) return;  // Use ElDeg since we're comparing with 0 degrees
      
      // Convert degrees threshold to radians for comparison with AngleBetween (which returns radians)
      double thresholdRad = ctx.Settings.Announcements.PositionAnnouncement.Degrees * Math.PI / 180.0;
      if (bearing.AngleFrom(LastBearing) < thresholdRad) return;
      
      LastBearing = bearing;

      // Use AzDeg/ElDeg for string formatting since we want degrees in the message
      string message = ctx.Settings.Announcements.PositionAnnouncement.Message
        .Replace("{azimuth}", bearing.AzDeg.ToString("F0"))
        .Replace("{elevation}", bearing.ElDeg.ToString("F0"));

      Say(message);
    }


    // this does not work for some voices:
    //private const string MinutesFormat = @"<say-as interpret-as=""duration"" format=""m"">{0}</say-as>";

    public void Announce(string name, AosAnnouncement announcement)
    {
      name = FormatSatName(name);
      string minutes = announcement.Minutes.ToString();
      string message = announcement.Message.Replace("{name}", name).Replace("{minutes}", minutes);

      Say(message);
    }

    private void Say(string message)
    {
      var voice = Voices.FirstOrDefault(v => GetVoiceName(v) == ctx!.Settings.Announcements.Voice)?.VoiceInfo;
      string culture = "en-US";
      
      if (voice != null)
      {
        Synth.SelectVoice(voice.Name);
        culture = voice.Culture.Name;
      }

      Synth.Volume = ctx.Settings.Announcements.Volume;
      Synth.SpeakSsmlAsync(string.Format(SsmlMessage, culture, message));
    }

    // for the Settings dialog
    public void SayVoiceName(string? name)
    {
      if (name == null) return;
      var voice = Voices.FirstOrDefault(v => GetVoiceName(v) == name)?.VoiceInfo;
      if (voice == null) return;

      Synth.SelectVoice(voice.Name);
      Synth.Volume = ctx.Settings.Announcements.Volume;
      string ssml = string.Format(SsmlMessage, voice.Culture.Name, voice.Name);
      Synth.SpeakSsmlAsync(ssml);
    }


    // debugging: format sat name for speech
    public static void SaySatName(string name)
    {
      var synth = new SpeechSynthesizer();
      synth.SetOutputToDefaultAudioDevice();
      string ssml = string.Format(SsmlMessage, "en-US", FormatSatName(name));
      synth.SpeakSsmlAsync(ssml);
    }




    //----------------------------------------------------------------------------------------------
    //                                         ssml
    //----------------------------------------------------------------------------------------------
    public static string GetVoiceName(InstalledVoice? voice)
    {
      if (voice == null) return "";
      return $"{voice.VoiceInfo.Name} : {voice.VoiceInfo.Culture.NativeName}";
    }

    public static string FormatSatName(string name)
    {
      // pronounce "SAT" and "CUBE" as words
      name = name.ToLower().Replace("-", " ").Replace("sat", "-sat-").Replace("cube", "-cube-");

      // in AO-07, "AO" is an acronym
      string letters = Regex.Replace(name, "[^a-z]", ".");
      int pos = letters.IndexOf(".");
      if (pos <= 0) pos = name.Length;

      // pronounce acronyms as characters
      if (pos < 4) 
        name = string.Format(NameFormat, name.Substring(0, pos), name.Substring(pos).Trim().ToLower());

      return name + "<break time=\"0.1s\"/>";
    }
  }
}

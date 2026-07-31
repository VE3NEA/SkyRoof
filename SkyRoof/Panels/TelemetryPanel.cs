using FontAwesome;
using MathNet.Numerics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using Serilog;
using SkyRoof.Satellites;
using VE3NEA;
using VE3NEA.SkyFM;
using VE3NEA.SkySSTV;
using VE3NEA.SkyTlm.Core;
using VE3NEA.SkyTlm.Deframing;
using VE3NEA.SkyTlm.Discovery;
using VE3NEA.SkyTlm.Telemetry;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class TelemetryPanel : DockContent
  {
    private readonly Context ctx;
    private SatnogsDbSatellite? Satellite;
    private SatnogsDbTransmitter Transmitter;
    private bool Terrestrial;
    private bool SatAboveHorizon = false;
    private SignalParams? SignalParams;
    // The ranked co-channel telemetry sibling (§2.3 of cochannel_sstv_pairing_plan) and its own freshly
    // resolved params. Non-null only when an SSTV transmitter is selected and its downlink carries a
    // decodable sibling; null in every other case, which is why nothing changes for an unpaired selection.
    // The pipeline's run-time findings (ResolvedBaud / ResolvedDeviation) are written back into this params
    // object, so it must be resolved once per transmitter change and then left alone.
    private (SatnogsDbTransmitter Transmitter, SignalParams Params)? Sibling;
    // §3: the (transmitter, params) pair that drives the telemetry pipeline — the selection itself whenever
    // the selection is telemetry-decodable (identical to today in every unpaired case), the sibling when an
    // SSTV transmitter is selected, and null for a CW / FM / unsupported selection, which never pulls in a
    // sibling. Used for two things only: constructing the StreamingPipeline and stamping the frames'
    // DecodeSnapshot. SignalParams, ResolvedSnapshot, UserChangedFields, the dots and the gear all stay
    // bound to the selection.
    private (SatnogsDbTransmitter Transmitter, SignalParams Params)? TelemetrySource =>
      IsTelemetryDecodable() ? (Transmitter, SignalParams!) : Sibling;
    private TelemetryDecocder? Decoder;
    private SatnogsUploader? SatnogsUploader;
    private TelemetryRegistry? TelemetryRegistry;
    // the most recently added tree node: either the pass node itself (before it has any leaves) or its
    // last-added leaf. Used only to keep the tree selection following new content (see TrackNewNode).
    // It used to double as "the current pass node" (this node's parent, or itself when it has no leaves),
    // but a co-channel pair has TWO live pass nodes at once, so the pass a frame or image belongs to is
    // resolved by identity in EnsureCurrentPassNode instead of by position here.
    private TreeNode? Current;
    private ILogger? FrameLogger;
    private DecodeSnapshot? CurrentDecode;
    // the status label's default text color, captured before any status update recolors it
    
    // provenance state for the SignalParams dialog's status dots and the gear button. All of it is per
    // transmitter and reset on every transmitter change (see SetTransmitter). ResolvedSnapshot is the pristine
    // DB-resolved params captured before the pipeline writes back any finding, used to tell a pipeline-discovered
    // Differential from a curated one (Baud/Deviation carry their own ResolvedBaud/ResolvedDeviation instead).
    private SignalParams? ResolvedSnapshot;
    // names of the SignalParams fields the user has manually overridden (subset of the demod fields)
    private readonly HashSet<string> UserChangedFields = new();
    // a frame has decoded since the last demod override — turns the user-changed demod dots (and the gear) green
    private bool DemodValidated;
    // user-selected telemetry-format definition (null = resolve by NORAD) and whether a frame has parsed with it
    private TelemetryDefinition? FormatOverride;
    private string? FormatOverrideId;
    private bool FormatValidated;

    // parameter discovery (discover_params_plan.md): the running search, the dialog it reports to, and the
    // count of frames decoded since a discovered set was applied — the evidence the operator saves on (§6.1).
    // All three are null/zero unless the operator has pressed Discover; discovery costs nothing when idle.
    private DiscoverySession? Discovery;
    private SignalParamsDialog? ParamsDialog;
    private int FramesSinceDiscovery = -1;
    // bursts the running session took for analysis, so the status line can tell analyzing (one is under
    // analysis) from waiting (the search is idle between bursts): the session counts a burst as analyzed
    // only once it is done with it. A burst dropped because the previous one was still running is not
    // counted here — the analysis it would have started never began.
    private int BurstsTaken;
    // bursts a pass may produce without a valid frame before the status label suggests Discover. A few
    // marginal bursts decoding nothing is ordinary; a run of them is what wrong parameters look like.
    private const int DiscoverHintBursts = 5;
    // template width, in Bd, for a discovery detector on a transmitter whose DB row carries no rate at all
    // (CW, SSTV, FM voice). Mid-range on purpose: too wide a template sums noise across bins the signal
    // never fills, too narrow a one sits inside a fast signal and still detects it.
    private const double DefaultDetectBaud = 4800;

    // the FM speech-to-text engine (integration §10). It loads a large model (~71 MB, ~1.5 s), so a single
    // instance is created lazily on first use and SHARED across transmitter changes rather than rebuilt with
    // every decoder; disposed on panel close. Null until an FM transmitter is selected with the model present.
    private SherpaOnnxEngine? FmSpeechEngine;
    // the FM transcript currently shown in richTextBox1 (null while showing telemetry/SSTV) — routes the
    // click-to-play mouse handling to the right content
    private FmTranscriptInfo? CurrentFmTranscript;
    // true when PlayFmClip found the shared speaker soundcard disabled and temporarily enabled it for the
    // clip, mirroring RecordingManager.StartPlayback/StopPlayback; fires FmClipEndTimer to disable it again
    private bool SpeakerEnabledForFmClip;
    private System.Windows.Forms.Timer? FmClipEndTimer;

    // Identity of the transmitter a decoder was built for, captured when the pipeline is created and bound to
    // that pipeline's event handlers. Frames surface on the decode worker thread, possibly after the user has
    // switched to a different transmitter; carrying the snapshot with the frame keeps it attributed to the
    // transmitter that actually produced it instead of to whatever is selected when the frame arrives.
    private sealed class DecodeSnapshot
    {
      internal readonly SatnogsDbSatellite? Satellite;
      internal readonly SatnogsDbTransmitter Transmitter;
      internal readonly SignalParams SignalParams;
      // The orbit the decoder was built in, captured here rather than re-queried when an event arrives.
      // GetNextPass returns the first pass starting from *now*, so it rolls to the NEXT orbit the instant the
      // current pass ends — and the events that arrive at exactly that moment are the decoder's own flush.
      // Re-deriving the orbit there files them under a pass that has not happened yet.
      internal readonly int Orbit;

      internal DecodeSnapshot(SatnogsDbSatellite? satellite, SatnogsDbTransmitter transmitter, SignalParams signalParams, int orbit)
      {
        Satellite = satellite;
        Transmitter = transmitter;
        SignalParams = signalParams;
        Orbit = orbit;
      }
    }

    // one progressively-built SSTV image: the tree node's Tag, updated in place as ImageUpdated events
    // re-render lines, finalized (and auto-saved) on ImageCompleted
    private sealed class SstvImageInfo
    {
      internal readonly DecodeSnapshot Snapshot;
      internal readonly DateTime FirstSeen = DateTime.Now;
      internal SstvImageEvent Event;
      internal Bitmap? Bitmap;
      internal string? SavedPath;

      internal SstvImageInfo(DecodeSnapshot snapshot, SstvImageEvent evt)
      {
        Snapshot = snapshot;
        Event = evt;
      }

      internal string Describe()
      {
        return
          $"Sat: {Snapshot.Transmitter?.Satellite?.name ?? "Unknown"}\r\n" +
          $"Tx: {Snapshot.Transmitter?.description}\r\n" +
          $"Mode: {Event.Mode}\r\n" +
          $"VIS: {(Event.FromVis ? "decoded" : "not decoded, mode from sync cadence")}\r\n" +
          $"Rows: {Event.ValidRows} of {Event.Image.Height}\r\n" +
          $"Status: {(Event.Final ? "complete" : "receiving...")}\r\n" +
          (SavedPath != null ? $"Saved: {SavedPath}\r\n" : "");
      }
    }

    // one FM-speech transcript, the Tag of the single "FM Speech" leaf node (§10.3): the pass's decoded
    // lines appended in place, plus the in-progress open line. Each completed line carries the 16 kHz audio
    // fragment that produced it (captured on the decode thread while the decoder is alive) so click-to-play
    // works independently of the decoder's lifetime (§10.4).
    private sealed class FmTranscriptInfo
    {
      internal readonly DecodeSnapshot Snapshot;
      // retained even after the decoder that produced it is disposed (its accumulated audio buffer isn't
      // freed by Dispose), so the still-open line's audio can be fetched on demand at click time
      internal readonly SkySpeechDecoder Engine;
      internal readonly int SampleRate;
      internal TreeNode? Node;
      internal readonly List<FmLineEntry> Lines = new();
      internal FmTranscriptLine? Pending;
      // char-range → audio map for the rendered richTextBox text, rebuilt on each render (completed lines only)
      internal readonly List<(int Start, int End, FmLineEntry Line)> Spans = new();
      // char-range of the in-progress line's text, rebuilt on each render; null when no line is open
      internal (int Start, int End)? PendingSpan;

      internal FmTranscriptInfo(DecodeSnapshot snapshot, SkySpeechDecoder engine, int sampleRate)
      {
        Snapshot = snapshot;
        Engine = engine;
        SampleRate = sampleRate;
      }
    }

    // one completed transcript line: the display text, its time since decode start (for the MM:SS column),
    // and the true-peak-normalized 16 kHz audio fragment that produced it
    private sealed class FmLineEntry
    {
      internal readonly string Text;
      internal readonly double StartSeconds;
      internal readonly float[] Audio;
      internal FmLineEntry(string text, double startSeconds, float[] audio)
      {
        Text = text;
        StartSeconds = startSeconds;
        Audio = audio;
      }
    }

    internal class TxPassInfo
    {
      internal DateTime StartTime = DateTime.Now;
      internal SatnogsDbTransmitter Transmitter;
      internal int Orbit;
      internal SignalParams? SignalParams;
      internal int BurstCount = 0;
      internal int FrameCount = 0;
      internal int ImageCount = 0;
      internal double MaxSnrDb = double.NaN;
      internal bool HasValidFrame = false;

      internal TxPassInfo(SatnogsDbTransmitter transmitter, int orbit)
      {
        Transmitter = transmitter;
        Orbit = orbit;
      }

      internal bool IsSame(SatnogsDbTransmitter transmitter, int orbit)
      {
        return Transmitter.uuid == transmitter.uuid && Orbit == orbit;
      }

      internal string Describe(string paramsText)
      {
        return
          $"Start: {StartTime:yyyy-MM-dd HH:mm:ss}\n" +
          $"Sat: {Transmitter?.Satellite?.name ?? "Unknown"}\n" +
          $"Tx: {Transmitter.description}\n" +
          $"Norad: {Transmitter?.Satellite?.norad_cat_id}\n" +
          $"Uuid: {Transmitter.uuid}\n" +
          $"Orbit: {Orbit}\n\n" +
          $"Bursts: {BurstCount}\n" +
          $"Frames: {FrameCount}\n" +
          $"Images: {ImageCount}\n" +
          (double.IsNaN(MaxSnrDb) ? "" : $"Max. SNR: {MaxSnrDb:F1} dB\n") +
          "\n" +
          $"{paramsText}";
      }
    }


    //----------------------------------------------------------------------------------------------
    //                                         system
    //----------------------------------------------------------------------------------------------
    // only for designer
    public TelemetryPanel()
    {
      InitializeComponent();
    }

    public TelemetryPanel(Context ctx)
    {
      Log.Information("Creating TelemetryPanel");
      this.ctx = ctx;

      InitializeComponent();

      // gear button: draw the icon as an Awesome-font glyph so its state is shown via the foreground color
      SettingsButton.Image = null;
      SettingsButton.Font = ctx.AwesomeFont14;
      SettingsButton.Text = FontAwesomeIcons.Gear;

      string path = Path.Combine(Utils.GetUserDataFolder(), "TelemetryRegistry");
      TelemetryRegistry = new TelemetryRegistry(path);

      ctx.TelemetryPanel = this;
      ctx.MainForm.TelemetryMNU.Checked = true;

      SatnogsUploader = new SatnogsUploader(ctx);

      // FM speech transcript click-to-play (§10.4): hand cursor over a clickable line, play its audio on click
      richTextBox1.MouseMove += richTextBox1_MouseMove;
      richTextBox1.MouseClick += richTextBox1_MouseClick;

      SetTransmitter();
    }

    private void TelemetryPanel_Shown(object? sender, EventArgs e)
    {
      splitContainer1.SplitterDistance = ctx.Settings.Telemetry.SplitterDistance;
      ImageSplitContainer.SplitterDistance = ctx.Settings.Telemetry.ImageSplitterDistance;
    }

    private void TelemetryPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing TelemetryPanel");
      ctx.TelemetryPanel = null;
      ctx.MainForm.TelemetryMNU.Checked = false;
      ctx.Settings.Telemetry.SplitterDistance = splitContainer1.SplitterDistance;
      ctx.Settings.Telemetry.ImageSplitterDistance = ImageSplitContainer.SplitterDistance;

      // stop and free the decode pipeline (joins its worker thread and releases native FFTW memory)
      Decoder?.Dispose();
      Decoder = null;
      CurrentDecode = null;

      // stop any click-to-play clip, then free the shared FM speech engine (the decoder above no longer uses it)
      StopFmClip();
      FmSpeechEngine?.Dispose();
      FmSpeechEngine = null;

      SatnogsUploader?.Dispose();
      SatnogsUploader = null;

      (FrameLogger as IDisposable)?.Dispose();
      FrameLogger = null;
    }




    //----------------------------------------------------------------------------------------------
    //                                     pipeline
    //----------------------------------------------------------------------------------------------
    internal void SetTransmitter()
    {
      var newSatellite = ctx.SatelliteSelector.SelectedSatellite;
      var newTransmitter = ctx.SatelliteSelector.SelectedTransmitter;
      bool newTerrestrial = ctx.FrequencyControl.RadioLink.IsTerrestrial;

      // a redundant re-selection of the same transmitter (e.g. a band switch that re-raises the event) must keep
      // the resolved params, the manual-override state and the pipeline's run-time findings intact. Otherwise the
      // panel's SignalParams is replaced by a fresh copy while the still-running decoder keeps writing findings
      // (locked baud/deviation) into the old object, so the tooltip / dialog dots / gear stop reflecting them.
      bool sameTransmitter = !newTerrestrial && !Terrestrial && Transmitter != null
        && Transmitter.uuid == newTransmitter.uuid && SignalParams != null;
      if (sameTransmitter)
      {
        Satellite = newSatellite;
        Transmitter = newTransmitter;
        UpdateTxStatus();
        return;
      }

      Satellite = newSatellite;
      Transmitter = newTransmitter;
      Terrestrial = newTerrestrial;

      // a new transmitter discards any manual override / provenance state and resets the gear button color
      UserChangedFields.Clear();
      DemodValidated = false;
      FormatOverride = null;
      FormatOverrideId = null;
      FormatValidated = false;
      SettingsButton.ForeColor = Color.Gray;

      if (Terrestrial) SatNameLabel.Text = "Terrestrial";
      else SatNameLabel.Text = $"{Satellite.name}  {Transmitter.description}";

      ResolveSignalParams();
      UpdateTxStatus();
      CreatDestroyPipeline();
    }

    private void ResolveSignalParams()
    {
      if (Terrestrial)
      {
        SignalParams = null;
        Sibling = null;
        return;
      }

      SignalParams = SignalParamsResolver.Resolve(Transmitter);
      // snapshot the pristine DB-resolved params before the pipeline writes any finding back into SignalParams
      ResolvedSnapshot = SignalParams is null ? null : SignalParams with { };
      ResolveSibling();
      UpdateParamsTooltip();
    }

    // Resolve the co-channel transmitter that drives telemetry when the selection cannot (§2.2 row 2): an
    // SSTV selection borrows the top-ranked decodable transmitter on its own downlink. A selection that is
    // telemetry-decodable already is its own source and needs no sibling, and a CW / FM / unsupported one
    // deliberately gets none — SSTV is the only pairing this feature makes.
    private void ResolveSibling()
    {
      Sibling = null;
      if (IsTelemetryDecodable() || !IsSstvDecodable()) return;

      var tx = CoChannel.RankedTelemetrySibling(Satellite, Transmitter);
      if (tx != null && SignalParamsResolver.Resolve(tx) is SignalParams p) Sibling = (tx, p);
    }

    /// <summary>Refresh the params tooltip on both status labels with the same "name: value" fields the Signal
    /// Details dialog shows (the values actually used for decoding, so the pipeline's locked deviation/baud
    /// replace the curated ones once found), plus the telemetry format its frames are parsed with. Re-called
    /// when a frame arrives so the actual values replace the initial ones once the pipeline locks them.</summary>
    private void UpdateParamsTooltip()
    {
      // pass the pristine DB snapshot so a pipeline-discovered precoding (Differential, overwritten in place with
      // no self-contained flag) is asterisked here too, the same way baud/deviation are
      string tooltip = DescribeSignalParamsOrUnknown(SignalParams, ResolvedSnapshot);
      // §4: with the gear hidden while a sibling drives telemetry, the tooltip is the only place its
      // parameters are visible — append its block, under its own transmitter description so the two blocks
      // cannot be confused, below the selected transmitter's.
      if (Sibling is { } sibling)
        tooltip += $"\n\n{sibling.Transmitter.description}\n{DescribeSignalParams(sibling.Params)}";
      toolTip1.SetToolTip(SatNameLabel, tooltip);
      toolTip1.SetToolTip(StatusLabel, tooltip);
    }

    // the tooltip-style params text, or "Parameters unknown" when there are none. db is the pristine DB-resolved
    // baseline used to flag a pipeline-discovered precoding; null (the pass-summary callers) leaves it unflagged.
    private string DescribeSignalParamsOrUnknown(SignalParams? p, SignalParams? db = null) =>
      p == null ? "Parameters unknown" : DescribeSignalParams(p, db);

    // the dialog's fields as "name: value" lines. Baud/Deviation show the pipeline finding when present, else the
    // curated value; the telemetry format is the manual override when set, else the NORAD-resolved definition.
    // Fields without a value (unknown enums, null numerics, tri-states left on Auto, an unresolved format) are omitted.
    private string DescribeSignalParams(SignalParams p, SignalParams? db = null)
    {
      // baud/deviation carry their own run-time-vs-curated flag (self-contained); precoding needs the db baseline
      // to detect a run-time change. A changed value shows with an asterisk the same way the META block marks it.
      var lines = EnumSignalParamFields(p, db).Select(f => $"{f.Name}: {f.Value}{(f.Changed ? " *" : "")}").ToList();
      string? format = FormatOverrideId ?? ResolveFormat(Satellite?.norad_cat_id, p.Framing)?.Id;
      if (!string.IsNullOrEmpty(format)) lines.Add($"Telemetry format: {format}");
      return string.Join("\n", lines);
    }

    // the "  name: value" signal-param lines for the META block: same fields, indented, and any value the
    // pipeline resolved at run time to something other than the DB-resolved value flagged with a trailing '*'.
    private string DescribeSignalParamsMeta(DecodeSnapshot snapshot)
    {
      var p = snapshot.SignalParams;
      // the pristine DB snapshot is only known for the currently selected transmitter's decoder — and it
      // describes the SELECTION, so it is no baseline for a sibling's frames or a co-channel SSTV image
      var db = ReferenceEquals(snapshot, CurrentDecode) && ReferenceEquals(snapshot.Transmitter, Transmitter)
        ? ResolvedSnapshot : null;

      var lines = EnumSignalParamFields(p, db)
        .Select(f => $"  {f.Name}: {f.Value}{(f.Changed ? " *" : "")}").ToList();

      string? usedFormat = FormatFor(snapshot)?.Id;
      if (!string.IsNullOrEmpty(usedFormat))
      {
        string? dbFormat = ResolveFormat(snapshot.Satellite?.norad_cat_id, p.Framing)?.Id;
        lines.Add($"  Telemetry format: {usedFormat}{(usedFormat != dbFormat ? " *" : "")}");
      }
      return lines.Count == 0 ? "" : string.Join("\n", lines) + "\n";
    }

    // the valued signal-param fields as (name, value, changed) tuples, skipping any field without a value. When
    // a DB-resolved snapshot is supplied, Changed marks a value the pipeline discovered at run time that differs
    // from the curated one (a run-time baud/deviation lock, or a discovered precoding mode).
    private IEnumerable<(string Name, string Value, bool Changed)> EnumSignalParamFields(SignalParams p, SignalParams? db)
    {
      if (p.Modulation != Modulation.Unknown)
        yield return ("Modulation", p.Modulation.ToString(), false);
      if (p.Framing != Framing.Unknown)
        yield return ("Framing", p.Framing.ToString(), false);

      double baud = p.ResolvedBaud ?? p.Baud;
      if (baud != 0)
        yield return ("Baud rate", FormatTlmNumber(baud), p.ResolvedBaud != null && p.ResolvedBaud != p.Baud);

      double? deviation = p.ResolvedDeviation ?? p.Deviation;
      if (deviation is double dev)
        yield return ("Deviation, Hz", FormatTlmNumber(dev), p.ResolvedDeviation != null && p.ResolvedDeviation != p.Deviation);

      if (p.AfCarrier is double afCarrier)
        yield return ("AF carrier, Hz", FormatTlmNumber(afCarrier), false);

      if (p.Manchester is bool manchester)
        yield return ("Manchester", manchester ? "On" : "Off", false);

      if (p.Differential is bool differential)
        yield return ("Precoding (diff.)", differential ? "On" : "Off", db != null && differential != db.Differential);
    }

    private static string FormatTlmNumber(double value) =>
      value.ToString("0.###", System.Globalization.CultureInfo.CurrentCulture);

    private static string FormatTlmNullable(double? value) => value is double v ? FormatTlmNumber(v) : "";

    private static string FormatTriState(bool? value) => value switch { true => "On", false => "Off", null => "Auto" };

    private void CreatDestroyPipeline()
    {
      bool aboveAndUp = !Terrestrial && SatAboveHorizon;
      // §2.2: the telemetry branch follows the resolved telemetry source, which is the selection itself
      // unless an SSTV transmitter is selected; the SSTV branch follows the whole downlink, not the
      // selection. Both may name a transmitter other than the selected one.
      var telemetrySource = TelemetrySource;
      bool telemetry = telemetrySource != null;
      bool sstv = IsSstvBranchWanted();
      // the FM branch runs only with the model downloaded; the engine loads lazily here (once per session,
      // then shared) so an FM transmitter with no model simply builds no FM branch (status reflects it)
      var fmEngine = aboveAndUp && IsFmDecodable() ? EnsureFmEngine() : null;
      // a discovery session needs a burst source. The telemetry pipeline is one, but it does not exist when
      // the transmitter is CW/SSTV/FM or its format is unsupported — exactly the cases the operator reaches
      // for Discover in — so the search brings its own detection-only pipeline (§4.1).
      var detectParams = Discovery != null && !telemetry && SignalParams != null ? DiscoveryDetectorParams() : null;
      bool needPipeline = aboveAndUp && (telemetry || sstv || fmEngine != null || detectParams != null);

      // keep the existing decoder only if it was built for the currently selected transmitter. a transmitter
      // change must rebuild the pipeline: otherwise it keeps decoding with the previous transmitter's params
      // and its frames get attributed to the newly selected transmitter (wrong sat/norad/telemetry parser).
      // starting or ending a search changes which branches the decoder carries, so it rebuilds for that too.
      // the identity that matters is the TELEMETRY SOURCE's, not the selection's: switching between the two
      // members of a co-channel pair resolves to the same pair of branches, and tearing the decoder down
      // would throw away a locked baud and the in-progress SSTV image for no gain.
      bool matches = Decoder != null && CurrentDecode != null
        && CurrentDecode.Transmitter.uuid == (telemetrySource?.Transmitter ?? Transmitter).uuid
        && (Decoder.Sstv != null) == sstv
        && (Decoder.Detector != null) == (detectParams != null);

      if (needPipeline && matches)
      {
        // A kept decoder goes on writing its run-time findings (a locked baud / deviation) into the params
        // object it was BUILT with. When the selection has just moved onto that decoder's own telemetry
        // source — the other member of a co-channel pair — ResolveSignalParams has meanwhile replaced
        // SignalParams with a fresh resolution of the same transmitter, which carries none of those
        // findings. Adopt the running object so the tooltip, the dots and the gear keep showing the values
        // the pipeline is actually using (§3). ResolvedSnapshot is left alone: it is the pristine DB
        // baseline, and that is exactly what the asterisks are measured against.
        if (CurrentDecode != null && ReferenceEquals(CurrentDecode.Transmitter, Transmitter)
          && !ReferenceEquals(CurrentDecode.SignalParams, SignalParams))
        {
          SignalParams = CurrentDecode.SignalParams;
          UpdateParamsTooltip();
          UpdateGearButton();
        }
        return;
      }
      if (!needPipeline && Decoder == null) return;

      // destroy the existing decoder: purge its queued IQ (recorded for the old transmitter / tuning) so the
      // backlog is discarded rather than decoded and mis-attributed, then dispose it
      if (Decoder != null)
      {
        Decoder.Purge();
        var old = Decoder;
        Decoder = null;
        CurrentDecode = null;
        old.Dispose();
      }

      // create the new decoder, binding the current selection to its handlers so every frame it emits is
      // attributed to this transmitter even if the selection changes while the frame is in flight
      if (needPipeline)
      {
        // one decoder, but up to two identities in it: telemetry events are attributed to the telemetry
        // source and SSTV events to the transmitter that advertises SSTV. In the unpaired case both are the
        // selection and this is exactly the single snapshot of before.
        var snapshot = new DecodeSnapshot(Satellite, telemetrySource?.Transmitter ?? Transmitter,
          telemetrySource?.Params ?? SignalParams!, ctx.SdrPasses.GetNextPass(Satellite)?.OrbitNumber ?? -1);
        var sstvSnapshot = SstvSnapshot(snapshot);
        CurrentDecode = snapshot;
        Decoder = new(snapshot.SignalParams, telemetry, sstv, fmEngine, detectParams);
        if (Decoder.Pipeline != null)
        {
          Decoder.Pipeline.FrameDecoded += frame => FrameDecodedHandler(frame, snapshot);
          Decoder.Pipeline.BurstDecoded += report => BurstDecodedHandler(report, snapshot);
        }
        // the detection-only branch exists for the search alone: its bursts go to the session and nowhere
        // else — no frames, no tree entry, no status label, because nothing here was decoded.
        if (Decoder.Detector != null) Decoder.Detector.BurstDecoded += OfferToDiscovery;
        if (Decoder.Sstv != null)
        {
          // the image-id → tree-node map lives in the subscription closure, so images from a disposed
          // decoder's flush can never collide with ids of the next decoder's images
          var imageNodes = new Dictionary<int, TreeNode>();
          Decoder.Sstv.ImageUpdated += evt => SstvImageHandler(evt, sstvSnapshot, imageNodes);
          Decoder.Sstv.ImageCompleted += evt => SstvImageHandler(evt, sstvSnapshot, imageNodes);
        }
        if (Decoder.Fm != null)
        {
          // the single "FM Speech" leaf's state lives in this closure, so a disposed decoder's flush lines
          // can never land in the next decoder's transcript node
          var fm = Decoder.Fm;
          var info = new FmTranscriptInfo(snapshot, fm, fm.OutputSampleRate);
          fm.LineCompleted += (line, index) => FmLineCompletedHandler(fm, line, info);
          fm.LineUpdated += line => BeginInvoke(() => UpdateFmPending(line, info));
        }
      }
    }

    private bool IsDecodable()
    {
      return IsTelemetryDecodable() || IsSstvDecodable() || IsFmDecodable();
    }

    // FM voice is decodable to a speech transcript (§10) when the transmitter's mode is FM. Whether the
    // decoder actually runs also needs the downloaded model (see FmModelPresent / EnsureFmEngine).
    private bool IsFmDecodable()
    {
      return SignalParams?.Modulation == Modulation.FM;
    }

    private bool IsTelemetryDecodable()
    {
      return SignalParamsResolver.IsTelemetryDecodable(SignalParams);
    }

    // SSTV needs no framing or baud: the VIS header / sync cadence in the demod domain carries the mode.
    // HasSstv also catches mixed FSK+SSTV transmitters (UmKA-1) that classify as FSK — for
    // those BOTH decoders run concurrently and self-gate on their own signatures.
    private bool IsSstvDecodable()
    {
      if (SignalParams == null) return false;
      return SignalParams.Modulation == Modulation.SSTV || SignalParamsResolver.HasSstv(Transmitter);
    }

    // §2.2: the SSTV branch runs whenever ANY transmitter on this downlink advertises SSTV, whatever is
    // selected and whatever the DB alive flags say — the decoder self-gates on VIS/sync, so an inactive
    // transmitter costs only filter CPU. IsSstvDecodable stays in the union because it also catches a
    // selection whose resolved modulation is SSTV through a layer HasSstv does not read.
    private bool IsSstvBranchWanted()
    {
      if (Terrestrial) return false;
      return IsSstvDecodable() || CoChannel.HasCoChannelSstv(Satellite, Transmitter);
    }

    // The identity SSTV images are attributed to: the co-channel transmitter that advertises SSTV, which may
    // be the selection, a sibling, or (when the branch runs only because the selection resolved to
    // Modulation.SSTV) neither, in which case the telemetry snapshot's identity is reused unchanged.
    private DecodeSnapshot SstvSnapshot(DecodeSnapshot telemetrySnapshot)
    {
      var tx = CoChannel.SstvTransmitter(Satellite, Transmitter);
      if (tx == null || ReferenceEquals(tx, telemetrySnapshot.Transmitter)) return telemetrySnapshot;
      // the same orbit: both branches belong to the one pass this decoder was built for
      return new DecodeSnapshot(Satellite, tx, SignalParamsResolver.Resolve(tx) ?? telemetrySnapshot.SignalParams,
        telemetrySnapshot.Orbit);
    }

    private void BurstDecodedHandler(StreamingBurstReport report, DecodeSnapshot snapshot)
    {
      // hand the burst to a running discovery search (§4.1). Offer returns immediately and drops the burst
      // if the previous one is still under analysis, so this stays free on the decode thread.
      OfferToDiscovery(report);

      BeginInvoke(() =>
        {
          // create the pass entry on the first burst (grayed until a valid frame arrives), not on the first frame
          var (passNode, txPassInfo) = EnsureCurrentPassNode(snapshot);
          txPassInfo.BurstCount++;
          UpdateStatusLabel("DECODING...", Color.Green);
          if (double.IsNaN(txPassInfo.MaxSnrDb) || report.Burst.SnrDb > txPassInfo.MaxSnrDb)
            txPassInfo.MaxSnrDb = report.Burst.SnrDb;
          // refresh the right panel if this pass entry is the one currently selected
          if (treeView1.SelectedNode == passNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));
        }
       );
    }

    private void FrameDecodedHandler(Frame frame, DecodeSnapshot snapshot)
    {
      ctx.KissServer.SendToAll(frame);
      if (snapshot.Satellite?.norad_cat_id is int norad) SatnogsUploader?.Submit(frame, norad);
      BeginInvoke(() =>
      {
        AddFrame(frame, snapshot);
        // frames decoded SINCE a discovered set was applied are the evidence the save decision rests on
        // (§6.1) — no further frames means the answer was probably a coincidence.
        if (FramesSinceDiscovery >= 0 && frame.CrcValid == true)
          ParamsDialog?.ShowFramesSinceApply(++FramesSinceDiscovery);
      });
    }




    //----------------------------------------------------------------------------------------------
    //                                   signal params override
    //----------------------------------------------------------------------------------------------
    // gear button: open the signal-details editor for the current transmitter and apply any manual override
    private void SettingsButton_Click(object sender, EventArgs e)
    {
      if (SignalParams == null)
      {
        MessageBox.Show(this, "Signal parameters are unknown for this transmitter.", "Signal Details",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
      }

      bool discoveryEnabled = ModifierKeys.HasFlag(Keys.Control);

      using var dlg = new SignalParamsDialog();
      dlg.DiscoverToggled += ToggleDiscovery;
      dlg.SaveOverrideRequested += SaveOverrideRequested;
      dlg.DiscoverBtn.Visible = discoveryEnabled;
      dlg.SaveOverrideBtn.Visible = discoveryEnabled;
      ParamsDialog = dlg;
      try
      {
        if (dlg.Open(BuildDialogView(), this) != DialogResult.OK) return;
        ApplyDialogResult(dlg);
      }
      finally
      {
        // a session must never outlive the dialog it reports to (§7 P2: detach cleanly).
        StopDiscovery();
        ParamsDialog = null;
      }
    }



    //----------------------------------------------------------------------------------------------
    //                                   parameter discovery
    //----------------------------------------------------------------------------------------------
    // The Discover button is a toggle. A session searches the bursts that arrive from the press onward, in
    // the background, concurrently with normal decoding, and ends on the first CRC-valid frame, on the
    // second press, or when the dialog closes (§4.6a). Nothing it decodes is ever published: the search
    // runs inside VE3NEA.SkyTlm with no sinks wired to it at all (§4.6).

    private void ToggleDiscovery(bool start)
    {
      StopDiscovery();
      if (!start || SignalParams == null) return;

      // below the horizon a session can never be offered a burst — the pipeline it would listen to is not
      // built until AOS. refuse the press and say why, here in the click, so the line never shows the
      // "waiting" of a search that has not started
      if (!SatAboveHorizon)
      {
        ParamsDialog?.ShowDiscoveryStopped("satellite below horizon");
        return;
      }

      // GENESIS/HADES framing enters the sweep only for that family — the same keyword test the resolver
      // uses, applied to the satellite this transmitter belongs to (§4.3).
      string satName = $"{Satellite?.name} {Transmitter?.description}";
      var options = new DiscoveryOptions
      {
        GenesisFamily = satName.Contains("HADES", StringComparison.OrdinalIgnoreCase)
                        || satName.Contains("GENESIS", StringComparison.OrdinalIgnoreCase)
      };

      BurstsTaken = 0;
      var session = new DiscoverySession(SignalParams, CoChannelParams, options);
      session.Progress += ShowDiscoveryProgress;
      session.Found += DiscoveryFound;
      session.Ended += () => BeginInvoke(() => ParamsDialog?.ShowDiscoveryEnded(FramesSinceDiscovery >= 0));
      Discovery = session;
      // build the search its own burst source if this transmitter's decode does not provide one
      CreatDestroyPipeline();
    }

    private void StopDiscovery()
    {
      var session = Discovery;
      if (session == null) return;
      Discovery = null;
      session.Dispose();
      // drop the detection-only pipeline the session was running on, if it had one
      CreatDestroyPipeline();
    }

    // the satellite has set: end the search and say so. Not a failure — the pass ran out before an answer
    // did — so the line reports the reason rather than "no parameters found". Telling the dialog first
    // makes this message, not the session's Ended notification, the one that stays on screen.
    private void StopDiscoveryAtLos()
    {
      ParamsDialog?.ShowDiscoveryStopped("search stopped: satellite below horizon");
      StopDiscovery();
    }

    /// <summary>
    /// Parameters for the detection-only pipeline a search falls back to. Blind FSK: the deviation is
    /// unknown, which is what sizes the analysis band for the widest plausible signal, and the search itself
    /// measures the real geometry from the samples afterwards. Nothing here is demodulated, so the baud only
    /// sets the width of the detector's matched template — the DB's rate when it has one (the format may be
    /// unsupported while the rate is perfectly well known), otherwise a mid-range default that keeps the
    /// template inside the signals the search can reach at all (200 - 20000 Bd).
    /// </summary>
    private SignalParams DiscoveryDetectorParams()
      => SignalParams! with
      {
        Modulation = Modulation.FSK,
        Baud = SignalParams!.Baud > 0 ? SignalParams.Baud : DefaultDetectBaud,
        Deviation = null,
        AfCarrier = null,
        Manchester = null,
        Framing = Framing.Unknown,
        ResolvedBaud = null,
        ResolvedDeviation = null
      };

    // Offer a burst to a running session and report where the search now stands. A burst the session drops
    // because the previous one is still under analysis is not counted as taken: the analysis it would have
    // started never began, and the skipped count is what tells the operator it happened.
    private void OfferToDiscovery(StreamingBurstReport report)
    {
      // a session that has already found its answer ignores the burst, so counting it as taken would leave
      // the line saying "analyzing" over a search that is over
      if (Discovery is not DiscoverySession session || !session.IsRunning) return;
      if (report.Segment is not { Length: > 0 }) return;   // a detect-only report carries nothing to analyze

      int skipped = session.Snapshot.BurstsSkipped;
      BurstsTaken++;
      session.Offer(report);

      var progress = session.Snapshot;
      if (progress.BurstsSkipped != skipped) BurstsTaken--;
      ShowDiscoveryProgress(progress);
    }

    // the search is analyzing while a burst it took is not yet counted as analyzed, and waiting otherwise
    private void ShowDiscoveryProgress(DiscoveryProgress progress)
      => ParamsDialog?.ShowDiscoveryProgress(progress.BurstsAnalyzed, progress.BurstsSkipped,
        BurstsTaken > progress.BurstsAnalyzed);

    // tier 1 of the hypothesis set: every co-channel transmitter of this satellite, resolved through the
    // production resolver and ignoring the DB's alive/status flags — it marks live transmitters inactive
    // often enough to matter (§4.2). Evaluated per burst, so a transmitter change mid-pass is picked up.
    private IEnumerable<SignalParams> CoChannelParams()
    {
      if (Satellite == null || Transmitter == null) yield break;
      foreach (var tx in Satellite.Transmitters)
      {
        if (ReferenceEquals(tx, Transmitter) || tx.downlink_low != Transmitter.downlink_low) continue;
        if (SignalParamsResolver.Resolve(tx) is { Baud: > 0 } p) yield return p;
      }
    }

    // A hypothesis decoded. Apply it to the current pass immediately and with no confirmation step: the
    // pass itself is the confirmation and it is free, while a wrong set publishes nothing at all — it
    // simply decodes nothing further, leaving the operator exactly where they already were (§4.5).
    private void DiscoveryFound(DiscoveryCandidate found)
    {
      BeginInvoke(() =>
      {
        StopDiscovery();
        // the discovered set replaces the demod fields only; the telemetry-format override and the
        // dialog-only fields the search does not touch (AfCarrier, Manchester) are carried through (§6.5).
        var applied = SignalParams! with
        {
          Modulation = found.Params.Modulation,
          Framing = found.Params.Framing,
          Baud = found.Params.Baud,
          Deviation = found.Params.ResolvedDeviation ?? found.Params.Deviation
        };
        foreach (var f in new[] { "Modulation", "Framing", "Baud", "Deviation" }) UserChangedFields.Add(f);
        // a discovered set has already decoded a frame — that is how the search found it — so it is
        // confirmed on arrival: the gear and the field dots go green, not the yellow of an untested edit.
        DemodValidated = true;
        FramesSinceDiscovery = 0;
        ApplySignalParamsOverride(applied);
        UpdateGearButton();
        UpdateParamsTooltip();
        // the "found" line stays up until a frame decodes with the parameters: reporting zero frames the
        // instant they are applied would overwrite the answer with an accusation before it was tested.
        ParamsDialog?.ShowDiscovered(applied);
      });
    }

    // Save to overrides: write the parameters on screen into transmitters-override.json, keyed by the
    // transmitter uuid and marked read_only so the shipped defaults never clobber them. The only operator
    // action in the flow, and the only step that persists anything beyond the pass (§6.1).
    private void SaveOverrideRequested(object? sender, EventArgs e)
    {
      if (ParamsDialog == null || Transmitter == null) return;
      try
      {
        ctx.SatnogsDb.SaveTransmitterOverride(Transmitter, ParamsDialog.CurrentParams());
        MessageBox.Show(ParamsDialog, "Saved to transmitters-override.json.", "Signal Details",
          MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "saving the transmitter override failed");
        MessageBox.Show(ParamsDialog, "Could not save the override: " + ex.Message, "Signal Details",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
    }

    // package the current params, the available telemetry formats, and the per-field dot state for the dialog
    private SignalParamsView BuildDialogView()
    {
      var formatIds = TelemetryRegistry?.AllDefinitions
        .Select(d => d.Id).Where(id => !string.IsNullOrEmpty(id)).Select(id => id!)
        .Distinct().OrderBy(id => id).ToList() ?? new List<string>();
      string? dbFormatId = ResolveFormat(Satellite?.norad_cat_id, SignalParams!.Framing)?.Id;

      return new SignalParamsView
      {
        Params = SignalParams!,
        DbParams = ResolvedSnapshot ?? SignalParams!,
        FormatIds = formatIds,
        FormatId = FormatOverrideId ?? dbFormatId,
        DbFormatId = dbFormatId,
        ModulationDot = DotFor("Modulation"),
        FramingDot = DotFor("Framing"),
        BaudDot = DotFor("Baud"),
        DeviationDot = DotFor("Deviation"),
        AfCarrierDot = DotFor("AfCarrier"),
        ManchesterDot = DotFor("Manchester"),
        DifferentialDot = DotFor("Differential"),
        FormatDot = FormatOverrideId == null ? SignalParamsDialog.FieldDot.None
          : FormatValidated ? SignalParamsDialog.FieldDot.Confirmed : SignalParamsDialog.FieldDot.Edited
      };
    }

    // dot state for one demod field: a user override (yellow until a frame confirms it, then green) takes
    // precedence over a pipeline finding (green — a finding only exists once a frame locked it).
    private SignalParamsDialog.FieldDot DotFor(string field)
    {
      if (UserChangedFields.Contains(field))
        return DemodValidated ? SignalParamsDialog.FieldDot.Confirmed : SignalParamsDialog.FieldDot.Edited;
      return PipelineFound(field) ? SignalParamsDialog.FieldDot.Confirmed : SignalParamsDialog.FieldDot.None;
    }

    // whether the pipeline discovered this field's value at run time (implies a frame decoded). Baud/Deviation
    // carry an explicit ResolvedBaud/ResolvedDeviation; Differential is overwritten in place, so it is detected
    // by comparison against the pristine DB-resolved snapshot.
    private bool PipelineFound(string field) => field switch
    {
      "Baud" => SignalParams?.ResolvedBaud != null,
      "Deviation" => SignalParams?.ResolvedDeviation != null,
      "Differential" => SignalParams?.Differential != ResolvedSnapshot?.Differential,
      _ => false
    };

    // apply the dialog result: the telemetry-format override takes a lightweight path (no pipeline rebuild,
    // future frames only); any demod-field change replaces the params and rebuilds the pipeline.
    private void ApplyDialogResult(SignalParamsDialog dlg)
    {
      if (dlg.ChangedFields.Contains("TelemetryFormat"))
      {
        FormatOverrideId = dlg.ResultFormatId;
        FormatOverride = TelemetryRegistry?.ById(FormatOverrideId);
        FormatValidated = false;
      }
      else if (dlg.ResetFields.Contains("TelemetryFormat"))
      {
        // reset back to NORAD resolution — drop the manual format override
        FormatOverride = null;
        FormatOverrideId = null;
        FormatValidated = false;
      }

      bool demodChanged = dlg.ChangedFields.Concat(dlg.ResetFields).Any(f => f != "TelemetryFormat");
      if (demodChanged)
      {
        foreach (var f in dlg.ChangedFields) if (f != "TelemetryFormat") UserChangedFields.Add(f);
        foreach (var f in dlg.ResetFields) if (f != "TelemetryFormat") UserChangedFields.Remove(f);
        // parameters the search found are applied as confirmed; a hand edit is a hypothesis until a frame
        // decodes with it.
        DemodValidated = dlg.DiscoveredApplied;
        ApplySignalParamsOverride(dlg.Result);
      }

      UpdateGearButton();
      UpdateParamsTooltip();
    }

    // adopt the user-edited params and rebuild the pipeline so they take effect immediately. CreatDestroyPipeline
    // keeps the existing decoder while the transmitter is unchanged, so the decoder is torn down explicitly here
    // to force a fresh one built from the overridden params.
    private void ApplySignalParamsOverride(SignalParams newParams)
    {
      SignalParams = newParams;

      if (Decoder != null)
      {
        Decoder.Purge();
        var old = Decoder;
        Decoder = null;
        CurrentDecode = null;
        old.Dispose();
      }
      CreatDestroyPipeline();
    }

    // gear glyph color mirrors the dots: neutral with nothing to show, orange while a user override is pending a
    // confirming frame, green once every pending override has produced one, or when the pipeline discovered a value
    // at run time (a locked baud/deviation, a resolved precoding) — which is itself a confirmed finding.
    private void UpdateGearButton()
    {
      bool userChange = UserChangedFields.Count > 0 || FormatOverrideId != null;
      bool pipelineFound = PipelineFound("Baud") || PipelineFound("Deviation") || PipelineFound("Differential");
      if (!userChange && !pipelineFound) { SettingsButton.ForeColor = Color.Gray; return; }

      bool demodOk = UserChangedFields.Count == 0 || DemodValidated;
      bool formatOk = FormatOverrideId == null || FormatValidated;
      SettingsButton.ForeColor = demodOk && formatOk
        ? SignalParamsDialog.ConfirmedColor : SignalParamsDialog.EditedColor;
    }




    //----------------------------------------------------------------------------------------------
    //                                       treeview
    //----------------------------------------------------------------------------------------------
    private void AddFrame(Frame frame, DecodeSnapshot snapshot)
    {
      var (passNode, txPassInfo) = EnsureCurrentPassNode(snapshot);

      // un-gray the pass entry once the first valid frame of the pass is decoded
      if (!txPassInfo.HasValidFrame)
      {
        txPassInfo.HasValidFrame = true;
        passNode.ForeColor = Color.Empty;
      }

      // a frame decoded with the current (overridden) pipeline confirms the demod override worked. Gated on the
      // current decoder's snapshot so a late frame from a pre-override pipeline (or a different transmitter)
      // can't confirm it. Turns the user-changed demod dots and the gear button green.
      if (ReferenceEquals(snapshot, CurrentDecode) && UserChangedFields.Count > 0 && !DemodValidated)
      {
        DemodValidated = true;
        UpdateGearButton();
      }

      var (addr, addrLen) = ExtractAddress(frame, snapshot);
      string nodeText = $"{DateTime.Now:HH:mm:ss}  {frame.Length} bytes  {addr}";
      var frameNode = new TreeNode(nodeText);
      string frameText = BuildFrameText(frame, snapshot, addr, addrLen);
      frameNode.Tag = frameText;
      txPassInfo.FrameCount++;

      SaveFrameToFile(frame, addr, frameText, snapshot);

      // the pipeline may have locked a blind FSK burst's actual deviation/baud while decoding this frame — refresh
      // the tooltip (so it shows the values actually used) and the gear (which turns green on such a finding). only
      // for the currently selected transmitter's decoder, so a late frame from a previous transmitter can't overwrite it.
      if (ReferenceEquals(snapshot, CurrentDecode))
      {
        UpdateParamsTooltip();
        UpdateGearButton();
      }

      AddLeaf(passNode, frameNode);
      if (treeView1.SelectedNode == passNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));
    }

    /// <summary>Returns the pass node this snapshot's content belongs to, and its info, creating the node
    /// when this is the first burst or frame of a new transmitter+orbit pass. New telemetry/SSTV pass nodes
    /// are grayed until their first valid frame/image; the FM speech path passes
    /// <paramref name="grayUntilContent"/> false because its node is only ever created once there is decoded
    /// content (§10.3, operator: never grayed). Callers must add their leaf under the node returned here
    /// rather than under the most recently touched one: with a co-channel pair they are not the same.</summary>
    private (TreeNode Node, TxPassInfo Info) EnsureCurrentPassNode(DecodeSnapshot snapshot, bool grayUntilContent = true)
    {
      int orbit = snapshot.Orbit;

      // A co-channel pair interleaves telemetry frames and SSTV images from TWO transmitters, so the match
      // runs over the last AND second-last top-level nodes instead of the current one alone — otherwise
      // every alternation between them spawns a node. Two is exactly enough for a pair, and an A -> B -> A
      // selection change still starts a fresh node the way it does today.
      int count = treeView1.Nodes.Count;
      for (int i = count - 1; i >= Math.Max(0, count - 2); i--)
      {
        var node = treeView1.Nodes[i];
        if (node.Tag is TxPassInfo info && info.IsSame(snapshot.Transmitter, orbit)) return (node, info);
      }

      var passNode = new TreeNode($"{DateTime.Now:yyyy-MM-dd HH:mm} {snapshot.Transmitter.Satellite.name}  {snapshot.Transmitter.description}");
      if (grayUntilContent) passNode.ForeColor = Color.Gray;
      var txPassInfo = new TxPassInfo(snapshot.Transmitter, orbit);
      txPassInfo.SignalParams = snapshot.SignalParams;
      passNode.Tag = txPassInfo;
      treeView1.Nodes.Add(passNode);
      TrackNewNode(passNode);

      return (passNode, txPassInfo);
    }

    /// <summary>Selects the newly added node (pass or leaf) if the tree selection was tracking the previously
    /// current node, or nothing was selected at all; otherwise leaves the user's selection alone. WinForms
    /// scrolls a newly selected node into view automatically.</summary>
    private void TrackNewNode(TreeNode newNode)
    {
      bool mustSelect = treeView1.SelectedNode == null || treeView1.SelectedNode == Current;
      Current = newNode;
      if (mustSelect) treeView1.SelectedNode = newNode;
    }

    /// <summary>Adds a leaf under the given pass node. Expands the pass node so the new leaf is visible, unless
    /// the user deliberately collapsed it while it already had leaves and is still looking at its summary —
    /// popping it open on every new frame/image would fight that choice.</summary>
    private void AddLeaf(TreeNode passNode, TreeNode leaf)
    {
      bool keepCollapsed = !passNode.IsExpanded && passNode.Nodes.Count > 0 && treeView1.SelectedNode == passNode;
      passNode.Nodes.Add(leaf);
      if (!keepCollapsed) passNode.Expand();
      TrackNewNode(leaf);
    }

    // the header source/destination address and the byte length of the address field, so the caller can label the
    // frame and drop those bytes from the ASCII/HEX payload views. AX.25 G3RUH frames, and USP frames (which
    // encapsulate an AX.25 UI frame), both begin with an AX.25 callsign address field. ("", 0) when none parses.
    // GEOSCAN is included because its beacon frames also encapsulate an AX.25 UI frame at offset 0 (e.g.
    // "RS92S5 -> BEACON"); its other flavor is raw Geoscan telemetry, which Describe rejects on its own since
    // the first byte does not shift into a plausible callsign character.
    private static (string Addr, int AddrLen) ExtractAddress(Frame frame, DecodeSnapshot snapshot)
    {
      switch (snapshot.SignalParams.Framing)
      {
        case Framing.AX25G3RUH:
        case Framing.USP:
        case Framing.GEOSCAN:
          string? addr = Ax25Address.Describe(frame.Bytes);
          return string.IsNullOrEmpty(addr) ? ("", 0) : (addr, Ax25Address.AddressFieldLength(frame.Bytes));

        default:
          return ("", 0);
      }
    }

    private string BuildFrameText(Frame frame, DecodeSnapshot snapshot, string addr, int addrLen)
    {
      // telemetry section: the extracted address (when any) followed by the parsed telemetry fields (when a
      // format matches). Only emitted when there is something to show.
      string fields = "";
      var def = FormatFor(snapshot);
      if (def != null)
      {
        var record = TelemetryParser.Parse(def, frame.Bytes);
        if (record != null)
        {
          fields = string.Join("", record.Fields.Select(f => $"  {f.Name}: {f.Value}{(f.Units.Length > 0 ? " " + f.Units : "")}\n"));
          // a frame parsing into fields with the manual format override confirms it — turn its dot/gear green
          if (ReferenceEquals(def, FormatOverride) && record.Fields.Count > 0 && !FormatValidated)
          {
            FormatValidated = true;
            UpdateGearButton();
          }
        }
      }

      string tlm = "";
      if (addr.Length > 0 || fields.Length > 0)
      {
        tlm = "PAYLOAD:\n";
        if (addr.Length > 0) tlm += $"  Address: {addr}\n";
        tlm += fields + "\n";
      }

      // ASCII and HEX are shown over the payload only — any header address bytes are removed and the HEX
      // offsets renumbered from 0
      var payload = addrLen > 0 ? frame.Bytes.Skip(addrLen).ToArray() : frame.Bytes;

      string chars = new string(payload.Select(b => b >= 0x20 && b < 0x7f ? (char)b : '.').ToArray());
      string asc = "";
      for (int i = 0; i < chars.Length; i += 28)
        asc += "  " + chars.Substring(i, Math.Min(28, chars.Length - i)) + "\n";
      asc = "ASCII:\n" + asc + "\n";

      string hex = "";
      for (int i = 0; i < payload.Length; i += 8)
        hex += $"  {i:X3}  " + string.Join(" ", payload.Skip(i).Take(8).Select(b => b.ToString("X2"))) + "\n";
      hex = "HEX:\n" + hex + "\n";

      string meta = "META:\n" +
        $"  Bytes: {frame.Length}\n" +
        $"  CFO: {frame.CfoHz:F1} Hz\n" +
        $"  SNR: {frame.SnrDb:F1} dB\n" +
        $"  CRC: {frame.CrcValid switch { true => "OK", false => "FAIL", null => "n/a" }}\n" +
        $"  Corrections: {frame.CorrectedBits}\n" +
        $"  Erasures: {frame.ErasedBytes}\n\n" +
        DescribeSignalParamsMeta(snapshot);

      return tlm + asc + hex + meta;
    }

    // the telemetry format for a frame: the manual override (only for the current transmitter's decoder), else
    // the format resolved from the frame's satellite and framing. A late frame from a previous transmitter
    // falls back to that same resolution.
    private TelemetryDefinition? FormatFor(DecodeSnapshot snapshot)
    {
      if (FormatOverride != null && ReferenceEquals(snapshot, CurrentDecode)) return FormatOverride;
      return ResolveFormat(snapshot.Satellite?.norad_cat_id, snapshot.SignalParams.Framing);
    }

    // id of the shared Sputnix USP telemetry definition (usp.json), applied to any USP-framed signal.
    private const string UspFormatId = "usp";

    // resolve the telemetry definition for a satellite: an explicit NORAD mapping wins; otherwise a signal
    // decoded with USP framing gets the shared USP definition, since USP framing carries USP telemetry even
    // for satellites not listed in usp.json.
    private TelemetryDefinition? ResolveFormat(int? noradId, Framing framing)
    {
      return TelemetryRegistry?.ForNorad(noradId)
        ?? (framing == Framing.USP ? TelemetryRegistry?.ById(UspFormatId) : null);
    }




    //----------------------------------------------------------------------------------------------
    //                                      sstv images
    //----------------------------------------------------------------------------------------------
    // called on the decode worker thread (or on the UI thread when a disposed decoder flushes); the
    // finalized image is auto-saved here, before marshaling, so a pass ending with the panel closing
    // cannot lose it
    private void SstvImageHandler(SstvImageEvent evt, DecodeSnapshot snapshot, Dictionary<int, TreeNode> imageNodes)
    {
      string? savedPath = evt.Final && evt.ValidRows > 0 ? SaveImageToFile(evt, snapshot) : null;
      BeginInvoke(() => ShowImage(evt, snapshot, imageNodes, savedPath));
    }

    private void ShowImage(SstvImageEvent evt, DecodeSnapshot snapshot, Dictionary<int, TreeNode> imageNodes, string? savedPath)
    {
      var (passNode, txPassInfo) = EnsureCurrentPassNode(snapshot);

      bool isNew = !imageNodes.TryGetValue(evt.ImageId, out TreeNode? node);
      if (isNew)
      {
        node = new TreeNode();
        node.Tag = new SstvImageInfo(snapshot, evt);
        imageNodes[evt.ImageId] = node;
        txPassInfo.ImageCount++;
        AddLeaf(passNode, node);
      }

      // swap in the new reconstruction; dispose the previous bitmap only after the PictureBox lets go of it
      var info = (SstvImageInfo)node!.Tag;
      var oldBitmap = info.Bitmap;
      info.Event = evt;
      info.Bitmap = evt.Image.ToBitmap();
      if (savedPath != null) info.SavedPath = savedPath;
      node.Text = $"{info.FirstSeen:HH:mm:ss}  {evt.Mode}  {evt.ValidRows}/{evt.Image.Height} rows";
      if (ImageBox.Image == oldBitmap) ImageBox.Image = info.Bitmap;
      oldBitmap?.Dispose();

      // an accepted image train is real content: un-gray the pass entry the way a valid frame does
      if (!txPassInfo.HasValidFrame)
      {
        txPassInfo.HasValidFrame = true;
        passNode.ForeColor = Color.Empty;
      }

      if (!evt.Final) UpdateStatusLabel("DECODING...", Color.Green);

      if (treeView1.SelectedNode == node) DisplayImageInfo(info);
      else if (treeView1.SelectedNode == passNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));

      // last, so the tree and the picture are fully drawn before the modal report dialog can appear
      CheckSendAmsatReport(snapshot, evt);
    }

    // ask once per pass, keyed the way LoggerInterface.CheckSendAmsatStatus keys its own prompt
    private (string SatName, int Orbit) LastAmsatReport = ("", 0);

    /// <summary>The first SSTV image of a pass is a confirmed reception, so offer the AMSAT status report the
    /// same way a logged QSO does (<c>LoggerInterface.CheckSendAmsatStatus</c>), with the satellite's SSTV
    /// entry preselected. Fires on the first decoded rows, NOT on image completion: an image runs the length
    /// of the transmission (~36 s for Robot 36), and waiting for it put the dialog half a minute behind the
    /// reception it is reporting. One decoded row is already the app's own test for real content — it is
    /// what un-grays the pass node — so the picture goes on rendering behind the dialog.</summary>
    private void CheckSendAmsatReport(DecodeSnapshot snapshot, SstvImageEvent evt)
    {
      if (evt.ValidRows == 0) return;

      var sat = snapshot.Satellite;
      if (sat == null || sat.AmsatEntries.Count == 0) return;
      // an unprompted dialog with no callsign to send the report under would only be a dead end
      if (string.IsNullOrEmpty(ctx.Settings.User.Call)) return;

      // the snapshot's orbit, not a fresh GetNextPass: that rolls to the next pass the moment this one ends,
      // which would make the decoder's own flush look like a new pass and raise a second dialog at LOS
      var info = (sat.name, snapshot.Orbit);
      if (info == LastAmsatReport) return;
      // set the guard BEFORE showing the dialog: its message loop keeps pumping, so the image events that
      // arrive while it is open re-enter here and would each open another one
      LastAmsatReport = info;

      AmsatReportDialog.SendReport(ctx, sat, "SSTV");
    }

    private void DisplayImageInfo(SstvImageInfo info)
    {
      if (richTextBox1.Parent != ImageSplitContainer.Panel2)
      {
        richTextBox1.Parent = ImageSplitContainer.Panel2;
        richTextBox1.Dock = DockStyle.Fill;
      }
      ImageSplitContainer.Visible = true;
      ImageBox.Image = info.Bitmap;
      richTextBox1.Text = info.Describe();
    }

    // switches the right panel back to plain telemetry text, undoing DisplayImageInfo's reparenting of
    // richTextBox1 into the (now hidden) ImageSplitContainer
    private void ShowTelemetryText()
    {
      if (richTextBox1.Parent != splitContainer1.Panel2)
      {
        richTextBox1.Parent = splitContainer1.Panel2;
        richTextBox1.Dock = DockStyle.Fill;
      }
      ImageSplitContainer.Visible = false;
    }

    /// <summary>Auto-save the finalized image as PNG + JSON metadata sidecar under the user data folder.</summary>
    private static string? SaveImageToFile(SstvImageEvent evt, DecodeSnapshot snapshot)
    {
      try
      {
        string folder = Path.Combine(Utils.GetUserDataFolder(), "SstvImages");
        string sat = string.Concat((snapshot.Satellite?.name ?? "Unknown").Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{sat}_{evt.Mode}_{evt.ImageId}.png");
        evt.Image.SavePng(path);

        var meta = new
        {
          Utc = DateTime.UtcNow,
          Satellite = snapshot.Satellite?.name,
          Norad = snapshot.Satellite?.norad_cat_id,
          Transmitter = snapshot.Transmitter.description,
          TransmitterUuid = snapshot.Transmitter.uuid,
          Mode = evt.Mode.ToString(),
          evt.FromVis,
          evt.ValidRows,
          evt.Image.Width,
          evt.Image.Height
        };
        File.WriteAllText(Path.ChangeExtension(path, ".json"), JsonConvert.SerializeObject(meta, Formatting.Indented));
        return path;
      }
      catch (Exception e)
      {
        Log.Error(e, "Failed to save SSTV image");
        return null;
      }
    }

    private void SaveImageMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is not SstvImageInfo info) return;
      using var dlg = new SaveFileDialog
      {
        Filter = "PNG Image|*.png",
        FileName = $"{info.FirstSeen:yyyyMMdd_HHmmss}_{info.Event.Mode}.png"
      };
      if (dlg.ShowDialog() == DialogResult.OK) info.Event.Image.SavePng(dlg.FileName);
    }

    private void CopyImageMNU_Click(object sender, EventArgs e)
    {
      if (ImageBox.Image != null) Clipboard.SetImage(ImageBox.Image);
    }

    // gray the "Open in Viewer" item until the selected image has been auto-saved to a file on disk
    private void ImageMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      var info = treeView1.SelectedNode?.Tag as SstvImageInfo;
      OpenImageMNU.Enabled = info?.SavedPath != null && File.Exists(info.SavedPath);
    }

    private void OpenImageMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is not SstvImageInfo info || info.SavedPath == null) return;
      try
      {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(info.SavedPath) { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to open SSTV image in viewer");
      }
    }




    //----------------------------------------------------------------------------------------------
    //                                      fm speech
    //----------------------------------------------------------------------------------------------
    // the SkyFM DLLs and sherpa model-pack files, manually unzipped by the user into the installation folder
    // (they are no longer part of the SkyRoof installer - see install\SkyRoof.iss)
    internal static string FmModelDir => Path.Combine(Application.StartupPath, "ASR_models");

    // whether the FM speech model has been unzipped into place (all pack files present). Also false, rather
    // than throwing, when VE3NEA.SkyFM.dll itself is missing (the whole FM artefact was never installed).
    private static bool FmModelPresent
    {
      get
      {
        try { return SherpaModelPack.IsPresent(FmModelDir); }
        catch { return false; }
      }
    }

    // lazily load the single shared FM speech engine (~71 MB model, ~1.5 s the first time). Null when the
    // FM artefact (SkyFM DLLs and/or model files) was not unzipped into the installation folder, or fails to
    // load. Shared across transmitter changes so it loads at most once per session; disposed on panel close.
    private SherpaOnnxEngine? EnsureFmEngine()
    {
      if (FmSpeechEngine != null) return FmSpeechEngine;
      try
      {
        if (!FmModelPresent) return null;
        SherpaOnnxEngine.ModelDirectory = FmModelDir;
        FmSpeechEngine = SherpaOnnxEngine.Hotwords(int8: true, modelDir: FmModelDir);
        Log.Information("Loaded FM speech model from {Dir}", FmModelDir);
      }
      catch (Exception e)
      {
        Log.Error(e, "Failed to load FM speech model");
        FmSpeechEngine = null;
      }
      return FmSpeechEngine;
    }

    // a transcript line closed (decode worker thread): capture its 16 kHz audio now, while the decoder still
    // holds the pass voice (§10.4 click-to-play), then marshal the completed line to the UI
    private void FmLineCompletedHandler(SkySpeechDecoder fm, FmTranscriptLine line, FmTranscriptInfo info)
    {
      float[] audio = fm.GetAudio(line.StartSeconds, line.EndSeconds);
      var entry = new FmLineEntry(line.Text, line.StartSeconds, audio);
      BeginInvoke(() => AddFmLine(entry, info));
    }

    private void AddFmLine(FmLineEntry entry, FmTranscriptInfo info)
    {
      EnsureFmLeaf(info);
      info.Lines.Add(entry);
      info.Pending = null;   // the open line just closed into this completed entry
      if (treeView1.SelectedNode == info.Node) RenderFmTranscript(info);
    }

    private void UpdateFmPending(FmTranscriptLine pending, FmTranscriptInfo info)
    {
      EnsureFmLeaf(info);
      info.Pending = pending;
      if (treeView1.SelectedNode == info.Node) RenderFmTranscript(info);
    }

    // create the pass node and the single "FM Speech" leaf on the first decoded content (§10.3). The FM pass
    // node is never grayed (operator decision) — it exists only once there is content
    private void EnsureFmLeaf(FmTranscriptInfo info)
    {
      var (passNode, txPassInfo) = EnsureCurrentPassNode(info.Snapshot, grayUntilContent: false);
      if (info.Node == null)
      {
        info.Node = new TreeNode("FM Speech") { Tag = info };
        AddLeaf(passNode, info.Node);
      }
      txPassInfo.HasValidFrame = true;
      UpdateStatusLabel("DECODING...", Color.Green);
    }

    // render the transcript into richTextBox1: one "MM:SS  text" line per decoded line, plus the in-progress
    // open line; record each line's character range for click-to-play hit-testing, completed and pending alike
    private void RenderFmTranscript(FmTranscriptInfo info)
    {
      ShowTelemetryText();
      CurrentFmTranscript = info;
      info.Spans.Clear();
      info.PendingSpan = null;
      var sb = new System.Text.StringBuilder();
      foreach (var entry in info.Lines)
      {
        string prefix = $"{FormatMmss(entry.StartSeconds)}  ";
        int textStart = sb.Length + prefix.Length;
        sb.Append(prefix).Append(entry.Text).Append('\n');
        info.Spans.Add((textStart, textStart + entry.Text.Length, entry));
      }
      if (info.Pending != null)
      {
        string prefix = $"{FormatMmss(info.Pending.StartSeconds)}  ";
        int textStart = sb.Length + prefix.Length;
        sb.Append(prefix).Append(info.Pending.Text).Append('\n');
        info.PendingSpan = (textStart, textStart + info.Pending.Text.Length);
      }
      richTextBox1.Text = sb.ToString();
    }

    // seconds since decode start (≈ AOS) as MM:SS — the first column of each transcript line (§10.3)
    private static string FormatMmss(double seconds)
    {
      int t = (int)seconds;
      return $"{t / 60:00}:{t % 60:00}";
    }


    // ----- click-to-play (§10.4) -----
    // the completed line whose text spans the mouse position, or Pending=true when it's over the in-progress
    // open line instead (both are clickable; the timestamp column and gaps are not)
    private (FmLineEntry? Completed, bool Pending) FmLineAt(Point location)
    {
      if (CurrentFmTranscript == null) return (null, false);
      int i = richTextBox1.GetCharIndexFromPosition(location);

      // GetCharIndexFromPosition clamps a click in the empty area below the transcript to the last
      // character, so a raw index match alone can't tell a genuine click from that snap. Ask the control for
      // the resolved index's own row instead of trusting Font.Height to equal the real row pitch (it doesn't
      // always, e.g. under DPI/font-metric rounding) — if the click isn't actually within that row, it landed
      // below (or above) all text
      Point resolvedPos = richTextBox1.GetPositionFromCharIndex(i);
      if (location.Y < resolvedPos.Y || location.Y >= resolvedPos.Y + richTextBox1.Font.Height) return (null, false);

      // end is the index of the line's trailing newline; include it (i <= end) so a click low on or to the
      // right of a line — which snaps to that newline — still hits the line
      foreach (var (start, end, line) in CurrentFmTranscript.Spans)
        if (i >= start && i <= end) return (line, false);

      if (CurrentFmTranscript.PendingSpan is { } pendingSpan && i >= pendingSpan.Start && i <= pendingSpan.End)
        return (null, true);

      return (null, false);
    }

    private void richTextBox1_MouseMove(object sender, MouseEventArgs e)
    {
      var (completed, pending) = FmLineAt(e.Location);
      richTextBox1.Cursor = completed != null || pending ? Cursors.Hand : Cursors.Default;
    }

    private void richTextBox1_MouseClick(object sender, MouseEventArgs e)
    {
      var (completed, pending) = FmLineAt(e.Location);
      if (completed != null)
      {
        if (completed.Audio.Length > 0) PlayFmClip(completed.Audio, CurrentFmTranscript!.SampleRate);
      }
      else if (pending && CurrentFmTranscript!.Pending is { } pendingLine)
      {
        // the open line isn't captured into an FmLineEntry until it closes, so fetch its audio-so-far
        // on demand from the retained decoder rather than pre-capturing it on every LineUpdated tick
        var audio = CurrentFmTranscript.Engine.GetAudio(pendingLine.StartSeconds, pendingLine.EndSeconds);
        if (audio.Length > 0) PlayFmClip(audio, CurrentFmTranscript.SampleRate);
      }
    }

    // play one transcript line's audio fragment through the same soundcard (device and gain) used for slicer
    // output playback, replacing any clip already playing
    private void PlayFmClip(float[] audio, int sampleRate)
    {
      try
      {
        var resampled = sampleRate == SdrConst.AUDIO_SAMPLING_RATE ? audio : ResampleFmClip(audio, sampleRate);
        StopFmClip();

        // mirror RecordingManager.StartPlayback: temporarily enable the shared speaker soundcard for the
        // duration of the clip if the user has speaker output turned off
        if (!ctx.SpeakerSoundcard.Enabled)
        {
          ctx.SpeakerSoundcard.Enabled = true;
          SpeakerEnabledForFmClip = true;
        }

        ctx.SpeakerSoundcard.AddSamples(resampled);

        if (SpeakerEnabledForFmClip)
        {
          int clipMs = (int)(1000.0 * resampled.Length / SdrConst.AUDIO_SAMPLING_RATE) + 300;
          FmClipEndTimer = new System.Windows.Forms.Timer { Interval = Math.Max(clipMs, 1) };
          FmClipEndTimer.Tick += FmClipEndTimer_Tick;
          FmClipEndTimer.Start();
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex, "FM clip playback failed");
      }
    }

    private static float[] ResampleFmClip(float[] audio, int sampleRate)
    {
      var bytes = new byte[audio.Length * sizeof(float)];
      Buffer.BlockCopy(audio, 0, bytes, 0, bytes.Length);
      var wf = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
      using var stream = new RawSourceWaveStream(new MemoryStream(bytes), wf);
      var source = new WdlResamplingSampleProvider(stream.ToSampleProvider(), SdrConst.AUDIO_SAMPLING_RATE);

      var resampled = new List<float>(audio.Length * SdrConst.AUDIO_SAMPLING_RATE / sampleRate);
      var buf = new float[4096];
      int count;
      while ((count = source.Read(buf, 0, buf.Length)) > 0)
        resampled.AddRange(buf.Take(count));
      return resampled.ToArray();
    }

    private void FmClipEndTimer_Tick(object? sender, EventArgs e)
    {
      StopFmClip();
    }

    private void StopFmClip()
    {
      if (FmClipEndTimer != null)
      {
        FmClipEndTimer.Stop();
        FmClipEndTimer.Tick -= FmClipEndTimer_Tick;
        FmClipEndTimer.Dispose();
        FmClipEndTimer = null;
      }

      ctx.SpeakerSoundcard.Buffer.Clear();

      if (SpeakerEnabledForFmClip)
      {
        ctx.SpeakerSoundcard.Enabled = false;
        SpeakerEnabledForFmClip = false;
      }
    }




    //----------------------------------------------------------------------------------------------
    //                                       save to file
    //----------------------------------------------------------------------------------------------
    // mirror everything shown in the tree to the file: the date and time, satellite, transmitter
    // and frame length in the header, the frame address (if any), then the frame detail shown in the
    // right pane
    private void SaveFrameToFile(Frame frame, string addr, string frameText, DecodeSnapshot snapshot)
    {
      if (!ctx.Settings.Telemetry.ArchiveToFile) return;

      FrameLogger ??= CreateFrameLogger();

      string header = $"Sat: {snapshot.Transmitter.Satellite.name}  Tx: \"{snapshot.Transmitter.description}\"  Uuid: {snapshot.Transmitter.uuid}  Frame: {frame.Length} bytes" +
        (addr.Length > 0 ? $"  Addr: {addr}" : "");
      FrameLogger.Information("{Header}\n{Body}", header, frameText);
    }

    private static ILogger CreateFrameLogger()
    {
      string fileName = Path.Combine(Utils.GetUserDataFolder(), "TelemetryDecodes", "frames_.txt");
      return new LoggerConfiguration()
        .WriteTo.File(fileName,
          rollingInterval: RollingInterval.Day,
          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss}  {Message:lj}{NewLine}",
          shared: true)
        .CreateLogger();
    }

    internal void UpdateTxStatus()
    {
      SatAboveHorizon = ctx.SdrPasses.GetNextPass(Satellite)?.IsAboveHorizon() ?? false;
      // LOS ends a running search (§4.6a): no further burst can arrive, so the session must not be left
      // running with the progress line sitting on "waiting" until the operator closes the dialog
      if (!SatAboveHorizon && Discovery != null) StopDiscoveryAtLos();
      CreatDestroyPipeline();

      // §4: no parameter editing, no Discover and no Save-to-override for a transmitter the user did not
      // select, so the gear disappears while a sibling drives telemetry. To correct a wrong rank pick the
      // operator selects that telemetry transmitter, which re-adds the SSTV branch anyway (§2.2 row 1).
      SettingsButton.Visible = Sibling == null;

      if (Terrestrial) UpdateStatusLabel("terrestrial, not decoded", Color.Red);
      else if (!IsDecodable()) UpdateStatusLabel(DescribeUnsupported(), Color.Red);
      // an FM-only transmitter with no FM artefact unzipped into the installation folder reads as
      // unsupported, silently - the user installs it manually, there is no in-app prompt or download
      else if (IsFmDecodable() && !IsTelemetryDecodable() && !IsSstvDecodable() && !FmModelPresent)
        UpdateStatusLabel(DescribeUnsupported(), Color.Red);
      else if (!SatAboveHorizon) UpdateStatusLabel("satellite below horizon", SystemColors.ControlText);
      else UpdateStatusLabel($"ready to decode{DescribeBranches()}", SystemColors.ControlText);
    }

    // Name what is actually unsupported about the SELECTED transmitter instead of blaming the telemetry
    // format for everything: a CW transmitter has no telemetry format to fail on, and an FM one usually only
    // lacks its speech model. The verdict covers the selection alone — a co-channel SSTV image, or an FM
    // transcript, may well be decoding while one of these is on the label.
    private string DescribeUnsupported()
    {
      return SignalParams?.Modulation switch
      {
        Modulation.CW => "CW decoding not supported",
        // reached only via the ladder's FM branch, i.e. the modulation is supported but the model is absent
        Modulation.FM => "FM decoding not supported",
        null => "signal parameters unknown",
        _ => "telemetry format not supported"
      };
    }

    // The branches that will run, named after the ladder has decided the selection is decodable at all —
    // "ready to decode: SSTV + GMSK 9k6 (USP)". Worth naming because with pairing the running branches are
    // no longer implied by the selected transmitter's own description.
    private string DescribeBranches()
    {
      var names = new List<string>();
      if (IsSstvBranchWanted()) names.Add("SSTV");
      if (TelemetrySource is { } source) names.Add(DescribeBranchParams(source.Params));
      if (IsFmDecodable() && FmModelPresent) names.Add("FM speech");
      return names.Count == 0 ? "" : ": " + string.Join(" + ", names);
    }

    // a telemetry branch in the DB's own spelling: "GMSK 9k6 (USP)"
    private static string DescribeBranchParams(SignalParams p)
    {
      double baud = p.ResolvedBaud ?? p.Baud;
      return $"{p.Modulation} {FormatBaudToken(baud)} ({p.Framing})";
    }

    // 9600 -> "9k6", 19200 -> "19k2", 1200 -> "1k2", 9600000 -> "9600k", 300 -> "300". Invariant on purpose:
    // the 'k' replaces the decimal point, so a culture that writes a comma must not leave one behind.
    private static string FormatBaudToken(double baud)
    {
      if (baud < 1000) return FormatTlmNumber(baud);
      string text = (baud / 1000).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "k");
      return text.Contains('k') ? text : text + "k";
    }

    private void UpdateStatusLabel(string text, Color color)
    {
      StatusLabel.Text = text;
      StatusLabel.ForeColor = color;
    }

    internal void ProcessSamples(DataEventArgs<Complex32> e)
    {
      Decoder?.StartProcessing(e);
    }

    private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
    {
      var node = e.Node;
      if (node == null) return;

      // showing non-FM content clears the click-to-play routing; RenderFmTranscript re-arms it for an FM node
      CurrentFmTranscript = null;

      if (node.Tag is SstvImageInfo imageInfo)
      {
        DisplayImageInfo(imageInfo);
        return;
      }

      if (node.Tag is FmTranscriptInfo fmInfo)
      {
        RenderFmTranscript(fmInfo);
        return;
      }

      ShowTelemetryText();
      if (node.Level == 0)
      {
        var info = node.Tag as TxPassInfo;
        richTextBox1.Text = info!.Describe(DescribeSignalParamsOrUnknown(info.SignalParams));
      }
      else
        richTextBox1.Text = (string)node!.Tag!;
    }

    private void ClearAllMNU_Click(object sender, EventArgs e)
    {
      Current = null;
      ShowTelemetryText();
      ImageBox.Image = null;
      richTextBox1.Clear();
      treeView1.Nodes.Clear();
    }
  }
}
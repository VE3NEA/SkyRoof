using FontAwesome;
using MathNet.Numerics;
using Newtonsoft.Json;
using Serilog;
using SkyRoof.Satellites;
using VE3NEA;
using VE3NEA.SkySSTV;
using VE3NEA.SkyTlm.Core;
using VE3NEA.SkyTlm.Deframing;
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
    private TelemetryDecocder? Decoder;
    private SatnogsUploader? SatnogsUploader;
    private TelemetryRegistry? TelemetryRegistry;
    // the most recently added tree node: either the pass node itself (before it has any leaves) or its
    // last-added leaf. the pass node a frame/image belongs to is always this node's parent, or the node
    // itself when it has no leaves yet — so a single field tracks both "current pass" and "last leaf"
    private TreeNode? Current;
    private TreeNode? CurrentPassNode => Current?.Parent ?? Current;
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

    // Identity of the transmitter a decoder was built for, captured when the pipeline is created and bound to
    // that pipeline's event handlers. Frames surface on the decode worker thread, possibly after the user has
    // switched to a different transmitter; carrying the snapshot with the frame keeps it attributed to the
    // transmitter that actually produced it instead of to whatever is selected when the frame arrives.
    private sealed class DecodeSnapshot
    {
      internal readonly SatnogsDbSatellite? Satellite;
      internal readonly SatnogsDbTransmitter Transmitter;
      internal readonly SignalParams SignalParams;

      internal DecodeSnapshot(SatnogsDbSatellite? satellite, SatnogsDbTransmitter transmitter, SignalParams signalParams)
      {
        Satellite = satellite;
        Transmitter = transmitter;
        SignalParams = signalParams;
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
        return;
      }

      SignalParams = SignalParamsResolver.Resolve(Transmitter);
      // snapshot the pristine DB-resolved params before the pipeline writes any finding back into SignalParams
      ResolvedSnapshot = SignalParams is null ? null : SignalParams with { };
      UpdateParamsTooltip();
    }

    /// <summary>Refresh the params tooltip on both status labels with the same "name: value" fields the Signal
    /// Details dialog shows (the values actually used for decoding, so the pipeline's locked deviation/baud
    /// replace the curated ones once found), plus the telemetry format its frames are parsed with. Re-called
    /// when a frame arrives so the actual values replace the initial ones once the pipeline locks them.</summary>
    private void UpdateParamsTooltip()
    {
      string tooltip = DescribeSignalParamsOrUnknown(SignalParams);
      toolTip1.SetToolTip(SatNameLabel, tooltip);
      toolTip1.SetToolTip(StatusLabel, tooltip);
    }

    // the tooltip-style params text, or "Parameters unknown" when there are none
    private string DescribeSignalParamsOrUnknown(SignalParams? p) =>
      p == null ? "Parameters unknown" : DescribeSignalParams(p);

    // the dialog's fields as "name: value" lines. Baud/Deviation show the pipeline finding when present, else the
    // curated value; the telemetry format is the manual override when set, else the NORAD-resolved definition.
    // Fields without a value (unknown enums, null numerics, tri-states left on Auto, an unresolved format) are omitted.
    private string DescribeSignalParams(SignalParams p)
    {
      // baud/deviation carry their own run-time-vs-curated flag (self-contained), so a locked value shows with an
      // asterisk here the same way the META block marks it
      var lines = EnumSignalParamFields(p, null).Select(f => $"{f.Name}: {f.Value}{(f.Changed ? " *" : "")}").ToList();
      string? format = FormatOverrideId ?? ResolveFormat(Satellite?.norad_cat_id, p.Framing)?.Id;
      if (!string.IsNullOrEmpty(format)) lines.Add($"Telemetry format: {format}");
      return string.Join("\n", lines);
    }

    // the "  name: value" signal-param lines for the META block: same fields, indented, and any value the
    // pipeline resolved at run time to something other than the DB-resolved value flagged with a trailing '*'.
    private string DescribeSignalParamsMeta(DecodeSnapshot snapshot)
    {
      var p = snapshot.SignalParams;
      // the pristine DB snapshot is only known for the currently selected transmitter's decoder
      var db = ReferenceEquals(snapshot, CurrentDecode) ? ResolvedSnapshot : null;

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
      bool needPipeline = !Terrestrial && IsDecodable() && SatAboveHorizon;

      // keep the existing decoder only if it was built for the currently selected transmitter. a transmitter
      // change must rebuild the pipeline: otherwise it keeps decoding with the previous transmitter's params
      // and its frames get attributed to the newly selected transmitter (wrong sat/norad/telemetry parser).
      bool matches = Decoder != null && CurrentDecode != null && CurrentDecode.Transmitter.uuid == Transmitter.uuid;

      if (needPipeline && matches) return;
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
        var snapshot = new DecodeSnapshot(Satellite, Transmitter, SignalParams!);
        CurrentDecode = snapshot;
        Decoder = new(SignalParams!, IsTelemetryDecodable(), IsSstvDecodable());
        if (Decoder.Pipeline != null)
        {
          Decoder.Pipeline.FrameDecoded += frame => FrameDecodedHandler(frame, snapshot);
          Decoder.Pipeline.BurstDecoded += report => BurstDecodedHandler(report, snapshot);
        }
        if (Decoder.Sstv != null)
        {
          // the image-id → tree-node map lives in the subscription closure, so images from a disposed
          // decoder's flush can never collide with ids of the next decoder's images
          var imageNodes = new Dictionary<int, TreeNode>();
          Decoder.Sstv.ImageUpdated += evt => SstvImageHandler(evt, snapshot, imageNodes);
          Decoder.Sstv.ImageCompleted += evt => SstvImageHandler(evt, snapshot, imageNodes);
        }
      }
    }

    private readonly static Modulation[] SupportedModulations = {
      Modulation.FSK,
      Modulation.GFSK,
      Modulation.GMSK,
      Modulation.BPSK,
      Modulation.AFSK
    };

    private bool IsDecodable()
    {
      return IsTelemetryDecodable() || IsSstvDecodable();
    }

    private bool IsTelemetryDecodable()
    {
      if (SignalParams == null) return false;
      if (SignalParams.Framing == Framing.Unknown || SignalParams.Modulation == Modulation.Unknown || SignalParams.Baud == 0) return false;
      return SupportedModulations.Contains(SignalParams.Modulation);
    }

    // SSTV needs no framing or baud: the VIS header / sync cadence in the demod domain carries the mode.
    // HasSstv also catches mixed FSK+SSTV transmitters (UmKA-1) that classify as FSK — for
    // those BOTH decoders run concurrently and self-gate on their own signatures.
    private bool IsSstvDecodable()
    {
      if (SignalParams == null) return false;
      return SignalParams.Modulation == Modulation.SSTV || SignalParamsResolver.HasSstv(Transmitter);
    }

    private void BurstDecodedHandler(StreamingBurstReport report, DecodeSnapshot snapshot)
    {
      BeginInvoke(() =>
        {
          UpdateStatusLabel("DECODING...", Color.Green);
          // create the pass entry on the first burst (grayed until a valid frame arrives), not on the first frame
          var txPassInfo = EnsureCurrentPassNode(snapshot);
          txPassInfo.BurstCount++;
          if (double.IsNaN(txPassInfo.MaxSnrDb) || report.Burst.SnrDb > txPassInfo.MaxSnrDb)
            txPassInfo.MaxSnrDb = report.Burst.SnrDb;
          // refresh the right panel if this pass entry is the one currently selected
          if (treeView1.SelectedNode == CurrentPassNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));
        }
       );
    }

    private void FrameDecodedHandler(Frame frame, DecodeSnapshot snapshot)
    {
      ctx.KissServer.SendToAll(frame);
      if (snapshot.Satellite?.norad_cat_id is int norad) SatnogsUploader?.Submit(frame, norad);
      BeginInvoke(() => AddFrame(frame, snapshot));
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

      using var dlg = new SignalParamsDialog();
      if (dlg.Open(BuildDialogView(), this) != DialogResult.OK) return;
      ApplyDialogResult(dlg);
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
        DemodValidated = false;
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
      var txPassInfo = EnsureCurrentPassNode(snapshot);

      // un-gray the pass entry once the first valid frame of the pass is decoded
      if (!txPassInfo.HasValidFrame)
      {
        txPassInfo.HasValidFrame = true;
        CurrentPassNode!.ForeColor = Color.Empty;
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

      AddLeaf(CurrentPassNode!, frameNode);
      if (treeView1.SelectedNode == CurrentPassNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));
    }

    /// <summary>Returns the current pass node's info, creating the pass node (grayed until the first valid
    /// frame) when this is the first burst or frame of a new transmitter+orbit pass.</summary>
    private TxPassInfo EnsureCurrentPassNode(DecodeSnapshot snapshot)
    {
      int orbit = ctx.SdrPasses.GetNextPass(snapshot.Satellite)?.OrbitNumber ?? -1;
      var passNode = CurrentPassNode;
      var txPassInfo = (TxPassInfo?)passNode?.Tag;

      if (passNode == null || !(txPassInfo!.IsSame(snapshot.Transmitter, orbit)))
      {
        passNode = new TreeNode($"{DateTime.Now:yyyy-MM-dd HH:mm} {snapshot.Transmitter.Satellite.name}  {snapshot.Transmitter.description}");
        passNode.ForeColor = Color.Gray;
        txPassInfo = new TxPassInfo(snapshot.Transmitter, orbit);
        txPassInfo.SignalParams = snapshot.SignalParams;
        passNode.Tag = txPassInfo;
        treeView1.Nodes.Add(passNode);
        TrackNewNode(passNode);
      }

      return txPassInfo!;
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
    private static (string Addr, int AddrLen) ExtractAddress(Frame frame, DecodeSnapshot snapshot)
    {
      switch (snapshot.SignalParams.Framing)
      {
        case Framing.AX25G3RUH:
        case Framing.USP:
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
      var txPassInfo = EnsureCurrentPassNode(snapshot);

      bool isNew = !imageNodes.TryGetValue(evt.ImageId, out TreeNode? node);
      if (isNew)
      {
        node = new TreeNode();
        node.Tag = new SstvImageInfo(snapshot, evt);
        imageNodes[evt.ImageId] = node;
        txPassInfo.ImageCount++;
        AddLeaf(CurrentPassNode!, node);
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
        CurrentPassNode!.ForeColor = Color.Empty;
      }

      if (!evt.Final) UpdateStatusLabel("DECODING...", Color.Green);

      if (treeView1.SelectedNode == node) DisplayImageInfo(info);
      else if (treeView1.SelectedNode == CurrentPassNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));
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
      CreatDestroyPipeline();

      if (Terrestrial) UpdateStatusLabel("terrestrial, not decoded", Color.Red);
      else if (!IsDecodable()) UpdateStatusLabel("format not supported", Color.Red);
      else if (!SatAboveHorizon) UpdateStatusLabel("satellite below horizon", SystemColors.ControlText);
      else UpdateStatusLabel("ready to decode", SystemColors.ControlText);
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

      if (node.Tag is SstvImageInfo imageInfo)
      {
        DisplayImageInfo(imageInfo);
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
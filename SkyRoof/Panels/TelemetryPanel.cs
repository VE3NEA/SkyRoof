using FontAwesome;
using MathNet.Numerics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using SkyRoof.Satellites;
using System.Globalization;
using VE3NEA;
using VE3NEA.Clock;
using VE3NEA.SkyFM;
using VE3NEA.SkySSTV;
using VE3NEA.SkyTlm.Audio;
using VE3NEA.SkyTlm.Core;
using VE3NEA.SkyTlm.Deframing;
using VE3NEA.SkyTlm.Discovery;
using VE3NEA.SkyTlm.Imaging;
using VE3NEA.SkyTlm.Imaging.RawJpeg;
using VE3NEA.SkyTlm.Imaging.Ssdv;
using VE3NEA.SkyTlm.Telemetry;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class TelemetryPanel : DockContent
  {
    private readonly Context ctx;
    private SatnogsDbSatellite? Satellite;
    // null while terrestrial: the panel declines to know about a satellite it is not decoding, so a
    // terrestrial frame cannot be filed — or uploaded — under one that was not on the air (§4.9). The
    // selector remains the source of truth, and leaving terrestrial mode restores both from it.
    private SatnogsDbTransmitter? Transmitter;
    private bool Terrestrial;
    // Terrestrial decoding has no transmitter, no orbit and no pass to be keyed on, so the tuned downlink
    // frequency is the identity instead (§4.9): it names the pass node, keys the keep-or-rebuild test and
    // heads the archive record. Zero while a satellite is selected, where the uuid and the orbit are the
    // identity as before. A dial move is therefore a new pass - but not a new set of parameters.
    private double TerrestrialHz => Terrestrial ? ctx.FrequencyControl.RadioLink.DownlinkFrequency : 0;
    private static string DescribeTerrestrial(double hz) => $"Terrestrial {hz / 1e6:F3} MHz";
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
    // the two branches are written as statements rather than a conditional, so each converts to the
    // declared type directly: a common type inferred across them would drop the transmitter's nullability
    private (SatnogsDbTransmitter? Transmitter, SignalParams Params)? TelemetrySource
    {
      get
      {
        if (IsTelemetryDecodable()) return (Transmitter, SignalParams!);
        return Sibling;
      }
    }
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
    // set while the image/text splitter is being positioned from the saved settings, so that the resulting
    // SplitterMoved event does not write back what it just read
    private bool RestoringImageSplitter;

    // parameter discovery (discover_params_plan.md): the running search and the dialog it reports to. Both
    // are null unless the operator has pressed Discover; discovery costs nothing when idle.
    private DiscoverySession? Discovery;
    private SignalParamsDialog? ParamsDialog;
    // the parameters proven enough to persist (§2). DemodValidated says one frame decoded with the params in
    // use — that is what turns the dots green. ConfirmingFrames counts the frames after it, and only when it
    // reaches ConfirmFrames may the set be written to the override file or uploaded to SatNOGS: one frame can
    // be a coincidence, three in a row cannot.
    private int ConfirmingFrames;
    private const int ConfirmFrames = 2;
    private bool DemodProven => DemodValidated && ConfirmingFrames >= ConfirmFrames;
    // SatNOGS gets frames decoded with the database's own parameters, or with a set the operator has
    // endorsed by saving it to the override file — and with nothing else. A set that merely works for one
    // pass stays local: the tree, the archive file and the KISS socket all have it, but an upload is the one
    // consequence that cannot be withdrawn, so it waits for the click rather than for the countdown (§4.6).
    // Read on the decode thread and written on the UI thread, hence volatile.
    private volatile bool UploadHeld;
    // the parameters in use are the ones written to transmitters-override.json. Cleared by any further edit:
    // a set edited after saving is unendorsed again, and holds again.
    private bool OverrideSaved;
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
    // the baseline the dialog and the search start from when the transmitter has no parameters at all
    // (§4.8): every field the operator fills in reads as an override against "nothing" and gets its dot,
    // and Reset to database value clears it back to empty.
    private static readonly SignalParams BlankParams =
      new(0, Modulation.Unknown, Framing.Unknown, 48000, null);

    // the FM speech-to-text engine (integration §10). It loads a large model (~71 MB, ~1.5 s), so a single
    // instance is created lazily on first use and SHARED across transmitter changes rather than rebuilt with
    // every decoder; disposed on panel close. Null until an FM transmitter is selected with the model present.
    private SherpaOnnxEngine? FmSpeechEngine;
    // the FM transcript currently shown in richTextBox1 (null while showing telemetry/SSTV) — routes the
    // click-to-play mouse handling to the right content
    private FmTranscriptInfo? CurrentFmTranscript;
    // the voice message currently shown in richTextBox1, routing the same click-to-play handling. A message
    // is one clip rather than a list of lines, so there is no per-span routing to do — the pane either is a
    // voice message or is not.
    private VoiceMessageInfo? CurrentVoice;
    // true when PlayAudioClip found the shared speaker soundcard disabled and temporarily enabled it for the
    // clip, mirroring RecordingManager.StartPlayback/StopPlayback; fires ClipEndTimer to disable it again
    private bool SpeakerEnabledForClip;
    private System.Windows.Forms.Timer? ClipEndTimer;

    // Identity of the transmitter a decoder was built for, captured when the pipeline is created and bound to
    // that pipeline's event handlers. Frames surface on the decode worker thread, possibly after the user has
    // switched to a different transmitter; carrying the snapshot with the frame keeps it attributed to the
    // transmitter that actually produced it instead of to whatever is selected when the frame arrives.
    private sealed class DecodeSnapshot
    {
      internal readonly SatnogsDbSatellite? Satellite;
      internal readonly SatnogsDbTransmitter? Transmitter;
      internal readonly SignalParams SignalParams;
      // The orbit the decoder was built in, captured here rather than re-queried when an event arrives.
      // GetNextPass returns the first pass starting from *now*, so it rolls to the NEXT orbit the instant the
      // current pass ends — and the events that arrive at exactly that moment are the decoder's own flush.
      // Re-deriving the orbit there files them under a pass that has not happened yet.
      internal readonly int Orbit;
      // the tuned frequency a terrestrial decode is identified by (§4.9); zero when Transmitter is set
      internal readonly double TerrestrialHz;

      internal DecodeSnapshot(SatnogsDbSatellite? satellite, SatnogsDbTransmitter? transmitter, SignalParams signalParams, int orbit,
        double terrestrialHz = 0)
      {
        Satellite = satellite;
        Transmitter = transmitter;
        SignalParams = signalParams;
        Orbit = orbit;
        TerrestrialHz = terrestrialHz;
      }
    }

    // The two kinds of image a tree leaf can hold: an SSTV frame-by-frame reconstruction and an SSDV /
    // raw-JPEG one. They share only the three places that do not care which they are looking at — the
    // right-hand pane, the image context menu and the tree selection handler — so the interface is what
    // those need and nothing more. Everything about how the two arrive, and how they are written to disk,
    // differs.
    private interface IImageNodeInfo
    {
      Bitmap? Bitmap { get; }
      // The transfer read as text, when it is text rather than a picture — the Geoscan playlist
      // interleaves photographs with one-fragment ASCII slides. Such a node has no bitmap and never
      // will, so it is shown as text instead of as an empty picture box. Null for everything else.
      string? Text { get; }
      string? SavedPath { get; }
      string Describe();
      // "Save Image As...": the dialog's filter and suggested name, and the write itself. SSTV saves the
      // rendered pixels as PNG; an SSDV image saves the received JPEG file verbatim.
      string SaveFilter { get; }
      string SaveFileName { get; }
      void SaveAs(string path);
      // whether there is anything to write. False grays "Save As..." rather than letting it produce an
      // empty file, which is what an SSDV transfer whose file header never arrived has to offer.
      bool CanSave { get; }
    }

    // one progressively-built SSTV image: the tree node's Tag, updated in place as ImageUpdated events
    // re-render lines, finalized (and auto-saved) on ImageCompleted
    private sealed class SstvImageInfo : IImageNodeInfo
    {
      internal readonly DecodeSnapshot Snapshot;
      internal readonly DateTime FirstSeen = DateTime.UtcNow;
      internal SstvImageEvent Event;
      public Bitmap? Bitmap { get; set; }
      // SSTV is always a picture: there is no text-carrying flavour of it
      public string? Text => null;
      public string? SavedPath { get; set; }

      // The picture currently on display, which is the decoder's own image until the denoise dialog
      // replaces it. Everything that shows, copies or writes the image reads this rather than
      // Event.Image, so "Save Image As..." writes what the operator is looking at (denoise plan §8).
      // Reset whenever a new event arrives, because a filtered rendering describes the reconstruction
      // it was computed from and not the one that just replaced it.
      internal RgbImage Rendering;

      // how Rendering was produced, for the info pane; null while it is the unfiltered reconstruction
      internal string? Filter;

      internal SstvImageInfo(DecodeSnapshot snapshot, SstvImageEvent evt)
      {
        Snapshot = snapshot;
        Event = evt;
        Rendering = evt.Image;
      }

      public string Describe()
      {
        return
          $"Sat: {Snapshot.Transmitter?.Satellite?.name ?? "Unknown"}\r\n" +
          $"Tx: {Snapshot.Transmitter?.description}\r\n" +
          $"Mode: {Event.Mode}\r\n" +
          $"VIS: {(Event.FromVis ? "decoded" : "not decoded, mode from sync cadence")}\r\n" +
          $"Rows: {Event.ValidRows} of {Event.Image.Height}\r\n" +
          $"Status: {(Event.Final ? "complete" : "receiving...")}\r\n" +
          (Filter != null ? $"Filter: {Filter}\r\n" : "") +
          (SavedPath != null ? $"Saved: {SavedPath}\r\n" : "");
      }

      public string SaveFilter => "PNG Image|*.png";
      // saved file names stay in local time whatever the clock widget shows, see NameMatches
      public string SaveFileName => $"{FirstSeen.ToLocalTime():yyyyMMdd_HHmmss}_{Event.Mode}.png";
      public void SaveAs(string path) => Rendering.SavePng(path);
      // a node exists only once the decoder has a reconstruction, so there is always a picture to write
      public bool CanSave => true;
    }

    // one progressively-built SSDV / raw-JPEG image: the tree node's Tag, updated in place as fragments
    // arrive, finalized (and auto-saved) when the assembler announces that nothing further is coming.
    // SkyTlm hands out JPEG bytes rather than pixels — that is what keeps System.Drawing out of the
    // library — so the decoding to a Bitmap happens here, in the app.
    private sealed class SsdvImageInfo : IImageNodeInfo
    {
      internal readonly DecodeSnapshot Snapshot;
      internal readonly DateTime FirstSeen = DateTime.UtcNow;
      // what this pass heard, and nothing else. It is what the tree label counts and what is written to
      // disk, so the sidecar stays a record of one reception rather than a record of a merge.
      internal ImageProduct PassProduct;
      // the same picture rebuilt from this pass plus the archived receptions in Archived, or null when
      // the operator has not asked for that. Recomputed as fragments arrive, so it keeps filling in live.
      internal ImageProduct? MergedProduct;
      // the displayed reconstruction rebuilt with the entropy repair switched off, or null when the
      // operator has not asked for that. A third product rather than a flag on the other two, because the
      // repair happens inside emission: the unrepaired picture can only be produced by building it again
      // from the fragments, and it therefore goes stale exactly where MergedProduct does.
      internal ImageProduct? UnrepairedProduct;
      // earlier receptions of this picture, read out of their sidecars and cached on the first combine so
      // that re-merging on every arriving fragment costs no disk
      internal List<ArchivedPass>? Archived;
      // whether the merged reconstruction is the one on display. Everything that shows or saves the image
      // reads Product, so this one flag is the whole toggle.
      internal bool Combined;
      // whether the entropy repair's answer is the one on display. On by default, because the repair is
      // automatic and runs inside emission — this is the escape hatch for a wrong resync, not an opt-in.
      internal bool Repaired = true;
      // the assembler has announced this image as over. Not the same as Product.Complete: a pass that ends
      // mid-image finalizes what arrived, which off air is the normal case rather than the exception.
      internal bool Final;
      // when the picture on screen was last decoded, so the renders can be coalesced — see ShowSsdvImage
      internal DateTime LastRendered;
      // whether this node has ever had something to show, and so has been offered the tree selection
      internal bool Tracked;
      public Bitmap? Bitmap { get; set; }
      public string? Text => Product.Text;
      // the auto-saved .jpg, or null when this reception had no picture to write. "Open in Viewer" and
      // the Saved: line both key off it, so both stay silent rather than pointing at a file that is not
      // a picture — which is how the 2026-09-13 header-loss defect reached the operator.
      public string? SavedPath { get; set; }
      // the auto-saved .json, which is written whenever there are fragments to archive and so can be
      // present with no SavedPath beside it. Kept rather than derived from SavedPath by extension for
      // exactly that reason — see FindArchivedPasses, which has to know its own sidecar.
      internal string? SidecarPath;

      internal SsdvImageInfo(DecodeSnapshot snapshot, ImageProduct product)
      {
        Snapshot = snapshot;
        PassProduct = product;
      }

      /// <summary>The repaired reconstruction: the merge when combining is on, this pass's otherwise.
      /// Toggling off is exact rather than approximate — the pass product was never altered.
      /// <para>Kept apart from <see cref="Product"/> because the repair switch has to be judged against
      /// what the repair found, which the unrepaired product by definition does not carry.</para></summary>
      internal ImageProduct RepairedProduct => Combined && MergedProduct != null ? MergedProduct : PassProduct;

      /// <summary>The reconstruction currently on display. The unrepaired rebuild wins when the operator
      /// has asked for it, and a rebuild that yielded nothing falls back to the repaired picture rather
      /// than to an empty pane — the same rule MergedProduct follows.</summary>
      internal ImageProduct Product => !Repaired && UnrepairedProduct != null ? UnrepairedProduct : RepairedProduct;

      public string Describe()
      {
        // a text transfer has no geometry, no gap boundary and nothing to render, so none of the lines
        // below say anything about it. The content is the point, and it is short enough to just show.
        if (Product.Text != null)
          return
            $"Sat: {Snapshot.Transmitter?.Satellite?.name ?? "Unknown"}\r\n" +
            $"Tx: {Snapshot.Transmitter?.description}\r\n" +
            $"Message: {Product.ImageId}\r\n" +
            (Product.Source != null ? $"Source: {Product.Source}\r\n" : "") +
            $"\r\n{Product.Text}\r\n";

        return
          $"Sat: {Snapshot.Transmitter?.Satellite?.name ?? "Unknown"}\r\n" +
          $"Tx: {Snapshot.Transmitter?.description}\r\n" +
          $"Image: {Product.ImageId}\r\n" +
          (Product.Source != null ? $"Source: {Product.Source}\r\n" : "") +
          $"Size: {Product.Width} x {Product.Height}\r\n" +
          // the fragment counts are shown as two lines rather than one merged number: what this pass
          // heard is a fact about the reception, and what the combination adds is a fact about the
          // archive, and an operator judging the pass needs to see them apart.
          $"Fragments: {PassProduct.FragmentsReceived} of {PassProduct.FragmentsExpected}\r\n" +
          (Combined && MergedProduct != null
            ? $"Combined: {MergedProduct.FragmentsReceived} of {MergedProduct.FragmentsExpected}" +
              $" (with {Archived!.Count} earlier {(Archived.Count == 1 ? "pass" : "passes")})\r\n"
            : "") +
          // only the raw-JPEG family has such a boundary: one lost fragment desynchronizes the entropy
          // stream and everything past it is noise, so the operator has to be told where truth stops.
          // -1 means the concept does not apply, which is SSDV, where a lost packet costs its own MCUs
          // and nothing else.
          (Product.FirstGapOffset >= 0 ? $"Intact to: {Product.FirstGapOffset} bytes\r\n" : "") +
          // what the entropy repair recovered past that boundary, which is the whole reason the boundary
          // is no longer where the picture stops being useful
          DescribeRepair(Product.Repair) +
          $"Status: {(Product.Complete ? "complete" : Final ? "incomplete" : "receiving...")}\r\n" +
          (SavedPath != null ? $"Saved: {SavedPath}\r\n" : "") +
          // saying which of the two files was written, because they are two decisions. A reception whose
          // JPEG header did not arrive has nothing a decoder could open, and the fragments are still
          // worth keeping — the next pass over this picture is what turns them into one.
          (SavedPath == null && SidecarPath != null
            ? $"No picture: the file header did not arrive.\r\nFragments archived: {SidecarPath}\r\n"
            : "");
      }

      /// <summary>
      /// What the entropy repair made of this reception, or nothing at all when it did not run — a
      /// complete file, a progressive one, an SSDV picture, or the repair switched off. Runs are reported
      /// separately from the total because they are different facts: a run is a piece of the scan put back
      /// where the encoder wrote it, and a declined run is one whose MCU count is known exactly and whose
      /// position is not, which is a refusal rather than a failure and is worth saying out loud.
      /// </summary>
      private static string DescribeRepair(JpegRepair? repair)
      {
        if (repair == null) return "";

        string counts = repair.PlacedMcus.Count == 2
          ? $"{repair.PlacedMcus[0]} and {repair.PlacedMcus[1]}"
          : string.Join(", ", repair.PlacedMcus);

        string placed = repair.PlacedMcus.Count switch
        {
          0 => "nothing placed",
          1 => $"1 run placed ({counts} MCUs)",
          _ => $"{repair.PlacedMcus.Count} runs placed ({counts} MCUs)"
        };

        return $"Repair: {repair.Gaps} {(repair.Gaps == 1 ? "gap" : "gaps")}, {placed}" +
          (repair.DeclinedRuns > 0 ? $", {repair.DeclinedRuns} declined" : "") +
          $" — {repair.RecoveredMcus} of {repair.McuCount} MCUs recovered\r\n";
      }

      public string SaveFilter => Product.Text != null ? "Text File|*.txt" : "JPEG Image|*.jpg";
      // saved file names stay in local time whatever the clock widget shows, see NameMatches
      public string SaveFileName =>
        $"{FirstSeen.ToLocalTime():yyyyMMdd_HHmmss}_{Product.ImageId}{(Product.Text != null ? ".txt" : ".jpg")}";
      // nothing to write when the reconstruction on display has no picture and no text: the emitter
      // returns no bytes at all for a transfer whose header did not arrive, and a zero-byte .jpg on disk
      // is worse than a grayed menu item. Reads Product, so it follows the Combine toggle.
      public bool CanSave => Product.Text != null || Product.Jpeg.Length > 0;
      public void SaveAs(string path)
      {
        if (Product.Text != null) File.WriteAllText(path, Product.Text);
        else File.WriteAllBytes(path, Product.Jpeg);
      }
    }

    // One received codec2 voice message: the tree node's Tag, updated in place as sub-frames arrive and
    // finalized (and auto-saved) when the assembler announces the message is over. Deliberately NOT an
    // IImageNodeInfo: that interface is built around a Bitmap and a picture pane, and a voice node has
    // neither — what it shares with the images is the shape of the lifecycle, not the presentation.
    private sealed class VoiceMessageInfo
    {
      internal readonly DecodeSnapshot Snapshot;
      internal readonly DateTime FirstSeen = DateTime.UtcNow;
      internal VoiceProduct Product;
      // the assembler will send nothing further for this message. Off air the normal case is a message the
      // pass ended in the middle of, so this is not the same as having heard the whole thing — and unlike
      // an image there is no way to know how much was missed, because nothing on air says how long a
      // message is.
      internal bool Final;
      internal string? SavedPath;

      internal VoiceMessageInfo(DecodeSnapshot snapshot, VoiceProduct product)
      {
        Snapshot = snapshot;
        Product = product;
      }

      internal string Describe()
      {
        return
          $"Sat: {Snapshot.Transmitter?.Satellite?.name ?? "Unknown"}\r\n" +
          $"Tx: {Snapshot.Transmitter?.description}\r\n" +
          $"Duration: {Product.DurationSeconds:0.0} s\r\n" +
          // "of N" is deliberately absent: N is the span of what arrived, not the length of what was sent,
          // and presenting it as a denominator would claim knowledge the downlink does not carry.
          $"Sub-frames: {Product.SubFramesReceived} received, {Product.SubFramesExpected} spanned\r\n" +
          $"Numbered: {Product.FirstNumber}..{Product.LastNumber}\r\n" +
          $"Gaps: {(Product.Complete ? "none" : $"{Product.SubFramesExpected - Product.SubFramesReceived} sub-frames, played as silence")}\r\n" +
          $"Status: {(Final ? "complete" : "receiving...")}\r\n" +
          (SavedPath != null ? $"Saved: {SavedPath}\r\n" : "") +
          "\r\nClick here to play.";
      }

      // saved file names stay in local time whatever the clock widget shows, see NameMatches
      internal string SaveFileName =>
        $"{FirstSeen.ToLocalTime():yyyyMMdd_HHmmss}_{Snapshot.Satellite?.name ?? "Unknown"}_voice.wav";
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

    /// <summary>Where one decoded frame's tree leaf belongs, and whether the assembler kept it. Both
    /// questions can only be answered on the decode thread — the type tests read the live assemblers, and
    /// the accept flag is what their <c>Push</c> calls set as they run — so the answer travels to the UI
    /// thread with the frame rather than being worked out again there.</summary>
    private readonly record struct FrameRouting(bool ToImage, bool ToVoice, bool Accepted);

    /// <summary>The one-frame scratch the assembler subscriptions write and the frame handler reads the
    /// moment <c>Push</c> returns. A plain cell is enough: one decoder pushes one frame at a time, on one
    /// thread, and the events fire synchronously inside the call. It lives in the subscription closure
    /// rather than in a field so that a disposed decoder's flush can never write where the next decoder's
    /// frame handler reads — the same reason the node maps live there.</summary>
    private sealed class PushOutcome { internal bool Accepted; }

    internal class TxPassInfo
    {
      internal DateTime StartTime = DateTime.UtcNow;
      internal SatnogsDbTransmitter? Transmitter;
      internal int Orbit;
      // the tuned frequency this terrestrial pass node is keyed on (§4.9); zero when Transmitter is set
      internal double TerrestrialHz;
      internal SignalParams? SignalParams;
      internal int BurstCount = 0;
      internal int FrameCount = 0;
      internal int ImageCount = 0;
      internal double MaxSnrDb = double.NaN;
      internal bool HasValidFrame = false;
      // The picture and the voice message this pass's fragments are filed under: the last one opened, which
      // is the one the assemblers are filling — both are single-transfer-at-a-time by construction, so the
      // last node opened and the transfer in progress are the same thing. Kept here rather than found by
      // walking the pass node's children, which on a pass of thousands of frames would cost a scan each.
      // Null until the pass's first picture or message, and fragments arriving before that stay at pass level.
      internal TreeNode? LastImageNode;
      internal TreeNode? LastVoiceNode;

      internal TxPassInfo(SatnogsDbTransmitter? transmitter, int orbit, double terrestrialHz = 0)
      {
        Transmitter = transmitter;
        Orbit = orbit;
        TerrestrialHz = terrestrialHz;
      }

      // terrestrial has neither uuid nor orbit to match on, so its pass node is keyed on the tuned
      // frequency: a dial move starts a new node, and a satellite pass never matches a terrestrial one (§4.9)
      internal bool IsSame(SatnogsDbTransmitter? transmitter, int orbit, double terrestrialHz)
      {
        if (Transmitter == null || transmitter == null)
          return Transmitter == null && transmitter == null && TerrestrialHz == terrestrialHz;
        return Transmitter.uuid == transmitter.uuid && Orbit == orbit;
      }

      internal string Describe(string paramsText)
      {
        // terrestrial names no satellite, transmitter, uuid or orbit - the tuned frequency is all of it (§4.9)
        string identity = Transmitter == null
          ? $"{DescribeTerrestrial(TerrestrialHz)}\n"
          : $"Sat: {Transmitter.Satellite?.name ?? "Unknown"}\n" +
            $"Tx: {Transmitter.description}\n" +
            $"Norad: {Transmitter.Satellite?.norad_cat_id}\n" +
            $"Uuid: {Transmitter.uuid}\n" +
            $"Orbit: {Orbit}\n";
        return
          $"Start: {ClockWidget.Stamp(StartTime, "yyyy-MM-dd HH:mm:ss")}\n" +
          identity +
          "\n" +
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
      // ImageSplitContainer is still hidden here, so it has not been laid out to its real size yet and its
      // splitter cannot be positioned. It is restored when it first becomes visible, see DisplayImageInfo
    }

    private void ImageSplitContainer_SplitterMoved(object? sender, SplitterEventArgs e)
    {
      // Panel2 is the fixed panel, so its height changes only when the user drags the splitter, not when
      // the panel is resized. That makes this the right moment, and the right quantity, to remember
      if (ImageSplitContainer.Visible && !RestoringImageSplitter)
        ctx.Settings.Telemetry.ImageTextHeight = ImageSplitContainer.Panel2.Height;
    }

    private void TelemetryPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing TelemetryPanel");
      ctx.TelemetryPanel = null;
      ctx.MainForm.TelemetryMNU.Checked = false;
      ctx.Settings.Telemetry.SplitterDistance = splitContainer1.SplitterDistance;

      // stop and free the decode pipeline (joins its worker thread and releases native FFTW memory)
      Decoder?.Dispose();
      Decoder = null;
      CurrentDecode = null;

      // stop any click-to-play clip, then free the shared FM speech engine (the decoder above no longer uses it)
      StopAudioClip();
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

      // staying in terrestrial mode is the same "selection": the manual parameters live as long as
      // terrestrial mode does, and a dial move keeps them — the operator is decoding whatever is at the
      // tuned frequency, and re-typing the parameters at every dial move would be absurd (§4.9). The
      // frequency is the terrestrial pass identity, so UpdateTxStatus's rebuild opens a new pass node.
      if (newTerrestrial && Terrestrial)
      {
        UpdateTxStatus();
        return;
      }

      // while terrestrial the panel holds no selection at all (§4.9): what the selector still points at is
      // not what the radio is tuned to, and copying it here is what would misfile a terrestrial frame.
      Satellite = newTerrestrial ? null : newSatellite;
      Transmitter = newTerrestrial ? null : newTransmitter;
      Terrestrial = newTerrestrial;

      // a new transmitter discards any manual override / provenance state and resets the gear button color
      UserChangedFields.Clear();
      DemodValidated = false;
      ConfirmingFrames = 0;
      FormatOverride = null;
      FormatOverrideId = null;
      FormatValidated = false;
      OverrideSaved = false;
      UpdateUploadHold();
      SettingsButton.ForeColor = SystemColors.GrayText;

      if (Terrestrial) SatNameLabel.Text = "Terrestrial";
      else SatNameLabel.Text = $"{Satellite!.name}  {Transmitter!.description}";

      ResolveSignalParams();
      UpdateTxStatus();
      CreatDestroyPipeline();
    }

    private void ResolveSignalParams()
    {
      if (Terrestrial)
      {
        // entering terrestrial mode starts from nothing; the set the operator then enters by hand is never
        // resolved over, because a re-tune inside terrestrial mode does not reach here at all (§4.9)
        SignalParams = null;
        Sibling = null;
        return;
      }

      SignalParams = SignalParamsResolver.Resolve(Transmitter!);
      // snapshot the pristine DB-resolved params before the pipeline writes any finding back into SignalParams
      ResolvedSnapshot = SignalParams is null ? null : SignalParams with { };
      ResolveSibling();
      UpdateParamsTooltip();
    }

    // Resolve the co-channel transmitter that drives telemetry when the selection cannot (§2.2 rows 2
    // and 4): an SSTV or SSDV selection borrows the top-ranked decodable transmitter on its own downlink.
    // A selection that is telemetry-decodable already is its own source and needs no sibling, and a CW /
    // FM / unsupported one deliberately gets none — those two are the only pairings this feature makes.
    private void ResolveSibling()
    {
      Sibling = null;
      if (IsTelemetryDecodable()) return;
      if (!IsSstvDecodable() && !SignalParamsResolver.HasSsdv(Transmitter)) return;

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
      // terrestrial has no horizon and no pass to gate on: the manual parameters are the whole precondition
      // there — or a running search, which is how a terrestrial signal with no parameters at all is reached
      // (§4.8) — while a satellite still needs its pass (§4.9)
      bool decodeWanted = Terrestrial ? SignalParams != null || Discovery != null : SatAboveHorizon;
      // §2.2: the telemetry branch follows the resolved telemetry source, which is the selection itself
      // unless an SSTV transmitter is selected; the SSTV branch follows the whole downlink, not the
      // selection. Both may name a transmitter other than the selected one.
      var telemetrySource = TelemetrySource;
      bool telemetry = telemetrySource != null;
      bool sstv = IsSstvBranchWanted();
      // the FM branch runs only with the model downloaded; the engine loads lazily here (once per session,
      // then shared) so an FM transmitter with no model simply builds no FM branch (status reflects it)
      var fmEngine = decodeWanted && IsFmDecodable() ? EnsureFmEngine() : null;
      // a discovery session needs a burst source. The telemetry pipeline is one, but it does not exist when
      // the transmitter is CW/SSTV/FM or its format is unsupported — exactly the cases the operator reaches
      // for Discover in — so the search brings its own detection-only pipeline (§4.1).
      var detectParams = Discovery != null && !telemetry ? DiscoveryDetectorParams() : null;
      bool needPipeline = decodeWanted && (telemetry || sstv || fmEngine != null || detectParams != null);

      // keep the existing decoder only if it was built for the currently selected transmitter. a transmitter
      // change must rebuild the pipeline: otherwise it keeps decoding with the previous transmitter's params
      // and its frames get attributed to the newly selected transmitter (wrong sat/norad/telemetry parser).
      // starting or ending a search changes which branches the decoder carries, so it rebuilds for that too.
      // the identity that matters is the TELEMETRY SOURCE's, not the selection's: switching between the two
      // members of a co-channel pair resolves to the same pair of branches, and tearing the decoder down
      // would throw away a locked baud and the in-progress SSTV image for no gain.
      // a terrestrial decode has no uuid to compare, so the tuned frequency answers for it: a dial move
      // rebuilds the pipeline and opens a new pass node, while the manual parameters survive it (§4.9)
      var identity = telemetrySource?.Transmitter ?? Transmitter;
      bool matches = Decoder != null && CurrentDecode != null
        && (identity == null
          ? CurrentDecode.Transmitter == null && CurrentDecode.TerrestrialHz == TerrestrialHz
          : CurrentDecode.Transmitter?.uuid == identity.uuid)
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
          telemetrySource?.Params ?? SignalParams!, ctx.SdrPasses.GetCurrentOrNextPass(Satellite)?.OrbitNumber ?? -1,
          TerrestrialHz);
        var sstvSnapshot = SstvSnapshot(snapshot);
        CurrentDecode = snapshot;
        Decoder = new(snapshot.SignalParams, snapshot.Satellite?.norad_cat_id, telemetry, sstv, fmEngine, detectParams);
        // shared by the frame handler and the two assembler subscriptions below, all of which run on the
        // decode thread and none of which outlive this decoder
        var pushed = new PushOutcome();
        if (Decoder.Pipeline != null)
        {
          // the image assembler is fed from the frame handler, and is captured here rather than read off
          // the Decoder field when a frame surfaces: by then the field may already hold the next
          // transmitter's decoder, and this decoder's frames must never reach that one's assembler.
          var images = Decoder.Images;
          var voice = Decoder.Voice;
          Decoder.Pipeline.FrameDecoded += frame => FrameDecodedHandler(frame, snapshot, images, voice, pushed);
          Decoder.Pipeline.BurstDecoded += report => BurstDecodedHandler(report, snapshot);
        }
        if (Decoder.Images != null)
        {
          // the image-id → tree-node map lives in the subscription closure, the same way the SSTV one
          // does, so an image finalized by a disposed decoder's flush can never collide with an id of the
          // next decoder's images. Images ride the telemetry frames, so they carry the TELEMETRY
          // snapshot's identity — there is no third snapshot here.
          var imageNodes = new Dictionary<int, TreeNode>();
          // ImageUpdated fires once per fragment the assembler took in, synchronously inside the Push the
          // frame handler is sitting in, which is what makes the flag readable there. ImageCompleted does
          // not set it: it also fires for the image the sender has just moved off, which this frame did
          // not go into.
          Decoder.Images.ImageUpdated += product => { pushed.Accepted = true; SsdvImageHandler(product, snapshot, imageNodes, false); };
          Decoder.Images.ImageCompleted += product => SsdvImageHandler(product, snapshot, imageNodes, true);
        }
        if (Decoder.Voice != null)
        {
          // one node per message, keyed the way the images are — but a voice message has no id of its own,
          // so the key is the message's first sub-frame number, which is what the assembler segments on and
          // is stable for the life of a message. The closure keeps a disposed decoder's flush out of the
          // next decoder's nodes, exactly as above.
          var voiceNodes = new Dictionary<int, TreeNode>();
          // sets the accept flag on the same terms as the image subscription above
          Decoder.Voice.VoiceUpdated += product => { pushed.Accepted = true; VoiceMessageHandler(product, snapshot, voiceNodes, false); };
          Decoder.Voice.VoiceCompleted += product => VoiceMessageHandler(product, snapshot, voiceNodes, true);
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
      return IsTelemetryDecodable() || IsSstvDecodable() || IsSsdvDecodable() || IsFmDecodable();
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

    // §2.2 row 4: an SSDV row is not a transmitter at all but a payload type carried inside a telemetry
    // stream — HADES-SA's "SSDV" row and its "FSK 800 bps" row are the same 436.875 MHz signal — so it
    // resolves to Modulation.Unknown and can never build a pipeline of its own. Yet it is the row an
    // operator naturally picks, and it must not be a dead end: it decodes whenever the co-channel sibling
    // that actually carries it does, which is exactly what makes the selection worth calling decodable.
    // Whether that sibling's frames really yield images is the assembler factory's answer, not a mode
    // string's — see IsImagingWanted.
    private bool IsSsdvDecodable()
    {
      return SignalParamsResolver.HasSsdv(Transmitter) && TelemetrySource != null;
    }

    // Whether the resolved telemetry source's frames carry images this build can reconstruct. Asked of the
    // factory, which is the single place that maps a framing to an assembler, so the status line cannot
    // drift from what the decoder actually builds. It allocates one throwaway assembler per call, which is
    // a small object and is only asked on a selection or horizon change.
    private bool IsImagingWanted()
    {
      return TelemetrySource is { } source
        && ImageAssemblerFactory.Create(source.Params, Satellite?.norad_cat_id) != null;
    }

    // §2.2: the SSTV branch runs whenever ANY transmitter on this downlink advertises SSTV, whatever is
    // selected and whatever the DB alive flags say — the decoder self-gates on VIS/sync, so an inactive
    // transmitter costs only filter CPU. IsSstvDecodable stays in the union because it also catches a
    // selection whose resolved modulation is SSTV through a layer HasSstv does not read.
    private bool IsSstvBranchWanted()
    {
      // no terrestrial early-out: with the selection cleared while terrestrial (§4.9),
      // HasCoChannelSstv(null, null) is false and HasSstv(null) is false, so the rule reduces by itself to
      // SignalParams?.Modulation == Modulation.SSTV - choosing SSTV in the combo is all it takes to start
      // the decoder, terrestrial or not, and co-channel pairing stays satellite-only without a guard.
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

    private void FrameDecodedHandler(Frame frame, DecodeSnapshot snapshot, IImageAssembler? images,
      IAudioAssembler? voice, PushOutcome pushed)
    {
      ctx.KissServer.SendToAll(frame);
      // held frames are dropped, not queued: uploading starts at the Save click and runs forward from there
      // (§4.6). Parameters that were never edited are never held — the plain database path is untouched.
      if (!UploadHeld && snapshot.Satellite?.norad_cat_id is int norad) SatnogsUploader?.Submit(frame, norad);
      // Which tree node the frame's leaf will go under, asked BEFORE the Push that may answer it: these are
      // the assemblers' own structural gates, and they are true for a fragment the Push then throws away —
      // a failed checksum, a duplicate, an offset that cannot be placed. Those are exactly the frames a
      // picture's child list would otherwise be missing, and nothing downstream can recover them, since
      // only the fragments that survived raise an event.
      bool toImage = images?.IsImageFrame(frame) == true;
      bool toVoice = voice?.IsVoiceFrame(frame) == true;
      pushed.Accepted = false;
      // Images ride the telemetry frames, so every frame is offered unconditionally and the assembler's
      // own source parser drops the ones that are not image fragments — on HADES-SA, where the SSDV
      // packets are interleaved with telemetry on one downlink, that is most of them. Re-transcoding the
      // whole image per accepted fragment costs microseconds, so this stays on the decode thread.
      images?.Push(frame);
      // voice rides the same frames as images, and is offered on the same terms — the assembler's own gate
      // drops everything that is not a codec2 sub-frame, which on this downlink is most frames. Re-decoding
      // the whole message per sub-frame is under a millisecond, so it too stays on the decode thread.
      voice?.Push(frame);
      // Read after both Pushes and before the marshal below, which is what puts the tree in the right order:
      // a fragment that opens a picture reaches ShowSsdvImage through a BeginInvoke queued INSIDE the Push
      // above, so the node it is filed under is already on the tree by the time AddFrame runs. Moving either
      // Push below this marshal would silently leave every first fragment parentless.
      var routing = new FrameRouting(toImage, toVoice, pushed.Accepted);
      BeginInvoke(() =>
      {
        // read before AddFrame, which may set DemodValidated on this very frame: that frame is the one that
        // made the parameters found, and what the save gate asks for is 2 MORE of them (§4.2).
        bool wasValidated = DemodValidated;
        AddFrame(frame, snapshot, routing);
        // frames decoded AFTER the parameters went green are the evidence the save decision rests on (§2).
        // The increment is kept out of the null-conditional call so that it also runs with the dialog closed
        // — the count must survive the operator closing the dialog and letting the pass run. Gated on the
        // current decoder's snapshot like the validation in AddFrame, so a late frame from a pre-override
        // pipeline cannot prove parameters it was never decoded with.
        if (frame.CrcValid == true && ReferenceEquals(snapshot, CurrentDecode) && DemodValidated)
        {
          // the frame that first validates the set does not count toward the two that must follow it, but it
          // must still turn the dots green and put the countdown on screen. Without this a hand edit showed
          // nothing at all until its second frame, while a discovered set showed "2 more frames to save" the
          // instant it was applied - the same moment reported two different ways (§4.2).
          if (wasValidated) ConfirmingFrames++;
          UpdateGearButton();
          if (wasValidated) ParamsDialog?.ShowConfirmingFrames(ConfirmingFrames, ConfirmFrames);
          else ParamsDialog?.Refresh(BuildDialogView());
        }
      });
    }




    //----------------------------------------------------------------------------------------------
    //                                   signal params override
    //----------------------------------------------------------------------------------------------
    // gear button: open the signal-details editor for the current transmitter and apply any manual override
    private void SettingsButton_Click(object sender, EventArgs e)
    {
      // a transmitter with no parameters at all has the most to gain from Discover, and a hand-entered set
      // is the only other way in, so the dialog opens on the blank baseline instead of refusing (§4.8)
      using var dlg = new SignalParamsDialog();
      dlg.DiscoverToggled += ToggleDiscovery;
      dlg.SaveOverrideRequested += SaveOverrideRequested;
      // every edit is applied as it is committed, so nothing is decided when the dialog closes - OK and
      // Cancel differ only in that Cancel puts the last written-down set back on its way out (§4.3)
      dlg.ParamsEdited += ParamsEditedHandler;
      ParamsDialog = dlg;
      try
      {
        dlg.Open(BuildDialogView(), this);
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
      if (!start) return;

      // below the horizon a session can never be offered a burst — the pipeline it would listen to is not
      // built until AOS. refuse the press and say why, here in the click, so the line never shows the
      // "waiting" of a search that has not started
      if (!Terrestrial && !SatAboveHorizon)
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
      // whether THIS search produced an answer, so its Ended notification can tell "the pass ran out" from
      // "found nothing". Per session rather than panel state: a set already confirmed by an earlier hand
      // edit is not this search's result.
      bool found = false;
      var session = new DiscoverySession(SignalParams ?? BlankParams, CoChannelParams, options);
      session.Progress += ShowDiscoveryProgress;
      session.Found += _ => found = true;
      session.Found += DiscoveryFound;
      session.Ended += () => BeginInvoke(() => ParamsDialog?.ShowDiscoveryEnded(found));
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
    {
      // with nothing resolved for this transmitter the baseline is blank, which reduces to exactly the same
      // detector as a DB row that carries no rate: blind FSK at the default template width (§4.8)
      var current = SignalParams ?? BlankParams;
      return current with
      {
        Modulation = Modulation.FSK,
        Baud = current.Baud > 0 ? current.Baud : DefaultDetectBaud,
        Deviation = null,
        AfCarrier = null,
        Manchester = null,
        Framing = Framing.Unknown,
        ResolvedBaud = null,
        ResolvedDeviation = null
      };
    }

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
        // The tier-1 hypotheses are the co-channel siblings' own parameters, so an answer equal to one of
        // them is not saying that this transmitter is described wrongly — it is saying that the wrong
        // transmitter was selected. Switching to the sibling is the correction; writing its parameters onto
        // this row would file the frames under a transmitter that did not send them, and would offer to save
        // a duplicate of the sibling's values against the wrong uuid. Nothing is marked and nothing is
        // saved: the database was already right about the twin (§4.5).
        if (CoChannelTwin(found.Params) is SatnogsDbTransmitter twin)
        {
          string description = twin.description;
          ctx.SatelliteSelector.SetSelectedTransmitter(twin);
          if (SignalParams != null)
            ParamsDialog?.Repopulate(BuildDialogView(), $"these are {description}'s parameters — switched to it");
          return;
        }
        // the discovered set replaces the demod fields only; the telemetry-format override and the
        // dialog-only fields the search does not touch (AfCarrier, Manchester) are carried through (§6.5).
        var applied = (SignalParams ?? BlankParams) with
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
        ApplySignalParamsOverride(applied);
        ConfirmingFrames = 0;
        AddDiscoveryFrames(found);
        UpdateGearButton();
        UpdateParamsTooltip();
        // the parameters are known now, so the label stops saying "signal parameters unknown" (§4.8)
        UpdateTxStatus();
        // the dialog shows the found values with green dots at once, and starts the countdown that holds
        // Save back until 2 more frames have decoded with them (§2).
        ParamsDialog?.ShowDiscovered(applied, ConfirmFrames);
      });
    }

    // The frames the search itself decoded are the whole evidence for the answer, and until now they were
    // the one thing the operator could not see: the search decodes them inside BurstDiscovery with no sinks
    // wired at all. Show them in the tree, log them and serve them over KISS like any other frame (§4.4).
    // They do NOT count toward ConfirmingFrames: they are what made the parameters found, and the save gate
    // asks for 2 MORE (§4.2) — AddFrame leaves that counter alone, which is what keeps them out of it. The
    // SatNOGS submission is held by §4.6 and needs no gate here: applying the override has just re-armed
    // that hold, so no frame of this batch could be uploaded even if it were offered.
    private void AddDiscoveryFrames(DiscoveryCandidate found)
    {
      if (CurrentDecode is not DecodeSnapshot decode) return;
      foreach (var frame in found.Frames)
      {
        ctx.KissServer.SendToAll(frame);
        // no routing: the search decodes these inside BurstDiscovery with no assembler wired to it at all,
        // so nothing has been offered a fragment of them and none of them can be filed under a picture or a
        // message. They go to the pass node, which is where every frame went before there was a choice.
        AddFrame(frame, decode, default);
      }
    }

    // The co-channel sibling whose own database parameters the answer reproduces, or null (§4.5). Tier 1 of
    // the hypothesis set is exactly those siblings, but a generic tier-2 hypothesis can land on a sibling's
    // values and the conclusion is identical, so the tier is not tested here — only the parameters are.
    private SatnogsDbTransmitter? CoChannelTwin(SignalParams found)
    {
      if (Satellite == null || Transmitter == null) return null;
      return Satellite.Transmitters.FirstOrDefault(tx =>
        !ReferenceEquals(tx, Transmitter) && tx.downlink_low == Transmitter.downlink_low
        && ParamsMatch(SignalParamsResolver.Resolve(tx), found));
    }

    // the two sets describe the same signal. The rates are compared after the same rounding the operator and
    // the override file see, because the search measures 9600.83 against a curated 9600.
    private static bool ParamsMatch(SignalParams? db, SignalParams found)
    {
      if (db == null) return false;
      if (db.Modulation != found.Modulation || db.Framing != found.Framing) return false;
      if (SignalParamsResolver.RoundToStandard(db.ResolvedBaud ?? db.Baud)
        != SignalParamsResolver.RoundToStandard(found.ResolvedBaud ?? found.Baud)) return false;
      return SignalParamsResolver.RoundToStandard(db.ResolvedDeviation ?? db.Deviation)
        == SignalParamsResolver.RoundToStandard(found.ResolvedDeviation ?? found.Deviation);
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
        // the operator has endorsed these parameters, so the frames decoded with them may be published (§4.6)
        OverrideSaved = true;
        UpdateUploadHold();
        // and they are now what Cancel goes back to: a saved set is the recorded truth about this
        // transmitter, so a Cancel after it has nothing to undo (§4.3)
        ParamsDialog.MarkSaved();
        // no frame is due to redraw the line, and it is still counting toward a decision now taken
        ParamsDialog.ShowConfirmingFrames(ConfirmingFrames, ConfirmFrames);
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
      // with nothing resolved for this transmitter the blank baseline stands in as both the current and the
      // database values, so every field reads as an override against "nothing" (§4.8)
      var current = SignalParams ?? BlankParams;
      string? dbFormatId = ResolveFormat(Satellite?.norad_cat_id, current.Framing)?.Id;

      return new SignalParamsView
      {
        Params = current,
        DbParams = ResolvedSnapshot ?? current,
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
          : FormatValidated ? SignalParamsDialog.FieldDot.Confirmed : SignalParamsDialog.FieldDot.Edited,
        // the save gate and the countdown live in the panel, where the frames arrive, so a reopened dialog
        // comes back as it was left rather than with Save unconditionally greyed out (§3, §4.3)
        CanSave = DemodProven && UserChangedFields.Count > 0,
        // terrestrial has no transmitter row to write against, and its parameters are a session tool rather
        // than a database correction, so Save is not offered there at all and the countdown in front of it
        // is replaced by the reason (§4.9). Kept apart from CanSave, which stays the verdict on the
        // evidence alone: the two answer different questions, and only one of them frames can change.
        Savable = Transmitter != null,
        Saved = OverrideSaved,
        Status = DemodValidated && UserChangedFields.Count > 0
          ? SignalParamsDialog.ConfirmingFramesText(ConfirmingFrames, ConfirmFrames, Transmitter != null,
            OverrideSaved) : null
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

    // An edit was committed in the open dialog. Apply it at once and hand the dialog back the state it does
    // not own: the dots, the save gate and the countdown all live here, where the frames arrive (§3).
    private void ParamsEditedHandler(object? sender, EventArgs e)
    {
      if (ParamsDialog == null) return;
      ApplyDialogResult(ParamsDialog);
      ParamsDialog.Refresh(BuildDialogView());
    }

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
      // parameters entered against a blank baseline make an "unknown" transmitter decodable, so the status
      // label must be re-derived rather than left on DescribeUnsupported (§4.8)
      UpdateTxStatus();
    }

    // adopt the user-edited params and rebuild the pipeline so they take effect immediately. CreatDestroyPipeline
    // keeps the existing decoder while the transmitter is unchanged, so the decoder is torn down explicitly here
    // to force a fresh one built from the overridden params.
    private void ApplySignalParamsOverride(SignalParams newParams)
    {
      SignalParams = newParams;
      // a new set of parameters is a new claim, and none of the frames counted so far were decoded with it:
      // the evidence for saving restarts from zero (§4.1), and the uploads hold again until it is saved
      ConfirmingFrames = 0;
      OverrideSaved = false;
      UpdateUploadHold();

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
      if (!userChange && !pipelineFound) { SettingsButton.ForeColor = SystemColors.GrayText; return; }

      bool demodOk = UserChangedFields.Count == 0 || DemodValidated;
      bool formatOk = FormatOverrideId == null || FormatValidated;
      SettingsButton.ForeColor = demodOk && formatOk
        ? SignalParamsDialog.ConfirmedColor : SignalParamsDialog.EditedColor;
    }

    // the upload hold's two inputs (§4.6), re-evaluated on the UI thread wherever either of them changes
    private void UpdateUploadHold() => UploadHeld = UserChangedFields.Count > 0 && !OverrideSaved;




    //----------------------------------------------------------------------------------------------
    //                                       treeview
    //----------------------------------------------------------------------------------------------
    private void AddFrame(Frame frame, DecodeSnapshot snapshot, FrameRouting routing)
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
      // the SSDV packet's own verdict, taken once here and used twice: it labels the tree leaf and it goes
      // into the detail text. Null on every frame that is not an SSDV packet, the whole raw-JPEG family
      // included, which is why it is not the thing that decides where the leaf is filed.
      var imageCheck = ImageAssemblerFactory.CheckImagePacket(
        snapshot.SignalParams, snapshot.Satellite?.norad_cat_id, frame);
      string nodeText = $"{ClockWidget.Stamp(DateTime.UtcNow, "HH:mm:ss")}  {frame.Length} bytes  {addr}" +
        DescribeFragmentOutcome(routing, imageCheck);
      var frameNode = new TreeNode(nodeText);
      string frameText = BuildFrameText(frame, snapshot, addr, addrLen, imageCheck);
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

      // An image or voice frame is filed under the picture or the message it belongs to rather than beside
      // it, which is what lets a pass of thousands of fragments read as a handful of lines. The parent is
      // the last one opened in this pass, not one looked up by id: a duplicate and a rejected fragment
      // carry no id anybody can trust, and the assemblers only ever fill one transfer at a time anyway.
      // A type-matching frame that arrives before any such node exists — the opening fragment of a picture
      // failing its CRC, say — falls back to the pass node, which is where it would have gone before.
      var parent = routing.ToImage ? txPassInfo.LastImageNode
        : routing.ToVoice ? txPassInfo.LastVoiceNode : null;
      if (parent != null) AddFragment(parent, frameNode);
      else AddLeaf(passNode, frameNode);
      if (treeView1.SelectedNode == passNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));
    }

    /// <summary>The suffix that says what became of an image or voice fragment, empty for an ordinary frame.
    /// Every frame of the type is filed under the picture, so without this its children cannot be read
    /// against its "n of m fragments" label — the ones that changed nothing are the whole of the difference
    /// between the two counts, and they are indistinguishable from the rest by time and length alone.</summary>
    private static string DescribeFragmentOutcome(FrameRouting routing, ImagePacketCheck? check) => routing switch
    {
      // the packet's own CRC-32 failed, so it was read and thrown away. Off air this is the difference
      // between "the satellite sent no picture" and "none of its packets survived the pass".
      { ToImage: true } when check is { Ok: false } => "  CRC FAIL",
      // it parsed, and the assembler still took nothing from it: on the SSDV family, where the CRC has
      // already spoken, a duplicate is the only thing left that it can be.
      { ToImage: true, Accepted: false } when check != null => "  duplicate",
      // the raw-JPEG family, whose fragments carry no checksum. Here the same silence may be a duplicate,
      // an offset too far out to be believed, or a transfer that is not a picture at all — USP moves logs
      // and configs down the channel it moves images down. Nothing here can separate those, so the label
      // claims no more than it knows.
      { ToImage: true, Accepted: false } => "  not used",
      { ToVoice: true, Accepted: false } => "  duplicate",
      _ => ""
    };

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
        if (node.Tag is TxPassInfo info && info.IsSame(snapshot.Transmitter, orbit, snapshot.TerrestrialHz)) return (node, info);
      }

      string title = snapshot.Transmitter == null ? DescribeTerrestrial(snapshot.TerrestrialHz)
        : $"{snapshot.Transmitter.Satellite.name}  {snapshot.Transmitter.description}";
      var passNode = new TreeNode($"{ClockWidget.Stamp(DateTime.UtcNow, "yyyy-MM-dd HH:mm")} {title}");
      if (grayUntilContent) passNode.ForeColor = SystemColors.GrayText;
      var txPassInfo = new TxPassInfo(snapshot.Transmitter, orbit, snapshot.TerrestrialHz);
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
    /// popping it open on every new frame/image would fight that choice.
    /// <para><paramref name="track"/> false adds the leaf without offering it the selection, for a node that
    /// has nothing to show yet: an image node is created on its first fragment, long before it can render,
    /// and following it there replaces whatever is on screen with a blank rectangle. Such a node is tracked
    /// later, on the first update that gives it content — see <c>ShowSsdvImage</c>.</para></summary>
    private void AddLeaf(TreeNode passNode, TreeNode leaf, bool track = true)
    {
      bool keepCollapsed = !passNode.IsExpanded && passNode.Nodes.Count > 0 && treeView1.SelectedNode == passNode;
      passNode.Nodes.Add(leaf);
      if (!keepCollapsed) passNode.Expand();
      if (track) TrackNewNode(leaf);
    }

    /// <summary>Adds a fragment leaf under the picture or message it belongs to. Deliberately not
    /// <see cref="AddLeaf"/>: it neither expands the parent nor offers the leaf the selection, because a
    /// picture is meant to stay one line in the tree with its fragments folded away underneath. Expanding
    /// here would unfold it on every fragment — nine a second on a Geoscan pass — and selecting a child
    /// would do the same indirectly, since WinForms expands a node's ancestors to bring it into view.
    /// <para>The picture's own node keeps the selection instead, which <c>ShowSsdvImage</c> gives it once
    /// there is something to look at. Selecting THAT leaves it collapsed: only ancestors are expanded.</para>
    /// </summary>
    private static void AddFragment(TreeNode parent, TreeNode leaf) => parent.Nodes.Add(leaf);

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

    private string BuildFrameText(Frame frame, DecodeSnapshot snapshot, string addr, int addrLen,
      ImagePacketCheck? imageCheck)
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

      // a GEOSCAN frame carries a header we can name even when it is not telemetry: the sending satellite,
      // the message type, and, on an image frame, where its bytes belong in the picture. Empty for the AX.25
      // beacon flavor of the same downlink, which the address and the telemetry fields already describe.
      // AO-123's frames likewise: the TM short header (spacecraft, virtual channel, counters), then the SSDV
      // packet header on an image frame or the text of a callsign beacon. CCSDS is shared by many satellites
      // with other layouts, so this one is selected by NORAD ID.
      string geo = "";
      if (snapshot.SignalParams.Framing == Framing.GEOSCAN)
        geo = string.Join("", GeoscanHeader.Describe(frame.Bytes).Select(f => $"  {f.Name}: {f.Value}\n"));
      else if (snapshot.SignalParams.Framing == Framing.CCSDS && snapshot.Satellite?.norad_cat_id == Asrtu1Header.NoradId)
        geo = string.Join("", Asrtu1Header.Describe(frame.Bytes).Select(f => $"  {f.Name}: {f.Value}\n"));

      // an SSDV packet carries a CRC-32 and RS of its own, and the framings that carry it — HADES, AO-40
      // FEC — carry no frame CRC at all, so the "CRC:" line below reads "n/a" on precisely the frames whose
      // payload can be checked. Off air that is the difference between "no image because the satellite sent
      // none" and "no image because none of the packets survived the pass", which the frame list cannot show.
      string ssdvMeta = imageCheck switch
      {
        { Ok: true, CorrectedBytes: 0 } => "  SSDV packet: CRC OK\n",
        { Ok: true } check => $"  SSDV packet: CRC OK, {check.CorrectedBytes} RS corrections\n",
        { Ok: false } => "  SSDV packet: CRC FAIL\n",
        null => ""
      };

      string tlm = "";
      if (addr.Length > 0 || geo.Length > 0 || fields.Length > 0)
      {
        tlm = "PAYLOAD:\n";
        if (addr.Length > 0) tlm += $"  Address: {addr}\n";
        tlm += geo + fields + "\n";
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
        ssdvMeta +
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
      // the finalized image is filtered once, here, with the auto-save filter from the settings, so the
      // picture on display and the picture on disk are the same one
      var rendering = evt.Image;
      string? filter = null;
      if (evt.Final) (rendering, filter) = ApplySaveFilter(evt, ctx.Settings.Telemetry.SstvSaveFilter);

      string? savedPath = evt.Final && evt.ValidRows > 0 ? SaveImageToFile(evt, rendering, snapshot) : null;
      BeginInvoke(() => ShowImage(evt, rendering, filter, snapshot, imageNodes, savedPath));
    }

    // The finalized picture with the auto-save filter applied, and its description for the info pane.
    // evt.Image has already been through the decode-time Wiener (D15), so Wiener returns it as is, with no
    // description, as before this setting existed; None and NLM start from the raw reconstruction in
    // evt.Planes, NLM with the library default parameters. Without planes there is nothing to re-filter.
    private static (RgbImage Image, string? Filter) ApplySaveFilter(SstvImageEvent evt, SstvSaveFilter filter)
    {
      if (evt.Planes is not { } planes || filter == SstvSaveFilter.Wiener) return (evt.Image, null);

      try
      {
        var options = new SstvDenoiseOptions
        {
          Method = filter == SstvSaveFilter.None ? SstvDenoiseMethod.None : SstvDenoiseMethod.Nlm
        };
        var image = options.Method == SstvDenoiseMethod.None ? planes.ToRgb() : planes.Denoise(options).ToRgb();
        return (image, DescribeFilter(options));
      }
      catch (Exception e)
      {
        Log.Error(e, "Failed to filter SSTV image for auto-save");
        return (evt.Image, null);
      }
    }

    private void ShowImage(SstvImageEvent evt, RgbImage rendering, string? filter, DecodeSnapshot snapshot,
      Dictionary<int, TreeNode> imageNodes, string? savedPath)
    {
      var (passNode, txPassInfo) = EnsureCurrentPassNode(snapshot);

      bool isNew = !imageNodes.TryGetValue(evt.ImageId, out TreeNode? node);
      if (isNew)
      {
        node = new TreeNode { ContextMenuStrip = ImageMenu };
        node.Tag = new SstvImageInfo(snapshot, evt);
        imageNodes[evt.ImageId] = node;
        txPassInfo.ImageCount++;
        AddLeaf(passNode, node);
      }

      // swap in the new reconstruction; dispose the previous bitmap only after the PictureBox lets go of it
      var info = (SstvImageInfo)node!.Tag;
      var oldBitmap = info.Bitmap;
      info.Event = evt;
      // the new reconstruction is what is now on display; any filtered rendering described the previous one.
      // On the final event it is the auto-save rendering, until the operator filters it by hand.
      info.Rendering = rendering;
      info.Filter = filter;
      info.Bitmap = rendering.ToBitmap();
      if (savedPath != null) info.SavedPath = savedPath;
      node.Text = $"{ClockWidget.Stamp(info.FirstSeen, "HH:mm:ss")}  {evt.Mode}  {evt.ValidRows}/{evt.Image.Height} rows";
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

    private void DisplayImageInfo(IImageNodeInfo info)
    {
      // A transfer that is text has no picture and never will, so the image pane would only be an empty
      // rectangle beside it. Show it the way a decoded frame is shown instead.
      if (info.Text != null)
      {
        ShowTelemetryText();
        richTextBox1.Text = info.Describe();
        return;
      }

      if (richTextBox1.Parent != ImageSplitContainer.Panel2)
      {
        richTextBox1.Parent = ImageSplitContainer.Panel2;
        richTextBox1.Dock = DockStyle.Fill;
      }
      bool wasHidden = !ImageSplitContainer.Visible;
      ImageSplitContainer.Visible = true;
      // making it visible is what finally docks it to its real size, so this is the first moment at which
      // the saved splitter position can be applied
      if (wasHidden) RestoreImageSplitter();
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

    // positions the image/text splitter so that the text sub-panel gets the height the user left it at
    private void RestoreImageSplitter()
    {
      int available = ImageSplitContainer.Height - ImageSplitContainer.SplitterWidth;
      int distance = available - ctx.Settings.Telemetry.ImageTextHeight;
      distance = Math.Clamp(distance, ImageSplitContainer.Panel1MinSize, available - ImageSplitContainer.Panel2MinSize);
      if (distance <= 0) return;

      // a distance that had to be clamped because the panel is too small must not overwrite the setting,
      // the user's height is still the one to use once the panel is large enough again
      RestoringImageSplitter = true;
      try { ImageSplitContainer.SplitterDistance = distance; }
      finally { RestoringImageSplitter = false; }
    }

    /// <summary>Auto-save the finalized image as PNG + JSON metadata sidecar under the user data folder.
    /// <paramref name="rendering"/> is the picture with the auto-save filter applied, see ApplySaveFilter.</summary>
    private static string? SaveImageToFile(SstvImageEvent evt, RgbImage rendering, DecodeSnapshot snapshot)
    {
      try
      {
        string folder = Path.Combine(Utils.GetUserDataFolder(), "SstvImages");
        string sat = string.Concat((snapshot.Satellite?.name ?? "Unknown").Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{sat}_{evt.Mode}_{evt.ImageId}.png");
        rendering.SavePng(path);

        var meta = new
        {
          Utc = DateTime.UtcNow,
          Satellite = snapshot.Satellite?.name,
          Norad = snapshot.Satellite?.norad_cat_id,
          Transmitter = snapshot.Transmitter?.description,
          TransmitterUuid = snapshot.Transmitter?.uuid,
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
      if (treeView1.SelectedNode?.Tag is not IImageNodeInfo info) return;
      using var dlg = new SaveFileDialog { Filter = info.SaveFilter, FileName = info.SaveFileName };
      if (dlg.ShowDialog() == DialogResult.OK) info.SaveAs(dlg.FileName);
    }

    private void CopyImageMNU_Click(object sender, EventArgs e)
    {
      if (ImageBox.Image != null) Clipboard.SetImage(ImageBox.Image);
    }

    // Post-filter a completed image (denoise plan §8). The dialog filters evt.Planes — the raw, unfiltered
    // reconstruction the event carries alongside the picture — so it always starts from the original however
    // many times it is applied, and choosing None is an exact undo rather than an inverse filter.
    private void DenoiseImageMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is not SstvImageInfo info || info.Event.Planes is not { } planes) return;

      using var dlg = new SstvDenoiseDialog();
      string caption = $"{info.Snapshot.Satellite?.name ?? "Unknown"}  {ClockWidget.Stamp(info.FirstSeen, "HH:mm:ss")}  {info.Event.Mode}";
      if (dlg.Open(planes, caption, this) != DialogResult.OK) return;

      info.Rendering = dlg.Result;
      info.Filter = DescribeFilter(dlg.Options);

      var oldBitmap = info.Bitmap;
      info.Bitmap = info.Rendering.ToBitmap();
      if (ImageBox.Image == oldBitmap) ImageBox.Image = info.Bitmap;
      oldBitmap?.Dispose();

      // the auto-saved PNG is the pass's record of this image, so it follows the display rather than
      // preserving a rendering the operator has just rejected
      if (info.SavedPath != null)
      {
        try { info.Rendering.SavePng(info.SavedPath); }
        catch (Exception ex) { Log.Error(ex, "Failed to re-save denoised SSTV image"); }
      }

      if (treeView1.SelectedNode?.Tag == info) richTextBox1.Text = info.Describe();
    }

    // None is reported rather than left blank, because it is not the same picture the node started with: the
    // decode path applies the Wiener by default (D15), so choosing None strips a filter rather than leaving
    // the image alone.
    private static string DescribeFilter(SstvDenoiseOptions o)
    {
      if (o.Method == SstvDenoiseMethod.None) return "none (raw reconstruction)";

      string gate = o.SkipNoiseOnlyBands ? ", skipping noise-only bands" : "";
      return o.Method == SstvDenoiseMethod.Wiener
        ? $"Wiener {o.WienerWindowW}x{o.WienerWindowH}{gate}"
        : $"NLM strength {o.NlmSig:0.00}, patch {o.NlmPatchWing}" +
          $"{(o.NlmTwoPass ? ", two-pass" : "")}{gate}";
    }

    // gray the "Open in Viewer" item until the selected image has been auto-saved to a file on disk, and
    // the "Combine with Previous Passes" item until this picture has actually been heard before — the
    // archive is searched here, on demand, rather than kept indexed. "Save As..." is grayed when there is
    // nothing to write at all, which follows the Combine toggle - see CanSave.
    private void ImageMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      var info = treeView1.SelectedNode?.Tag as IImageNodeInfo;
      SaveImageMNU.Enabled = info?.CanSave == true;
      OpenImageMNU.Enabled = info?.SavedPath != null && File.Exists(info.SavedPath);

      // denoising needs the raw reconstruction, which rides only on the FINAL image event: a picture still
      // being drawn has no planes to filter, and filtering it would be overwritten by the next line anyway
      DenoiseImageMNU.Enabled = info is SstvImageInfo sstv && sstv.Event.Planes != null;

      var ssdv = info as SsdvImageInfo;
      bool canCombine = ssdv != null && CanCombine(ssdv);
      CombineImageMNU.Enabled = canCombine;
      CombineImageMNU.Checked = ssdv?.Combined == true;
      CombineImageMNU.Text = canCombine
        ? $"Combine with Previous Passes ({ssdv!.Archived!.Count})"
        : "Combine with Previous Passes";

      // offered only where the walker actually found something to put back — a complete file, an SSDV
      // picture and a refusal all have nothing to switch off. Judged on the REPAIRED product, which is the
      // one that carries the finding: reading it off Product would gray the item out as soon as it was
      // unchecked, and leave the operator with no way back.
      RepairImageMNU.Enabled = ssdv?.RepairedProduct.Repair != null;
      // and unchecked wherever it is grayed, because a grayed check mark reads as "repaired, and you
      // cannot undo it" on a picture the repair never ran on at all
      RepairImageMNU.Checked = RepairImageMNU.Enabled && ssdv!.Repaired;
    }

    private void OpenImageMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is not IImageNodeInfo info || info.SavedPath == null) return;
      try
      {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(info.SavedPath) { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to open the image in viewer");
      }
    }




    //----------------------------------------------------------------------------------------------
    //                                      ssdv images
    //----------------------------------------------------------------------------------------------
    // called on the decode worker thread (or on the UI thread when a disposed decoder flushes); the
    // finalized image is auto-saved here, before marshaling, so a pass ending with the panel closing
    // cannot lose it. final says the assembler will send nothing further for this image — which is not the
    // same as the image being whole, and off air it usually is not.
    private void SsdvImageHandler(ImageProduct product, DecodeSnapshot snapshot, Dictionary<int, TreeNode> imageNodes, bool final)
    {
      var saved = final && ShouldSaveImage(product) ? SaveImageToFile(product, snapshot) : default;
      BeginInvoke(() => ShowSsdvImage(product, snapshot, imageNodes, saved, final));
    }

    /// <summary>What actually reached the disk for one finalized image. The <c>.jpg</c> and the
    /// <c>.json</c> stopped being one decision on 2026-09-13: a reception whose JPEG header was lost has
    /// no picture to write — three such images were auto-saved as unopenable <c>.jpg</c> files that day —
    /// but its fragments are exactly what a later pass merges against, so the sidecar still goes out.
    /// Either path may be null; both null means nothing was written.</summary>
    private readonly record struct SavedImage(string? JpegPath, string? SidecarPath);

    // fragments an image must carry before it is written to disk — see ShouldSaveImage for why two is
    // the right number and why a coverage fraction is deliberately not used
    private const int MinFragmentsToSave = 2;

    // how often a filling-in image is re-decoded and put back on screen — see ShowSsdvImage
    private static readonly TimeSpan RenderInterval = TimeSpan.FromSeconds(1);

    /// <summary>Whether a finalized image is worth writing to disk. Deliberately harder to satisfy than
    /// showing it is: a stray tree node costs nothing and goes away with the session, but a file in
    /// <c>SsdvImages\</c> is durable clutter. Two conditions:
    /// <list type="bullet">
    /// <item><b>More than one fragment.</b> A false positive is a single misread frame. Two of them
    /// landing in the same image — each having passed the deframer's CRC/RS, each parsing as an image
    /// fragment, and both carrying the same image identity — is not a coincidence that happens; if it
    /// has happened, the image is real. So this is the whole test, and one fragment is the only thing
    /// it rejects.</item>
    /// <item>The reconstruction has a <b>geometry</b>. Free on SSDV, which carries width and height in
    /// every packet header; for the raw-JPEG family it comes from walking the file's own markers, so a
    /// picture built from a fragment at an arbitrary offset has neither an SOI nor a frame header and
    /// reports 0 x 0. That is the check that guards the front end with no off-air validation, USP's.</item>
    /// </list>
    /// A <b>coverage fraction is deliberately not used.</b> It does not catch the case it looks like it
    /// should — a lone raw-JPEG fragment reports 1 of 1 expected, i.e. 100%, because the expected count
    /// is derived from the span actually written — and it withholds real images: the off-air
    /// <c>hades-sa_img225_tailonly</c>, whose first nine packets were lost, is 6 of 15.
    /// <para>An image that fails these is still shown and still savable by hand from the image context
    /// menu. Only the automatic write is withheld.</para>
    /// <para>The fragment count drops to <b>one</b> where the fragments carry their own CRC, which is
    /// what <see cref="ImageProduct.FragmentFormat"/> reports. The two-fragment rule exists to reject a
    /// single misread frame, and a CRC-32 already does that far better than a coincidence argument can —
    /// a false accept is a 1-in-4-billion event. Such a fragment is also worth keeping for its own sake:
    /// it is archived in the sidecar and can complete this picture on a later pass, which a fragment
    /// thrown away cannot. It does <b>not</b> drop for the raw-JPEG format, whose fragments are
    /// re-identifiable but carry no checksum, so the coincidence argument is all there is there.</para>
    /// <para>The <b>geometry</b> test is likewise dropped for that format, and only for it. A run that
    /// joined a transfer after its head went past has no frame header and reports 0 x 0 — 282 fragments
    /// of the 2026-09-12/13 Geoscan capture arrived that way — and those fragments are exactly what a
    /// later pass needs to finish the picture. Withholding them makes the archive unable to do the one
    /// thing it is for. USP, which is what the geometry test was written to guard because its front end
    /// has no off-air validation, hands out no fragments and so keeps the test.</para></summary>
    private static bool ShouldSaveImage(ImageProduct product)
    {
      bool rawJpeg = product.FragmentFormat == RawJpegMerge.Format;

      // something has to reach the disk: a picture to write, or fragments to archive. Until 2026-09-13
      // this read "no picture, nothing saved", which threw the fragments away with it — and the
      // receptions that emit no picture, the ones whose header was lost, are precisely the ones whose
      // fragments a later pass needs. SaveImageToFile decides which of the two files it writes.
      if (product.Jpeg.Length == 0 && (product.FragmentFormat == null || product.Fragments.Count == 0))
        return false;

      int floor = product.FragmentFormat != null && !rawJpeg ? 1 : MinFragmentsToSave;

      return product.FragmentsReceived >= floor
        && (rawJpeg || product.Width > 0 && product.Height > 0);
    }

    private void ShowSsdvImage(ImageProduct product, DecodeSnapshot snapshot, Dictionary<int, TreeNode> imageNodes,
      SavedImage saved, bool final)
    {
      var (passNode, txPassInfo) = EnsureCurrentPassNode(snapshot);

      bool isNew = !imageNodes.TryGetValue(product.ImageId, out TreeNode? node);
      if (isNew)
      {
        node = new TreeNode { ContextMenuStrip = ImageMenu };
        node.Tag = new SsdvImageInfo(snapshot, product);
        imageNodes[product.ImageId] = node;
        txPassInfo.ImageCount++;
        // added but not followed: a node created on its first fragment has nothing to show, and until
        // this capture the tree followed it anyway — which is how a good picture was replaced on screen
        // by a blank rectangle at the exact instant it was written to disk. See TrackNewNode below.
        AddLeaf(passNode, node, track: false);
        // from here on this pass's image fragments are filed under this node — see AddFrame. Set for the
        // SSDV and raw-JPEG families only: an SSTV image is built from audio and has no frames to adopt.
        txPassInfo.LastImageNode = node;
      }

      // take in the new reconstruction of this pass; RenderImage below swaps the picture on screen for it
      var info = (SsdvImageInfo)node!.Tag;
      info.PassProduct = product;
      info.Final |= final;
      if (saved.JpegPath != null) info.SavedPath = saved.JpegPath;
      if (saved.SidecarPath != null) info.SidecarPath = saved.SidecarPath;
      // the tree label always counts what THIS pass heard, combined or not — it is the pass that the node
      // is a record of, and a label that changed under a toggle would make two nodes incomparable
      node.Text = product.Text != null
        ? $"{ClockWidget.Stamp(info.FirstSeen, "HH:mm:ss")}  Message {product.ImageId}"
        : $"{ClockWidget.Stamp(info.FirstSeen, "HH:mm:ss")}  Image {product.ImageId}  " +
          $"{product.FragmentsReceived}/{product.FragmentsExpected} fragments";

      // Coalesce the decodes. A Geoscan pass delivers around nine fragments a second and every one of
      // them used to cost a full 800x600 JPEG decode on the UI thread. One picture a second is as fast
      // as a filling-in image can be read, and the final reconstruction is rendered whatever the clock
      // says, so nothing is ever left showing a stale picture.
      if (final || DateTime.UtcNow - info.LastRendered >= RenderInterval)
      {
        info.LastRendered = DateTime.UtcNow;
        // combining stays live: the archived fragments are already cached, so the merge is redone with
        // the fragments that just arrived and the combined picture fills in during the pass like any
        // other. It is inside the gate because it costs more than the decode does — a raw-JPEG merge
        // rewrites every fragment of every reception — and there is no point merging what is not shown.
        if (info.Combined) Recombine(info);
        // and the unrepaired rebuild goes stale on the same fragments, so it fills in live beside them
        if (!info.Repaired) RebuildUnrepaired(info);
        RenderImage(info);
      }

      // now that it has been rendered, offer it the selection — once, and only if there is something to
      // see. A picture joined mid-transfer and an ASCII slide both reach here with no bitmap; the slide
      // has its text instead, and the mid-transfer run has nothing until a later pass fills in its head.
      if (!info.Tracked && (info.Bitmap != null || product.Text != null))
      {
        info.Tracked = true;
        TrackNewNode(node);
      }

      // an accepted image fragment is real content: un-gray the pass entry the way a valid frame does
      if (!txPassInfo.HasValidFrame)
      {
        txPassInfo.HasValidFrame = true;
        passNode.ForeColor = Color.Empty;
      }

      if (!final) UpdateStatusLabel("DECODING...", Color.Green);

      if (treeView1.SelectedNode == node) DisplayImageInfo(info);
      else if (treeView1.SelectedNode == passNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));
    }

    // Re-render the node's currently displayed reconstruction — this pass's, or the combined one — and
    // dispose the bitmap it replaces, but only after the PictureBox has let go of it. A JPEG the OS decoder
    // refuses, which the first fragments of an image legitimately can be, leaves the previous rendering on
    // screen rather than blanking it, and leaves its bitmap undisposed.
    // That rule is for a picture filling in, where the next arrival can only improve what is on screen. It
    // is wrong when the reconstruction being shown is swapped for a different one, because then a refused
    // JPEG is the answer rather than a hiccup: turning Combine off on an image whose own pass never carried
    // a file header left the combined picture on screen, beside the text saying there was no picture. Such
    // a caller passes keepOnFailure: false and gets the empty pane that this pass alone actually has.
    private void RenderImage(SsdvImageInfo info, bool keepOnFailure = true)
    {
      var oldBitmap = info.Bitmap;
      var bitmap = DecodeJpeg(info.Product.Jpeg);
      if (bitmap != null || !keepOnFailure) info.Bitmap = bitmap;
      if (ImageBox.Image == oldBitmap) ImageBox.Image = info.Bitmap;
      if (bitmap != null || !keepOnFailure) oldBitmap?.Dispose();
    }

    // The received JPEG as a Bitmap. Copied out of the stream rather than handed the stream directly: GDI+
    // keeps a Bitmap's source stream open for the life of the bitmap, and this one is a MemoryStream over
    // a buffer that is replaced on the next fragment. Null when the file cannot be decoded at all.
    private static Bitmap? DecodeJpeg(byte[] jpeg)
    {
      try
      {
        using var stream = new MemoryStream(jpeg);
        using var decoded = System.Drawing.Image.FromStream(stream);
        return new Bitmap(decoded);
      }
      catch (Exception e)
      {
        Log.Debug(e, "Failed to decode a received JPEG image");
        return null;
      }
    }

    /// <summary>Auto-save the finalized image as JPEG + JSON metadata sidecar under the user data folder,
    /// beside the SSTV images. The bytes are written verbatim — they already are a JPEG file, gaps filled
    /// and EOI in place, whatever fraction of the image arrived.
    /// <para>Where the fragments carry their own CRC the sidecar also holds them, base64'd, which is what
    /// lets a later pass of the same picture be combined with this one. They are the fragments of
    /// <b>this</b> reception only, never of a combination — a sidecar that recorded a merge would be
    /// re-merged next time, and the archive would slowly stop meaning anything.</para></summary>
    private static SavedImage SaveImageToFile(ImageProduct product, DecodeSnapshot snapshot)
    {
      try
      {
        string folder = Path.Combine(Utils.GetUserDataFolder(), "SsdvImages");
        Directory.CreateDirectory(folder);
        string sat = string.Concat((snapshot.Satellite?.name ?? "Unknown").Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{sat}_{product.ImageId}.jpg");
        // only when there is a picture: emission withholds one whose JPEG header was lost, and writing
        // those bytes anyway is what put three files that open in no decoder into this folder
        string? jpegPath = null;
        if (product.Jpeg.Length > 0)
        {
          File.WriteAllBytes(path, product.Jpeg);
          jpegPath = path;
        }

        var meta = new
        {
          Utc = DateTime.UtcNow,
          Satellite = snapshot.Satellite?.name,
          Norad = snapshot.Satellite?.norad_cat_id,
          Transmitter = snapshot.Transmitter?.description,
          TransmitterUuid = snapshot.Transmitter?.uuid,
          product.ImageId,
          product.Source,
          product.Width,
          product.Height,
          product.FragmentsReceived,
          product.FragmentsExpected,
          product.FirstGapOffset,
          product.Complete
        };
        var json = JObject.FromObject(meta);
        if (product.FragmentFormat != null) WriteFragments(json, product);
        // the sidecar goes out either way — it is the record of the reception, and when there is no
        // picture it is the only thing worth keeping
        string sidecarPath = Path.ChangeExtension(path, ".json")!;
        File.WriteAllText(sidecarPath, json.ToString(Formatting.Indented));
        return new SavedImage(jpegPath, sidecarPath);
      }
      catch (Exception e)
      {
        Log.Error(e, "Failed to save the received image");
        return default;
      }
    }




    //----------------------------------------------------------------------------------------------
    //                                      codec2 voice
    //----------------------------------------------------------------------------------------------
    // called on the decode worker thread (or on the UI thread when a disposed decoder flushes); the
    // finalized message is auto-saved here, before marshaling, so a pass ending with the panel closing
    // cannot lose it. final says the assembler will send nothing further for this message.
    private void VoiceMessageHandler(VoiceProduct product, DecodeSnapshot snapshot,
      Dictionary<int, TreeNode> voiceNodes, bool final)
    {
      string? savedPath = final && ShouldSaveVoice(product) ? SaveVoiceToFile(product, snapshot) : null;
      BeginInvoke(() => ShowVoiceMessage(product, snapshot, voiceNodes, savedPath, final));
    }

    /// <summary>Whether a finalized message is worth writing to disk. The SSDV rule cannot be reused: there
    /// its two-fragment floor rests on each fragment having passed a CRC-32, so two of them agreeing on an
    /// image identity is not a coincidence that happens. <b>Voice frames carry no checksum at all</b> — no
    /// CRC, no FEC — so the only evidence a message is real is that several frames of exactly the right
    /// shape turned up close together, each numbered where the previous left off. Three is where that stops
    /// being explicable by chance, and it costs nothing real: a message the satellite actually sent runs 20
    /// to 37 sub-frames.
    /// <para>A rejected message is still shown in the tree and still playable. Only the automatic write is
    /// withheld, on the same principle as images: a stray node goes away with the session, a file in
    /// <c>Codec2Voice\</c> does not.</para></summary>
    private static bool ShouldSaveVoice(VoiceProduct product)
    {
      return product.SubFramesReceived >= MinSubFramesToSave && product.Wav.Length > 0;
    }

    private const int MinSubFramesToSave = 3;

    /// <summary>Auto-save the finalized message as WAV + JSON metadata sidecar under the user data folder,
    /// beside the SSTV and SSDV images. The bytes are written verbatim — they already are a playable file,
    /// gaps filled with silence, whatever fraction of the message arrived.</summary>
    private static string? SaveVoiceToFile(VoiceProduct product, DecodeSnapshot snapshot)
    {
      try
      {
        string folder = Path.Combine(Utils.GetUserDataFolder(), "Codec2Voice");
        Directory.CreateDirectory(folder);
        string sat = string.Concat((snapshot.Satellite?.name ?? "Unknown").Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{sat}_voice.wav");
        File.WriteAllBytes(path, product.Wav);

        var meta = new
        {
          Utc = DateTime.UtcNow,
          Satellite = snapshot.Satellite?.name,
          Norad = snapshot.Satellite?.norad_cat_id,
          Transmitter = snapshot.Transmitter?.description,
          TransmitterUuid = snapshot.Transmitter?.uuid,
          product.DurationSeconds,
          product.SampleRate,
          product.FirstNumber,
          product.LastNumber,
          product.SubFramesReceived,
          product.SubFramesExpected,
          product.Complete
        };
        File.WriteAllText(Path.ChangeExtension(path, ".json"),
          JObject.FromObject(meta).ToString(Formatting.Indented));
        return path;
      }
      catch (Exception e)
      {
        Log.Error(e, "Failed to save the received voice message");
        return null;
      }
    }

    private void ShowVoiceMessage(VoiceProduct product, DecodeSnapshot snapshot,
      Dictionary<int, TreeNode> voiceNodes, string? savedPath, bool final)
    {
      var (passNode, txPassInfo) = EnsureCurrentPassNode(snapshot);

      bool isNew = !voiceNodes.TryGetValue(product.FirstNumber, out TreeNode? node);
      if (isNew)
      {
        node = new TreeNode { ContextMenuStrip = VoiceMenu };
        node.Tag = new VoiceMessageInfo(snapshot, product);
        voiceNodes[product.FirstNumber] = node;
        AddLeaf(passNode, node);
        // from here on this pass's voice sub-frames are filed under this node — see AddFrame
        txPassInfo.LastVoiceNode = node;
      }

      var info = (VoiceMessageInfo)node!.Tag;
      info.Product = product;
      info.Final |= final;
      if (savedPath != null) info.SavedPath = savedPath;
      // no "of N": nothing on air says how long a message is, so the label counts what arrived and how long
      // the reconstruction plays, and claims nothing more
      node.Text = $"{ClockWidget.Stamp(info.FirstSeen, "HH:mm:ss")}  Voice  " +
        $"{product.SubFramesReceived} sub-frames, {product.DurationSeconds:0.0} s";

      // an accepted sub-frame is real content: un-gray the pass entry the way a valid frame does
      if (!txPassInfo.HasValidFrame)
      {
        txPassInfo.HasValidFrame = true;
        passNode.ForeColor = Color.Empty;
      }

      if (!final) UpdateStatusLabel("DECODING...", Color.Green);

      if (treeView1.SelectedNode == node) DisplayVoiceInfo(info);
      else if (treeView1.SelectedNode == passNode) richTextBox1.Text = txPassInfo.Describe(DescribeSignalParamsOrUnknown(txPassInfo.SignalParams));
    }

    // a voice node has no picture, so the right pane goes back to plain text rather than to the image
    // splitter, and the text itself becomes the play button (the FM transcript's interaction, minus the
    // per-line routing — a message is one clip, so anywhere in the pane means the same thing)
    private void DisplayVoiceInfo(VoiceMessageInfo info)
    {
      ShowTelemetryText();
      richTextBox1.Text = info.Describe();
    }

    // Play the message as it currently stands. Decoding the WAV rather than keeping a parallel float buffer
    // keeps one representation of the audio: the product's file IS the audio, and it is rebuilt from every
    // sub-frame received so far, so what plays is always what would be saved.
    private void PlayVoice(VoiceMessageInfo info)
    {
      var wav = info.Product.Wav;
      if (wav.Length <= WavHeaderBytes) return;

      var pcm = new float[(wav.Length - WavHeaderBytes) / 2];
      for (int i = 0; i < pcm.Length; i++)
        pcm[i] = BitConverter.ToInt16(wav, WavHeaderBytes + 2 * i) / 32768f;

      PlayAudioClip(pcm, info.Product.SampleRate);
    }

    // SkyTlm's WavWriter emits the canonical 44-byte RIFF/fmt/data header and nothing else
    private const int WavHeaderBytes = 44;

    private void VoiceMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      var info = treeView1.SelectedNode?.Tag as VoiceMessageInfo;
      OpenVoiceMNU.Enabled = info?.SavedPath != null && File.Exists(info.SavedPath);
    }

    private void PlayVoiceMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is VoiceMessageInfo info) PlayVoice(info);
    }

    private void SaveVoiceMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is not VoiceMessageInfo info) return;
      using var dlg = new SaveFileDialog { Filter = "WAV audio|*.wav", FileName = info.SaveFileName };
      if (dlg.ShowDialog() == DialogResult.OK) File.WriteAllBytes(dlg.FileName, info.Product.Wav);
    }

    private void OpenVoiceMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is not VoiceMessageInfo info || info.SavedPath == null) return;
      try
      {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(info.SavedPath) { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to open the voice message in a player");
      }
    }




    //----------------------------------------------------------------------------------------------
    //                              combine with previous passes
    //----------------------------------------------------------------------------------------------
    // Layout version of the archived fragments in a sidecar. A reader checks it and skips a file it does
    // not recognize, so a future change of format leaves old pictures readable by old builds and new ones
    // ignored by them, rather than misread by either.
    private const int SidecarFormat = 1;

    // How far back the archive is searched for earlier receptions of the picture on screen. The limit is
    // not a performance measure — it is the only defence there is against an SSDV image ID being reused.
    // The ID is 8 bits and satellites cycle it freely (HADES-SA sent 237, 238 and 239 twice within 92
    // minutes on 2026-08-03), so this bounds how far a wrong match can reach back, and no further.
    private static readonly TimeSpan CombineWindow = TimeSpan.FromDays(30);

    // one earlier reception of the same picture, read back from the sidecar that recorded it
    private sealed record ArchivedPass(string Path, DateTime Utc, IReadOnlyList<ImageFragment> Fragments);

    // Append this reception's fragments to its sidecar, base64'd, one object each so that a file can be
    // read by eye and a truncated one is visibly truncated. The correction count travels with the bytes
    // because it cannot be recovered from them: they are already repaired, and re-parsing reports them
    // clean. It is what ranks two copies of one fragment when the picture is rebuilt.
    private static void WriteFragments(JObject json, ImageProduct product)
    {
      json["Format"] = SidecarFormat;
      json["Variant"] = product.FragmentFormat;
      json["Packets"] = new JArray(product.Fragments.Select(f => new JObject
      {
        ["Id"] = f.Id,
        ["Corrected"] = f.CorrectedBytes,
        ["Data"] = Convert.ToBase64String(f.Bytes)
      }));
    }

    // Whether this image can be combined with anything, and the cue to have the candidates ready if it
    // can. An empty result is deliberately not cached: two passes of the same satellite in one session is
    // the case this feature exists for, so a sidecar written half an hour from now must be able to enable
    // an item that was grayed when the menu was last opened.
    private static bool CanCombine(SsdvImageInfo info)
    {
      if (info.PassProduct.FragmentFormat == null) return false;
      if (info.Archived == null || info.Archived.Count == 0) info.Archived = FindArchivedPasses(info);
      return info.Archived.Count > 0;
    }

    /// <summary>Earlier receptions of the picture on screen, newest first. The auto-saved file names carry
    /// everything the search needs to reject a file — <c>20260803_034123_HADES-SA_238.json</c> is a date,
    /// a satellite and an image ID — so the folder is filtered by name and only the few survivors are
    /// opened, which keeps the cost of a menu opening a directory listing however long the archive
    /// grows.</summary>
    private static List<ArchivedPass> FindArchivedPasses(SsdvImageInfo info)
    {
      var found = new List<ArchivedPass>();
      string folder = Path.Combine(Utils.GetUserDataFolder(), "SsdvImages");
      if (!Directory.Exists(folder)) return found;

      // this node's own sidecar is not an earlier pass; it is this one, written at finalization. Read
      // from SidecarPath rather than derived from SavedPath: since the two writes split, a reception can
      // have archived its fragments without having written a picture to hang the name off.
      string ownSidecar = info.SidecarPath ?? "";
      DateTime oldest = DateTime.Now - CombineWindow;

      try
      {
        foreach (string file in Directory.EnumerateFiles(folder, "*.json"))
        {
          if (string.Equals(file, ownSidecar, StringComparison.OrdinalIgnoreCase)) continue;
          if (!NameMatches(Path.GetFileNameWithoutExtension(file), info.PassProduct.ImageId, oldest)) continue;

          var pass = ReadArchivedPass(file, info);
          if (pass != null) found.Add(pass);
        }
      }
      catch (Exception e)
      {
        Log.Error(e, "Failed to search for earlier receptions of an image");
      }

      // newest first, which is the order the merge resolves ties in: of two equally clean copies of one
      // fragment the more recent reception is the likelier to be the same picture
      found.Sort((a, b) => b.Utc.CompareTo(a.Utc));
      return found;
    }

    // The date and image ID a saved file name carries, tested without opening it. The stamp is local time,
    // as DateTime.Now wrote it.
    private static bool NameMatches(string name, int imageId, DateTime oldest)
    {
      const string StampFormat = "yyyyMMdd_HHmmss";
      if (name.Length < StampFormat.Length) return false;

      int at = name.LastIndexOf('_');
      if (at < 0 || !int.TryParse(name.AsSpan(at + 1), out int id) || id != imageId) return false;

      return DateTime.TryParseExact(name.AsSpan(0, StampFormat.Length), StampFormat,
        CultureInfo.InvariantCulture, DateTimeStyles.None, out var stamp) && stamp >= oldest;
    }

    // One sidecar read back, or null when it is not an earlier reception of this picture after all: a
    // layout this build does not know, a different satellite, a different packet format, or no fragments
    // recorded at all (every sidecar written before this feature, and every one for a family whose
    // fragments cannot be verified on their own). A malformed file is skipped rather than fatal — the
    // archive is a folder the operator can edit, and one bad file must not cost the others.
    private static ArchivedPass? ReadArchivedPass(string file, SsdvImageInfo info)
    {
      try
      {
        var json = JObject.Parse(File.ReadAllText(file));
        if ((int?)json["Format"] != SidecarFormat) return null;
        if ((string?)json["Variant"] != info.PassProduct.FragmentFormat) return null;
        // A different satellite is normally a different picture, and for SSDV that is the end of it. The
        // Geoscan fleet is the exception: it transmits one shared playlist, and Geoscan-4 and Geoscan-5
        // sent the byte-identical file for every fnum they had in common on 2026-09-12/13. So a sibling's
        // sidecar is let through and RawJpegMerge validates it on the bytes — a reception that disagrees
        // anywhere it overlaps is dropped whole. If the playlist ever diverges that check fires on its
        // own, which is why there is no version negotiation here and no list of which birds match.
        if ((int?)json["Norad"] != info.Snapshot.Satellite?.norad_cat_id
            && info.PassProduct.FragmentFormat != RawJpegMerge.Format) return null;
        if (json["Packets"] is not JArray packets || packets.Count == 0) return null;

        var fragments = new List<ImageFragment>(packets.Count);
        foreach (var p in packets)
          fragments.Add(new ImageFragment(
            (int)p["Id"]!, Convert.FromBase64String((string)p["Data"]!), (int)p["Corrected"]!));

        return new ArchivedPass(file, (DateTime?)json["Utc"] ?? DateTime.MinValue, fragments);
      }
      catch (Exception e)
      {
        Log.Debug(e, "Skipped an unreadable image sidecar: {File}", file);
        return null;
      }
    }

    // Rebuild the combined reconstruction from this pass and the cached archived ones. The pass goes first
    // so that of two equally clean copies of a fragment the one just received is kept. A merge that yields
    // nothing leaves MergedProduct null, which shows this pass's picture rather than an empty one.
    private static void Recombine(SsdvImageInfo info)
    {
      var receptions = new List<IReadOnlyList<ImageFragment>> { info.PassProduct.Fragments };
      foreach (var pass in info.Archived!) receptions.Add(pass.Fragments);

      string? format = info.PassProduct.FragmentFormat;
      // The raw-JPEG merge needs the image ID and the sender handed to it: an SSDV packet repeats both
      // in every header, a raw-JPEG fragment is a byte range and repeats neither.
      info.MergedProduct = format == RawJpegMerge.Format
        ? RawJpegMerge.Build(receptions, format, info.PassProduct.ImageId, info.PassProduct.Source)
        : SsdvMerge.Build(receptions, format, info.PassProduct.Source);
    }

    // Rebuild the displayed reconstruction with the entropy repair switched off, from the same receptions
    // the repaired one was built from. There is no second copy to go back to and there deliberately is not
    // one: the repair happens during emission, and the fragments are the record, so either answer can be
    // produced from them at any time. Null for any family the raw-JPEG merge does not read, which is every
    // family the repair never ran on anyway.
    private static void RebuildUnrepaired(SsdvImageInfo info)
    {
      var receptions = new List<IReadOnlyList<ImageFragment>> { info.PassProduct.Fragments };
      if (info.Combined && info.Archived != null)
        foreach (var pass in info.Archived) receptions.Add(pass.Fragments);

      info.UnrepairedProduct = RawJpegMerge.Build(receptions, info.PassProduct.FragmentFormat,
        info.PassProduct.ImageId, info.PassProduct.Source, repair: false);
    }

    private void CombineImageMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is not SsdvImageInfo info) return;

      if (info.Combined) info.Combined = false;
      else
      {
        if (!CanCombine(info)) return;
        info.Combined = true;
        Recombine(info);
      }

      // the unrepaired rebuild is of the receptions now on display, so it is stale the moment that set
      // changes — exactly as MergedProduct is
      if (!info.Repaired) RebuildUnrepaired(info);

      RenderImage(info, keepOnFailure: false);
      DisplayImageInfo(info);
    }

    // The escape hatch for a resync the walker got wrong (B6): the repair is automatic and has no tunable,
    // so what the operator judges is the result rather than any parameter, and the switch is a plain
    // checkable item rather than a dialog. The auto-saved .jpg follows the display the way
    // DenoiseImageMNU_Click's PNG does, so the file on disk stays the picture that was accepted.
    private void RepairImageMNU_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Tag is not SsdvImageInfo info) return;

      info.Repaired = !info.Repaired;
      if (!info.Repaired)
      {
        RebuildUnrepaired(info);
        // nothing came of the rebuild, so there is no unrepaired picture to switch to. Put the switch back
        // rather than leave the repaired one on screen under an unchecked menu item.
        if (info.UnrepairedProduct == null) info.Repaired = true;
      }

      RenderImage(info, keepOnFailure: false);
      ResaveImage(info);
      DisplayImageInfo(info);
    }

    // Rewrite the auto-saved .jpg with the reconstruction now on display. The sidecar is not rewritten: it
    // records the fragments this pass heard, which no switch on screen changes.
    private static void ResaveImage(SsdvImageInfo info)
    {
      if (info.SavedPath == null || info.Product.Jpeg.Length == 0) return;

      try { File.WriteAllBytes(info.SavedPath, info.Product.Jpeg); }
      catch (Exception ex) { Log.Error(ex, "Failed to re-save the received image"); }
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
      if (CurrentVoice != null) { richTextBox1.Cursor = Cursors.Hand; return; }

      var (completed, pending) = FmLineAt(e.Location);
      richTextBox1.Cursor = completed != null || pending ? Cursors.Hand : Cursors.Default;
    }

    private void richTextBox1_MouseClick(object sender, MouseEventArgs e)
    {
      // the whole pane is the play button for a voice message, so there is no hit-testing to do — unlike
      // a transcript, which is a list of separately playable lines
      if (CurrentVoice != null) { PlayVoice(CurrentVoice); return; }

      var (completed, pending) = FmLineAt(e.Location);
      if (completed != null)
      {
        if (completed.Audio.Length > 0) PlayAudioClip(completed.Audio, CurrentFmTranscript!.SampleRate);
      }
      else if (pending && CurrentFmTranscript!.Pending is { } pendingLine)
      {
        // the open line isn't captured into an FmLineEntry until it closes, so fetch its audio-so-far
        // on demand from the retained decoder rather than pre-capturing it on every LineUpdated tick
        var audio = CurrentFmTranscript.Engine.GetAudio(pendingLine.StartSeconds, pendingLine.EndSeconds);
        if (audio.Length > 0) PlayAudioClip(audio, CurrentFmTranscript.SampleRate);
      }
    }

    // play one decoded mono clip — an FM transcript line, or a codec2 voice message — through the same
    // soundcard (device and gain) used for slicer output playback, replacing any clip already playing.
    // Shared rather than duplicated per source: the two things easy to get wrong here are honouring the
    // configured device and turning it back off afterwards, and both are solved once.
    private void PlayAudioClip(float[] audio, int sampleRate)
    {
      try
      {
        var resampled = sampleRate == SdrConst.AUDIO_SAMPLING_RATE ? audio : ResampleClip(audio, sampleRate);
        StopAudioClip();

        // mirror RecordingManager.StartPlayback: temporarily enable the shared speaker soundcard for the
        // duration of the clip if the user has speaker output turned off
        if (!ctx.SpeakerSoundcard.Enabled)
        {
          ctx.SpeakerSoundcard.Enabled = true;
          SpeakerEnabledForClip = true;
        }

        ctx.SpeakerSoundcard.AddSamples(resampled);

        if (SpeakerEnabledForClip)
        {
          int clipMs = (int)(1000.0 * resampled.Length / SdrConst.AUDIO_SAMPLING_RATE) + 300;
          ClipEndTimer = new System.Windows.Forms.Timer { Interval = Math.Max(clipMs, 1) };
          ClipEndTimer.Tick += ClipEndTimer_Tick;
          ClipEndTimer.Start();
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex, "FM clip playback failed");
      }
    }

    private static float[] ResampleClip(float[] audio, int sampleRate)
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

    private void ClipEndTimer_Tick(object? sender, EventArgs e)
    {
      StopAudioClip();
    }

    private void StopAudioClip()
    {
      if (ClipEndTimer != null)
      {
        ClipEndTimer.Stop();
        ClipEndTimer.Tick -= ClipEndTimer_Tick;
        ClipEndTimer.Dispose();
        ClipEndTimer = null;
      }

      ctx.SpeakerSoundcard.Buffer.Clear();

      if (SpeakerEnabledForClip)
      {
        ctx.SpeakerSoundcard.Enabled = false;
        SpeakerEnabledForClip = false;
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

      string identity = snapshot.Transmitter == null ? DescribeTerrestrial(snapshot.TerrestrialHz)
        : $"Sat: {snapshot.Transmitter.Satellite.name}  Tx: \"{snapshot.Transmitter.description}\"  Uuid: {snapshot.Transmitter.uuid}";
      string header = $"{identity}  Frame: {frame.Length} bytes" +
        (addr.Length > 0 ? $"  Addr: {addr}" : "");
      FrameLogger.Information("{Header}\n{Body}", header, frameText);
    }

    private static ILogger CreateFrameLogger()
    {
      string fileName = Path.Combine(Utils.GetUserDataFolder(), "TelemetryDecodes", "frames_.txt");
      // both zones, so that a decode can be matched against the UTC-stamped system log and
      // against the local-time file names of the images saved from the same pass
      return new LoggerConfiguration()
        .Enrich.With<UtcTimeEnricher>()
        .WriteTo.File(fileName,
          rollingInterval: RollingInterval.Day,
          outputTemplate: "{Utc:yyyy-MM-dd HH:mm:ss}Z  ({Timestamp:HH:mm:ss} LT)  {Message:lj}{NewLine}",
          shared: true)
        .CreateLogger();
    }

    internal void UpdateTxStatus()
    {
      bool wasAbove = SatAboveHorizon;
      // the horizon is an instantaneous property, so read the elevation rather than search for a pass to test:
      // a search started at "now" is blind to the last seconds of the pass it is already inside (see
      // GetCurrentOrNextPass) and would report LOS ~10 s early, tearing the pipeline down before the pass ends.
      // this also keeps a 1-Hz tick from re-predicting a day of passes on the UI thread
      SatAboveHorizon = ctx.SdrPasses.ObserveSatellite(Satellite, DateTime.UtcNow)?.Elevation.Degrees > 0;
      // LOS ends a running search (§4.6a): no further burst can arrive, so the session must not be left
      // running with the progress line sitting on "waiting" until the operator closes the dialog
      if (!Terrestrial && !SatAboveHorizon && Discovery != null) StopDiscoveryAtLos();
      // LOS also ends the evidence (§4.10). The parameters and their green dots survive — they are still the
      // parameters in use and they still worked — but a frame from last week's pass plus one from this one is
      // not "two frames in a row with these parameters", so a Save left unclicked needs two fresh frames.
      // An override already saved is untouched: that is a database correction, not a per-pass finding.
      if (wasAbove && !SatAboveHorizon && ConfirmingFrames > 0)
      {
        ConfirmingFrames = 0;
        UpdateGearButton();
        ParamsDialog?.ShowConfirmingFrames(ConfirmingFrames, ConfirmFrames);
      }
      CreatDestroyPipeline();

      // §4: no parameter editing, no Discover and no Save-to-override for a transmitter the user did not
      // select, so the gear disappears while a sibling drives telemetry. To correct a wrong rank pick the
      // operator selects that telemetry transmitter, which re-adds the SSTV branch anyway (§2.2 row 1).
      SettingsButton.Visible = Sibling == null;

      // terrestrial is no longer a refusal: with parameters it decodes like any other signal (§4.9), so
      // only the state before the operator has entered any is reported as such
      if (Terrestrial && SignalParams == null) UpdateStatusLabel("terrestrial, signal parameters not set", Color.Red);
      else if (!IsDecodable()) UpdateStatusLabel(DescribeUnsupported(), Color.Red);
      // an FM-only transmitter with no FM artefact unzipped into the installation folder reads as
      // unsupported, silently - the user installs it manually, there is no in-app prompt or download
      else if (IsFmDecodable() && !IsTelemetryDecodable() && !IsSstvDecodable() && !FmModelPresent)
        UpdateStatusLabel(DescribeUnsupported(), Color.Red);
      else if (!Terrestrial && !SatAboveHorizon) UpdateStatusLabel("satellite below horizon", SystemColors.ControlText);
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
      // Imaging is a consumer of the telemetry frames, not a decoder of its own, and it is built on every
      // satellite whose framing has an assembler — 100-odd USP birds that have never sent a picture
      // included. Naming it there would be noise, so it is named only where the operator asked for it by
      // selecting the SSDV row and the parameters it resolved to would otherwise not mention it.
      if (IsSsdvDecodable() && IsImagingWanted()) names.Add("images");
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

    // a right-click does not select in a TreeView, but every context menu here — the tree's own, and the
    // image and voice menus attached to the leaf nodes — reads treeView1.SelectedNode, so the node under
    // the cursor is selected first. MouseDown, not NodeMouseClick, because the menu opens on mouse up.
    private void treeView1_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Right) return;
      var node = treeView1.GetNodeAt(e.X, e.Y);
      if (node != null) treeView1.SelectedNode = node;
    }

    private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
    {
      var node = e.Node;
      if (node == null) return;

      // showing non-FM content clears the click-to-play routing; RenderFmTranscript re-arms it for an FM node
      CurrentFmTranscript = null;
      // and the voice routing, which the same click handler serves — only one of the two can be armed
      CurrentVoice = null;

      if (node.Tag is IImageNodeInfo imageInfo)
      {
        DisplayImageInfo(imageInfo);
        return;
      }

      if (node.Tag is VoiceMessageInfo voiceInfo)
      {
        CurrentVoice = voiceInfo;
        DisplayVoiceInfo(voiceInfo);
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
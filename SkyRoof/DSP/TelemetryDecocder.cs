using MathNet.Numerics;
using Serilog;
using VE3NEA;
using VE3NEA.SkyFM;
using VE3NEA.SkySSTV;
using VE3NEA.SkyTlm.Audio;
using VE3NEA.SkyTlm.Core;
using VE3NEA.SkyTlm.Imaging;

namespace SkyRoof
{
  public class TelemetryDecocder : ThreadedProcessor<Complex32>
  {
    public StreamingPipeline? Pipeline;
    public StreamingPipeline? Detector;
    public SstvDecoder? Sstv;
    public SkySpeechDecoder? Fm;
    // the image assembler for this transmitter's frames (SSDV packets or raw-JPEG byte ranges), or null
    // when the satellite sends no images we can reconstruct. Unlike the branches above it consumes FRAMES,
    // not samples, so nothing feeds it here — the panel's frame handler does (see TelemetryPanel).
    public IImageAssembler? Images;
    // the voice assembler for this transmitter's frames (codec2 sub-frames), or null when the satellite
    // sends no audio. Fed from the same frame handler as Images, and for the same reason: voice rides the
    // telemetry frames rather than arriving on a transmitter of its own.
    public IAudioAssembler? Voice;

    // Detection-only options for the discovery burst source: segment the stream and hand the samples over,
    // but never demodulate or deframe. The shape gate is off because the template it would gate on is built
    // from parameters the search exists to replace — gating on them would discard the very bursts it needs.
    private static readonly StreamingOptions DetectOnlyOptions = new()
    {
      DetectOnly = true,
      GateByShape = false
    };

    // mixed-mode dispatch: one transmitter may alternate FSK telemetry and SSTV in a pass
    // (UmKA-1), so both decoders may run concurrently and self-gate — FSK bursts fail the SSTV VIS/sync
    // test, SSTV segments present no valid FSK frames. The caller decides which decoders to build. The FM
    // speech decoder is built only when an engine is supplied (an FM transmitter with the model
    // downloaded); the engine is SHARED across transmitter changes and is not owned here.
    // detectParams builds the detection-only pipeline parameter discovery falls back to when this
    // transmitter has no telemetry decode of its own to take bursts from (CW/SSTV/FM, unsupported format).
    // Imaging has no gate of its own: images ride the telemetry frames, so the assembler is built whenever
    // the telemetry branch is, and the factory answers with null for a satellite that sends none.
    public TelemetryDecocder(SignalParams signalParams, int? noradId, bool telemetry, bool sstv, IAsrEngine? fmEngine,
      SignalParams? detectParams = null)
    {
      if (telemetry)
      {
        Pipeline = new StreamingPipeline(signalParams);
        Images = ImageAssemblerFactory.Create(signalParams, noradId);
        // Unlike the image assembler this one binds a native library (codec2.dll) the moment it is built,
        // and throws if it is missing or unusable. That must not cost the pass its telemetry: voice is an
        // extra payload type on this downlink, so a broken install loses the audio and nothing else.
        try
        {
          Voice = AudioAssemblerFactory.Create(signalParams, noradId);
        }
        catch (Exception e)
        {
          Log.Error(e, "Voice decoding is unavailable; telemetry continues without it");
        }
      }
      if (detectParams != null) Detector = new StreamingPipeline(detectParams, DetectOnlyOptions);
      if (sstv) Sstv = new SstvDecoder();
      if (fmEngine != null) Fm = new SkySpeechDecoder(fmEngine);
    }

    protected override void Process(DataEventArgs<Complex32> args)
    {
      Pipeline?.Push(args.Data);
      Detector?.Push(args.Data);
      Sstv?.Process(args.Data.AsSpan(0, args.Count));
      Fm?.Process(args.Data.AsSpan(0, args.Count));
    }

    public override void Dispose()
    {
      // stop and join the worker thread BEFORE freeing the pipeline's native FFTW memory, otherwise an
      // in-flight Push() on that thread runs an FFT against freed buffers and corrupts the native heap.
      base.Dispose();
      Pipeline?.Dispose();
      Pipeline = null;
      Detector?.Dispose();
      Detector = null;

      // drain the image assembler so the picture still being received is finalized (ImageCompleted fires
      // with the fragments that arrived before LOS / the transmitter switch). A pass almost never ends on
      // an image boundary — off air the normal case is an image 12 packets of 15 complete — so without
      // this the last image of every pass would be lost rather than saved.
      Images?.Flush();
      Images = null;

      // and the voice assembler, for the same reason and more urgently: a voice message carries no length,
      // so nothing but this flush can ever declare one over. Without it the recording still being received
      // at LOS would not merely be unsaved, it would never be announced at all.
      Voice?.Flush();
      (Voice as IDisposable)?.Dispose();
      Voice = null;

      // drain the decoder so the partially received image is finalized (ImageCompleted fires with the
      // rows decoded before LOS / the transmitter switch), then free its filter chains
      Sstv?.Flush();
      Sstv?.Dispose();
      Sstv = null;

      // drain the FM speech decoder so the last open transcript line closes (LineCompleted fires), then
      // free its front-end; the shared engine is left alive for the next transmitter's decoder.
      Fm?.Flush();
      Fm?.Dispose();
      Fm = null;
    }
  }
}

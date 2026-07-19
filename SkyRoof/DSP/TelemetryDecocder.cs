using MathNet.Numerics;
using VE3NEA;
using VE3NEA.SkyFM;
using VE3NEA.SkySSTV;
using VE3NEA.SkyTlm.Core;

namespace SkyRoof
{
  public class TelemetryDecocder : ThreadedProcessor<Complex32>
  {
    public StreamingPipeline? Pipeline;
    public SstvDecoder? Sstv;
    public SkySpeechDecoder? Fm;

    // mixed-mode dispatch: one transmitter may alternate FSK telemetry and SSTV in a pass
    // (UmKA-1), so both decoders may run concurrently and self-gate — FSK bursts fail the SSTV VIS/sync
    // test, SSTV segments present no valid FSK frames. The caller decides which decoders to build. The FM
    // speech decoder is built only when an engine is supplied (an FM transmitter with the model
    // downloaded); the engine is SHARED across transmitter changes and is not owned here.
    public TelemetryDecocder(SignalParams signalParams, bool telemetry, bool sstv, IAsrEngine? fmEngine)
    {
      if (telemetry) Pipeline = new StreamingPipeline(signalParams);
      if (sstv) Sstv = new SstvDecoder();
      if (fmEngine != null) Fm = new SkySpeechDecoder(fmEngine);
    }

    protected override void Process(DataEventArgs<Complex32> args)
    {
      Pipeline?.Push(args.Data);
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

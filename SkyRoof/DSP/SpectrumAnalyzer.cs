using MathNet.Numerics.Statistics;
using VE3NEA;

namespace SkyRoof
{
  public class SpectrumAnalyzer<T> : ThreadedProcessor<T>
  {
    public Spectrum<T>? Spectrum;
    public float Median;
    public event EventHandler<DataEventArgs<float>>? SpectrumAvailable;

    public SpectrumAnalyzer(int size, int step, int outputSize = 0) : base()
    {
      Spectrum = new(size, step, 3, outputSize);
      Spectrum.SpectrumAvailable += Spectrum_SpectrumAvailable;
    }

    public override void Dispose()
    {
      base.Dispose();
      Spectrum?.Dispose();
      Spectrum = null;
    }

    protected override void Process(DataEventArgs<T> args)    
    {
      Spectrum?.Process(args);
    }

    private void Spectrum_SpectrumAvailable(object? sender, DataEventArgs<float> e)
    {
      if (Spectrum == null) return;
      Median = FilterMedian(Spectrum.FastMedian);
      SpectrumAvailable?.Invoke(this, e);
    }

    float[] mdnBuf = new float[11];
    float[] mdnBuf2 = new float[11];
    int mdnIdx;
    float filt;

    private float FilterMedian(float mdn)
    {
      // eliminate short spikes
      if (++mdnIdx == mdnBuf.Length) mdnIdx = 0;
      mdnBuf[mdnIdx] = mdn;
      Array.Copy(mdnBuf, mdnBuf2, mdnBuf.Length);
      mdn = ArrayStatistics.PercentileInplace(mdnBuf2, 50);

      // smooth
      filt = 0.8f * filt + 0.2f * mdn;
      return filt;
    }
  }
}

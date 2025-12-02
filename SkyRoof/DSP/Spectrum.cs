using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using VE3NEA;

namespace SkyRoof
{
  public class Spectrum<T> : IDisposable
  {
    private readonly int FftSize;
    private double step;
    private readonly int PolyphaseSegmentCount;

    private readonly Fft<T> Fft;
    private readonly int BufferLength;
    private readonly float[] Window;
    private readonly float[] PowerSpectrum, AverageSpectrum, MedianComputationBuffer;
    private T[] Buffer;
    private int WritePos;
    private int FftStep;
    private double SamplesSinceStep;
    private int SpectraSinceStep;
    private DataEventArgs<float> Args = new();

    public double Step { get => step; set => SetStep(value); }

    public float FastMedian;

    public event EventHandler<DataEventArgs<float>>? SpectrumAvailable;


    public Spectrum(int size, int step, int polyhpaseSegmentCount, int outputSize)
    {
      FftSize = size;
      PolyphaseSegmentCount = polyhpaseSegmentCount;
      
      Fft = new(size);
      BufferLength = Fft.InputData.Length * PolyphaseSegmentCount;
      Buffer = new T[BufferLength];

      if (outputSize == 0) outputSize = Fft.OutputData.Length;
      PowerSpectrum = new float[outputSize];
      AverageSpectrum = new float[outputSize];
      MedianComputationBuffer = new float[outputSize];
      Window = Dsp.BlackmanWindow(BufferLength);
      Dsp.Normalize(Window);
      
      SetStep(step);
    }

    public void Dispose()
    {
      Fft.Dispose();
    }

    private void SetStep(double value)
    {
      step = value;

      int maxFftStep = BufferLength / 2;
      int spectraPerStep = Math.Max(1, (int)Math.Round(step / maxFftStep));
      FftStep = (int)Math.Round(step / spectraPerStep);

      SamplesSinceStep = 0;
      SpectraSinceStep = 0;
    }


    public void Process(DataEventArgs<T> args)
    {

      int readPos = 0;

      while (readPos < args.Count)
      {
        int copyCount = Math.Min(BufferLength - WritePos, args.Count - readPos);
        Array.Copy(args.Data, readPos, Buffer, WritePos, copyCount);
        readPos += copyCount;
        WritePos += copyCount;

        if (WritePos == BufferLength)
        {
          ComputeSpectrum();
          AverageSpectra();

          WritePos -= FftStep;
          Array.Copy(Buffer, FftStep, Buffer, 0, WritePos);
        }
      }
    }

    private void ComputeSpectrum()
    {
      AppplyWindow();
      Fft.Execute();

      if (typeof(T) == typeof(float))
        for (int i = 0; i < PowerSpectrum.Length; i++)
          PowerSpectrum[i] = Fft.OutputData[i].MagnitudeSquared;
      else
      {
        // compute power
        int mid = FftSize / 2;
        for (int i = 0; i < PowerSpectrum.Length / 2; i++)
        {
          PowerSpectrum[i] = Fft.OutputData[mid + i].MagnitudeSquared;
          PowerSpectrum[mid + i] = Fft.OutputData[i].MagnitudeSquared;
        }
      }
    }

    private void AverageSpectra()
    {
      if (SpectraSinceStep == 0)
        Array.Copy(PowerSpectrum, AverageSpectrum, PowerSpectrum.Length);
      else
        for (int i = 0; i < PowerSpectrum.Length; i++)
          AverageSpectrum[i] += PowerSpectrum[i];

      SpectraSinceStep++;
      SamplesSinceStep += FftStep;

      if (SamplesSinceStep >= Step)
      {
        NormalizeAverageSpectrum();
        Args.Data = AverageSpectrum;
        SpectrumAvailable?.Invoke(this, Args);

        SpectraSinceStep = 0;
        SamplesSinceStep -= Step;
      }
    }

    private void NormalizeAverageSpectrum()
    {
      var offset = FastMedian = ComputeMedian(AverageSpectrum);
      if (offset > 0) offset = (float)Math.Log(offset);

      for (int i = 0; i < PowerSpectrum.Length; i++)
        AverageSpectrum[i] = (float)(Math.Log(AverageSpectrum[i] + 1e-30) - offset);
    }

    private float ComputeMedian(float[] data)
    {
      Array.Copy(data, MedianComputationBuffer, data.Length);
      return ArrayStatistics.PercentileInplace(MedianComputationBuffer, 50);
    }

    // generic arithmetic is a pain. originally it was one line:
    // for (int i = 0; i < BufferLength; i++) Fft.InputData[i] = Buffer[i] * Window[i];
    private unsafe void AppplyWindow()
    {
      Array.Clear(Fft.InputData);
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
      fixed (void* pInBuffer = Buffer)
      fixed (void* pOutBuffer = Fft.InputData)
      fixed (float* pWindow = Window)
      {
        int dst = 0;
        if (typeof(T) == typeof(float))
          for (int src = 0; src < BufferLength; src++)
          {
            ((float*)pOutBuffer)[dst] += ((float*)pInBuffer)[src] * pWindow[src];
            if (++dst == Fft.InputData.Length) dst = 0;
          }

        else if (typeof(T) == typeof(Complex32))
          for (int i = 0; i < BufferLength; i++)
          {
            ((Complex32*)pOutBuffer)[dst] += ((Complex32*)pInBuffer)[i] * pWindow[i];
            if (++dst == Fft.InputData.Length) dst = 0;
          }
#pragma warning restore CS8500
      }
    }
  }
}
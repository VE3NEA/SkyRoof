using CSCore;
using MathNet.Numerics;

namespace VE3NEA
{
  // <T> is float or Complex32
  public class WaveSource<T> : IWaveSource
  {
    private readonly WaveFormat format;
    private RingBuffer<T> ringBuffer;

    public WaveSource(int samplingRate)
    {
      int channelCount = typeof(T) == typeof(Complex32) ? 2 : 1;
      format = new WaveFormat(samplingRate, 32, channelCount, AudioEncoding.IeeeFloat);
      ringBuffer = new(samplingRate / 5);
    }

  public void AddSamples(T[] samples, int offset = 0, int? count = null)
    {
      ringBuffer.Write(samples, offset, count ?? samples.Length);
    }

    public void ClearBuffer()
    {
      ringBuffer.Clear(); 
    }




    //------------------------------------------------------------------------------------------------------------------
    //                                             IWaveSource
    //------------------------------------------------------------------------------------------------------------------
    public bool CanSeek => false;
    public WaveFormat WaveFormat => format;
    public long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public long Length => throw new NotImplementedException();
    public int Read(byte[] buffer, int offset, int count)
    {
      ringBuffer.ReadBytes(buffer, offset, count);
      return count;
    }




    //------------------------------------------------------------------------------------------------------------------
    //                                             IDisposable
    //------------------------------------------------------------------------------------------------------------------
    public void Dispose()
    {
      // nothing to dispose of
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VE3NEA
{
  public static class SoapySdrHelper
  {

    public static string[] MarshalStringArray(IntPtr ptr, nint count)
    {
      if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<string>();

      var result = new string[count];

      for (int i = 0; i < count; i++)
      {
        IntPtr strPtr = Marshal.ReadIntPtr(ptr, i * IntPtr.Size);
        result[i] = Marshal.PtrToStringAnsi(strPtr);
      }
      return result;
    }

    public static NativeSoapySdr.SoapySDRRange[] MarshalRangeArray(IntPtr resultPtr, nint length)
    {
      var result = new NativeSoapySdr.SoapySDRRange[length];
      int structSize = Marshal.SizeOf(typeof(NativeSoapySdr.SoapySDRRange));
      for (int i = 0; i < length; i++)
      {
        IntPtr ptr = IntPtr.Add(resultPtr, i * structSize);
        result[i] = Marshal.PtrToStructure<NativeSoapySdr.SoapySDRRange>(ptr);
      }
      return result;
    }

    internal static SoapySdrArgs[] MarshalKwArgsArray(nint resultPtr, nint length)
    {
      var result = new SoapySdrArgs[length];

      int structSize = Marshal.SizeOf(typeof(NativeSoapySdr.SoapySDRKwargs));
      for (int i = 0; i < length; i++)
      {
        IntPtr ptr = IntPtr.Add(resultPtr, i * structSize);
        NativeSoapySdr.SoapySDRKwargs nativeKwargs = Marshal.PtrToStructure<NativeSoapySdr.SoapySDRKwargs>(ptr);
        result[i] = SoapySdrArgs.FromNative(nativeKwargs);
      }

      NativeSoapySdr.SoapySDRKwargsList_clear(resultPtr, length);
      SoapySdr.CheckError();
      return result;
    }

    internal static Dictionary<string, string> MarshalOptions(IntPtr keysPtr, IntPtr valuesPtr, nint count)
    {
      var result = new Dictionary<string, string>();
      if (count == 0) return result;

      string[] values = valuesPtr == IntPtr.Zero ? Array.Empty<string>() : MarshalStringArray(valuesPtr, count);
      string[] keys = keysPtr == IntPtr.Zero ? values : MarshalStringArray(keysPtr, count);
      for (int i = 0; i < count; i++) result[keys[i]] = values[i];
      return result;
    }

    internal static SoapySDRArgInfo[] MarshalArgsInfoArray(IntPtr ptr, nint count)
    {
      var result = new SoapySDRArgInfo[count];
      var structSize = Marshal.SizeOf(typeof(NativeSoapySdr.SoapySDRArgInfo));

      for (int i = 0; i < count; i++)
        result[i] = SoapySDRArgInfo.FromNative(ptr + structSize * i);

      return result;
    }
  }
}

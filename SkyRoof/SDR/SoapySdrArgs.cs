using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VE3NEA
{
  public class SoapySDRKwargs : Dictionary<string, string>
  {
    public static SoapySDRKwargs FromNative(NativeSoapySdr.SoapySDRKwargs native)
    {
      var kwargs = new SoapySDRKwargs();
      string[] keys = SoapySdrHelper.MarshalStringArray(native.keys, native.size);
      string[] values = SoapySdrHelper.MarshalStringArray(native.vals, native.size);

      for (int i = 0; i < keys.Length; i++) kwargs[keys[i]] = values[i];

      return kwargs;
    }

    public IntPtr ToNative()
    {
      var native = new NativeSoapySdr.SoapySDRKwargs();
      native.size = Count;

      if (Count > 0)
      {
        IntPtr[] keyPtrs = new IntPtr[Count];
        IntPtr[] valuePtrs = new IntPtr[Count];

        int index = 0;
        foreach (var kvp in this)
        {
          keyPtrs[index] = Marshal.StringToHGlobalAnsi(kvp.Key);
          valuePtrs[index] = Marshal.StringToHGlobalAnsi(kvp.Value);
          index++;
        }

        native.keys = Marshal.UnsafeAddrOfPinnedArrayElement(keyPtrs, 0);
        native.vals = Marshal.UnsafeAddrOfPinnedArrayElement(valuePtrs, 0);
      }

      int structSize = Marshal.SizeOf(typeof(NativeSoapySdr.SoapySDRKwargs));
      IntPtr result = Marshal.AllocHGlobal(structSize);
      Marshal.StructureToPtr(native, result, true);
      return result;
    }
  }
}
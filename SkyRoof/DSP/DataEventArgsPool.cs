using System.Collections.Concurrent;
using VE3NEA;

namespace VE3NEA
{
  // DataEventArgs are instantiated many times per second. Pool them to reduce GC


  //------------------------------------------------------------------------------------------------------------------
  //                                      pool of DataEventArgs<T>
  //------------------------------------------------------------------------------------------------------------------
  internal class DataEventArgsPool<T>
  {
    private ConcurrentDictionary<int, FixedSizeArgsPool<T>> Pools = new();
    public DataEventArgs<T> Rent(int dataSize)
    {
      if (!Pools.ContainsKey(dataSize)) Pools[dataSize] = new(dataSize);
      return Pools[dataSize].Rent();
    }

    public DataEventArgs<T> RentCopyOf(DataEventArgs<T> args)
    {
      var result = Rent(args.Data.Length);

      result.Count = args.Count;
      result.Utc = args.Utc;
      Array.Copy(args.Data, result.Data, result.Count);

      return result;
    }

      public void Return(DataEventArgs<T> args)
    {
      Pools[args.Data.Length].Return(args);
    }




    //------------------------------------------------------------------------------------------------------------------
    //                        pool of DataEventArgs<T> where e.Data has certain size
    //------------------------------------------------------------------------------------------------------------------
    private class FixedSizeArgsPool<T>
    {
      private int DataSize;
      private ConcurrentBag<DataEventArgs<T>> Pool = new();

      public FixedSizeArgsPool(int dataSize)
      {
        DataSize = dataSize;
      }

      internal DataEventArgs<T> Rent()
      {
        if (Pool.TryTake(out DataEventArgs<T>? result))
        {
          result.Utc = DateTime.UtcNow;
          return result;
        }

        return new DataEventArgs<T>(new T[DataSize], DataSize);
      }

      internal void Return(DataEventArgs<T> args)
      {
        Pool.Add(args);
      }
    }
  }
}

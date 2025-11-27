namespace VE3NEA
{
  public class DataEventArgs<T> : EventArgs
  {
    public T[] Data = Array.Empty<T>();
    public int Count;
    public DateTime Utc;

    public DataEventArgs() { }

    public DataEventArgs(T[] data, int count)
    {
      Data = data;
      Count = count;
      Utc = DateTime.UtcNow;
    }
  }
}
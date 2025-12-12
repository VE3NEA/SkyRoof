using System.Collections.Concurrent;
using Serilog;

namespace VE3NEA
{
  public abstract class ThreadedProcessor<T> : IDisposable
  {
    private EventWaitHandle wakeupEvent;
    public Thread processingThread;
    private bool stopping = false;
    private DataEventArgsPool<T> ArgsPool = new();
    protected ConcurrentQueue<DataEventArgs<T>> Queue = new();

    public bool Enabled = true;


    public ThreadedProcessor()
    {
      wakeupEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

      processingThread = new Thread(new ThreadStart(ProcessingThreadProcedure));
      processingThread.IsBackground = true;
      processingThread.Name = GetType().Name;
      processingThread.Start();
      processingThread.Priority = ThreadPriority.Highest;
    }

    public virtual void Dispose()
    {
      if (stopping) return;
      stopping = true;
      wakeupEvent.Set();
      processingThread.Join();
      Queue.Clear();
      wakeupEvent.Dispose();
    }

    public void StartProcessing()
    {
      wakeupEvent.Set();
    }

    public void StartProcessing(DataEventArgs<T> args)
    {
      if (!Enabled) return;

      Queue.Enqueue(ArgsPool.RentCopyOf(args));
      StartProcessing();
    }

    private void ProcessingThreadProcedure()
    {
      while (true)
      {
        wakeupEvent.WaitOne();
        if (stopping) break;

        try
        {
          // safety valve
          if (Queue.Count > 50)
            while (Queue.TryDequeue(out DataEventArgs<T> args))
              ArgsPool.Return(args);

          while (Queue.Any())
          {
            Queue.TryDequeue(out DataEventArgs<T> args);
            Process(args);
            ArgsPool.Return(args);
          }
        }
        catch (Exception e)
        {
          Log.Error(e, $"Error in {GetType().Name}");
        }
      }
    }

    protected abstract void Process(DataEventArgs<T> args);
  }
}

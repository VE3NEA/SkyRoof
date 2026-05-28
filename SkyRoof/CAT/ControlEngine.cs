using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace SkyRoof
{
  public abstract class ControlEngine : IDisposable
  {
    protected SynchronizationContext syncContext = SynchronizationContext.Current!;

    public readonly string Host;
    protected readonly ushort Port;
    protected readonly int Delay;
    protected readonly bool log;

    protected Thread? processingThread;
    protected TcpClient? TcpClient;
    protected bool stopping = false;
    protected bool ErrorLogged = false;
    private readonly int sendTimeout;
    private readonly int receiveTimeout;
    private readonly int reconnectDelay;
    private readonly ManualResetEventSlim stopEvent = new ManualResetEventSlim(false);

    public event EventHandler? StatusChanged;
    public bool IsRunning {get; private set;}


    public ControlEngine(string host, ushort port, IControlEngineSettings settings)
    {
      Host = host;
      Port = port;
      Delay = settings.Delay;
      log = settings.LogTraffic;
      sendTimeout = settings.SendTimeout;
      receiveTimeout = settings.ReceiveTimeout;
      reconnectDelay = settings.ReconnectDelay;
    }




    //----------------------------------------------------------------------------------------------
    //                                        thread 
    //----------------------------------------------------------------------------------------------
    internal void Retry()
    {
      if (processingThread == null) StartThread();
    }
    
    protected void StartThread()
    {
      if (processingThread != null) return;

      stopping = false;
      stopEvent.Reset();
      processingThread = new Thread(new ThreadStart(ThreadProcedure));
      processingThread.IsBackground = true;
      processingThread.Name = GetType().Name;
      processingThread.Start();
    }

    protected void StopThread()
    {
      if (stopping) return;
      stopping = true;
      stopEvent.Set();
      processingThread?.Join();
      processingThread = null;
    }

    private void ThreadProcedure()
    {
      processingThread!.Priority = ThreadPriority.Highest;

      bool lastReportedRunning = false;
      bool everConnected = false;

      while (!stopping)
      {
        if (Connect() && Setup())
        {
          IsRunning = true;
          ErrorLogged = false;
          everConnected = true;
          if (!lastReportedRunning) { lastReportedRunning = true; OnStatusChanged(); }

          while (!stopping && TcpClient != null && TcpClient.Connected)
            try
            {
              Cycle();
              Thread.Sleep(Delay);
            }
            catch (SocketException ex)
            {
              Log.Error(ex, $"Socket error in {GetType().Name}");
              break;
            }
            catch (Exception ex)
            {
              Log.Error(ex, $"Error in {GetType().Name}");
            }
        }

        Disconnect();
        IsRunning = false;
        if (lastReportedRunning) { lastReportedRunning = false; OnStatusChanged(); }

        if (!stopping)
        {
          string state = everConnected ? "connection lost" : "initial connection failed";
          Log.Information($"{GetType().Name}: {state}, retrying in {reconnectDelay / 1000} seconds");
          stopEvent.Wait(reconnectDelay);
        }
      }

      processingThread = null;
    }

    protected abstract bool Setup();
    protected abstract void Cycle();




    //----------------------------------------------------------------------------------------------
    //                                    connection
    //----------------------------------------------------------------------------------------------
    private bool Connect()
    {
      TcpClient = new();
      TcpClient.SendTimeout = sendTimeout;
      TcpClient.ReceiveTimeout = receiveTimeout;

      try
      {
        Log.Information($"Connecting to {Host}:{Port}");
        TcpClient!.Connect(Host, Port);
        Log.Information($"Connected to {Host}:{Port}");
        return true;
      }
      catch (SocketException ex)
      {
        if (!ErrorLogged)
        {
          ErrorLogged = true;
          Log.Error(ex, $"Unable to connect to {Host}:{Port}");
        }
        return false;
      }
    }

    protected void Disconnect()
    {
      try
      {
        if (TcpClient != null)
        {
          Log.Information($"Disconnecting from {Host}:{Port}");
          if (TcpClient.Connected) TcpClient.Client.Shutdown(SocketShutdown.Both);
          TcpClient.Close();
          Log.Information($"Disconnected from {Host}:{Port}");
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Error while disconnecting from {Host}:{Port}");
      }
      finally
      {
        TcpClient?.Dispose(); // Ensure resources are released
        TcpClient = null; // Reset the TcpClient reference
      }
    }




    //----------------------------------------------------------------------------------------------
    //                                       status
    //----------------------------------------------------------------------------------------------

    // send notification to the UI thread asynchronously
    protected void OnStatusChanged()
    {
      syncContext.Post(s => StatusChanged?.Invoke(this, EventArgs.Empty), null);
    }

    public string GetStatusString()
    {
      return IsRunning ? "Connected" : "Not connected";
    }




    //----------------------------------------------------------------------------------------------
    //                                       commands
    //----------------------------------------------------------------------------------------------
    protected bool SendWriteCommands(string[]? commands)
    {
      bool ok = true;

      foreach (string cmd in commands!)
        ok = ok && SendWriteCommand(cmd);

      return ok;
    }

    protected bool SendWriteCommand(string command)
    {
      var reply = SendCommand(command);
      if (reply == "RPRT 0\n") return true;
      if (reply != null) BadReply(reply);
      return false;
    }

    protected string? SendReadCommand(string command)
    {
      var reply = SendCommand(command);
      if (reply == null) return null;
      if (reply.EndsWith("\n"))
      {
        reply = reply.Substring(0, reply.Length - 1);
        if (log) Log.Information($"Reply from {GetType().Name} ctld: {reply}");
        return reply;
      }

      BadReply(reply);
      return null;
    }

    protected string? SendCommand(string command)
    {
      try
      {
        if (log) Log.Information($"Sending command to {GetType().Name} ctld: {command}");
        byte[] commandBytes = Encoding.ASCII.GetBytes(command + "\n");
        TcpClient!.Client.Send(commandBytes);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Failed to send command to {GetType().Name} ctld");
        throw;
      }

      try
      {
        return ReadLine();
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Failed to receive reply from {GetType().Name} ctld");
        throw;
      }
    }

    protected void BadReply(string reply)
    {
      Log.Error($"Unexpected reply from {GetType().Name} ctld: {reply.Trim()}");
    }

    byte[] buffer = new byte[65536];
    protected string ReadLine()
    {
      int totalRead = 0;

      while (totalRead < buffer.Length)
      {
        int bytesRead = TcpClient!.Client.Receive(buffer, totalRead, buffer.Length - totalRead, SocketFlags.None);
        if (bytesRead == 0) break; // connection closed
        totalRead += bytesRead;

        for (int i = totalRead - 1; i >= totalRead - bytesRead; i--)
          if (buffer[i] == (byte)'\n')
            return Encoding.ASCII.GetString(buffer, 0, i + 1);
      }

      // If no newline found, return all read bytes
      return Encoding.ASCII.GetString(buffer, 0, totalRead);
    }




    //----------------------------------------------------------------------------------------------
    //                                     IDisposable
    //----------------------------------------------------------------------------------------------
    public virtual void Dispose()
    {
      StopThread();
      stopEvent.Dispose();
    }
  }
}

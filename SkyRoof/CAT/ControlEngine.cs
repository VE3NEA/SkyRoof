﻿using System;
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

    public event EventHandler? StatusChanged;
    public bool IsRunning {get; private set;}


    public ControlEngine(string host, ushort port, IControlEngineSettings settings)
    {
      Host = host;
      Port = port;
      Delay = settings.Delay;
      log = settings.LogTraffic;
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
      processingThread = new Thread(new ThreadStart(ThreadProcedure));
      processingThread.IsBackground = true;
      processingThread.Name = GetType().Name;
      processingThread.Start();      
    }

    protected void StopThread()
    {
      if (stopping) return;
      stopping = true;
      processingThread?.Join();
      processingThread = null;
    }

    private void ThreadProcedure()
    {
      processingThread!.Priority = ThreadPriority.Highest;

      if (Connect() && Setup())
      {
        IsRunning = true;
        OnStatusChanged();

        while (!stopping)
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
      processingThread = null;
      OnStatusChanged();
    }

    protected abstract bool Setup();
    protected abstract void Cycle();




    //----------------------------------------------------------------------------------------------
    //                                    connection
    //----------------------------------------------------------------------------------------------
    private bool Connect()
    {
      TcpClient = new();
      TcpClient.SendTimeout = 1000;
      TcpClient.ReceiveTimeout = 3000;

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

      else if (reply != null) BadReply(reply);
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
        byte[] replyBytes = new byte[65536];
        int replyByteCount = ReadLine(replyBytes);
        return Encoding.ASCII.GetString(replyBytes, 0, replyByteCount);
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

    protected int ReadLine(byte[] buffer)
    {
      int totalRead = 0;
      while (totalRead < buffer.Length)
      {
        int bytesRead = TcpClient!.Client.Receive(buffer, totalRead, buffer.Length - totalRead, SocketFlags.None);
        if (bytesRead == 0) break; // connection closed

        for (int i = totalRead; i < totalRead + bytesRead; i++)
          if (buffer[i] == (byte)'\n')
            return i + 1;

        totalRead += bytesRead;
      }
      return totalRead;
    }




    //----------------------------------------------------------------------------------------------
    //                                     IDisposable
    //----------------------------------------------------------------------------------------------
    public virtual void Dispose()
    {
      StopThread();
    }
  }
}

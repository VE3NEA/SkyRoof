using System.Net;
using System.Net.Sockets;
using Serilog;
using VE3NEA.SkyTlm.Core;

namespace SkyRoof
{
  // Shares decoded telemetry frames over a KISS-over-TCP server, wire-compatible with the KISS output of
  // the UZ7HO soundmodem (default port 8100) and Dire Wolf (port 8001): each decoded frame is sent as a
  // KISS data frame (command byte 0x00 = channel 0, data) with no FCS, FEND-delimited and FESC-escaped, so
  // any KISS client (gr-satellites, kissutil, APRS apps) that talks to those TNCs works against SkyRoof.
  public class KissServer : IDisposable
  {
    private const byte FEND = 0xC0;
    private const byte FESC = 0xDB;
    private const byte TFEND = 0xDC;
    private const byte TFESC = 0xDD;

    private TcpListener? Listener;
    private readonly List<TcpClient> Clients = new();
    private readonly object ClientsLock = new();

    public bool Active { get; private set; }
    public int ClientCount { get { lock (ClientsLock) return Clients.Count; } }


    //----------------------------------------------------------------------------------------------
    //                                      start / stop
    //----------------------------------------------------------------------------------------------
    public void Start(int port)
    {
      Stop();

      try
      {
        Listener = new TcpListener(IPAddress.Any, port);
        Listener.Start();
        Active = true;
        Log.Information($"KISS server listening on port {port}");
        HandleIncomingConnections();
      }
      catch (Exception e)
      {
        Active = false;
        Log.Error(e, "Failed to start KISS server");
      }
    }

    public void Stop()
    {
      if (!Active) return;

      Active = false;
      Listener?.Stop();
      Listener = null;

      lock (ClientsLock)
      {
        foreach (var client in Clients) try { client.Close(); } catch { }
        Clients.Clear();
      }
    }

    public void Dispose()
    {
      Stop();
    }


    //----------------------------------------------------------------------------------------------
    //                                      connections
    //----------------------------------------------------------------------------------------------
    private async void HandleIncomingConnections()
    {
      var listener = Listener;
      try
      {
        while (listener != null)
        {
          var client = await listener.AcceptTcpClientAsync();
          lock (ClientsLock) Clients.Add(client);
          DrainClient(client);
        }
      }
      catch (Exception)
      {
        // listener stopped
      }
    }

    // KISS clients (and apps probing for a TNC) send SetHardware/TXDELAY commands we don't honor; read and
    // discard inbound bytes so the connection stays healthy, and detect disconnects to drop the client.
    private async void DrainClient(TcpClient client)
    {
      try
      {
        var stream = client.GetStream();
        var buffer = new byte[256];
        while (await stream.ReadAsync(buffer) > 0) { }
      }
      catch (Exception) { }

      RemoveClient(client);
    }

    private void RemoveClient(TcpClient client)
    {
      lock (ClientsLock) Clients.Remove(client);
      try { client.Close(); } catch { }
    }


    //----------------------------------------------------------------------------------------------
    //                                      send frame
    //----------------------------------------------------------------------------------------------
    public void SendToAll(Frame frame)
    {
      if (!Active) return;

      TcpClient[] clients;
      lock (ClientsLock)
      {
        if (Clients.Count == 0) return;
        clients = Clients.ToArray();
      }

      byte[] kiss = Encode(frame.Bytes);

      foreach (var client in clients)
        try { client.GetStream().Write(kiss); }
        catch (Exception) { RemoveClient(client); }
    }

    // wrap the frame bytes in a KISS data frame: FEND, command byte 0x00, escaped payload, FEND
    private static byte[] Encode(byte[] data)
    {
      var output = new List<byte>(data.Length + 4) { FEND, 0x00 };

      foreach (byte b in data)
        if (b == FEND) { output.Add(FESC); output.Add(TFEND); }
        else if (b == FESC) { output.Add(FESC); output.Add(TFESC); }
        else output.Add(b);

      output.Add(FEND);
      return output.ToArray();
    }
  }
}

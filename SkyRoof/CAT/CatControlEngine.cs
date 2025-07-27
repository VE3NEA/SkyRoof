using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Speech.Synthesis.TtsEngine;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CSCore.Win32;
using Newtonsoft.Json;
using Serilog;
using SkyRoof.Properties;
using VE3NEA;

namespace SkyRoof
{
  public enum CatMode { RxOnly, TxOnly, Simplex, Split, Duplex }

  public class CatControlEngine : ControlEngine
  {
    private readonly RadioInfo RadioInfo;
    private Capabilities Caps => RadioInfo.capabilities;
    private Commands Cmds => RadioInfo.commands;

    public CatMode CatMode;
    private readonly bool IgnoreDialKnob;

    public bool? Ptt { get; private set; } = null;
    private bool PttChanged = false;

    public long RequestedRxFrequency, LastWrittenRxFrequency, LastReadRxFrequency;
    public long RequestedTxFrequency, LastWrittenTxFrequency, LastReadTxFrequency;
    private Slicer.Mode? RequestedRxMode, LastWrittenRxMode;      
    private Slicer.Mode? RequestedTxMode, LastWrittenTxMode;
    private bool? RequestedPtt;

    public event EventHandler? RxTuned;
    public event EventHandler? TxTuned;

    public static RadioInfoList BuildRadioInfoList()
    {
      string path = Path.Combine(Utils.GetUserDataFolder(), "cat_info.json");
      //if (!File.Exists(path)) 
        File.WriteAllBytes(path, Resources.cat_info);

      return JsonConvert.DeserializeObject<RadioInfoList>(File.ReadAllText(path))!;
    }

    public CatControlEngine(CatRadioSettings radioSettings, CatSettings catSettings) : base(radioSettings.Host, radioSettings.Port, catSettings)
    {
      RadioInfo = BuildRadioInfoList().First(r => r.radio == radioSettings.RadioType);
    }


    private void LogPtt(string msg)
    {
      if (log) Log.Information(msg);
    }

    private void LogFreqs(string msg)
    {
      if (log) Log.Information($"{msg}  (RxReq={RequestedRxFrequency:N0}  RxWr={LastWrittenRxFrequency:N0}  RxRd={LastReadRxFrequency:N0})");
    }

    // some radios use 10 Hz steps and some don't, handle them all the same way
    private long RoundTo10(double freq)
    {
      int step = RadioInfo.tuning_step_hz;
      return step * (long)Math.Truncate(freq / step);
    }




    //----------------------------------------------------------------------------------------------
    //                                      public methods
    //----------------------------------------------------------------------------------------------
    public virtual void Start(bool rx, bool tx, bool crossband)
    {
      // determmine required cat control mode
      if (rx && !tx) CatMode = CatMode.RxOnly;
      else if (!rx && tx) CatMode = CatMode.TxOnly;
      else if (crossband && Cmds.setup_duplex != null) CatMode = CatMode.Duplex;
      else if (Cmds.setup_split != null) CatMode = CatMode.Split;
      else CatMode = CatMode.Simplex;

      StartThread();
    }

    public void SetRxFrequency(double frequency)
    {
      frequency = RoundTo10(frequency);
      LogFreqs($"SetRxFrequency {frequency}");
      RequestedRxFrequency = (long)frequency;
    }

    public void SetTxFrequency(double frequency)
    {
      frequency = RoundTo10(frequency);
      LogFreqs($"SetTxFrequency {frequency}");
      RequestedTxFrequency = (long)frequency;
    }

    public void SetRxMode(Slicer.Mode mode)
    {
      RequestedRxMode = mode;
    }

    public void SetTxMode(Slicer.Mode mode)
    {
      RequestedTxMode = mode;
    }

    public void SetPtt(bool ptt)
    {
      LogPtt($"SetPtt {ptt}");
      RequestedPtt = ptt;
    }




    //----------------------------------------------------------------------------------------------
    //                                        thread 
    //----------------------------------------------------------------------------------------------
    protected override void ReadWrite()
    {
      ReadPtt();
      if (NeddToWriteTxFreqModeBeforePtt()) TryWriteTxFreqModeBeforePtt();
      TryWritePtt();

      if (!IgnoreDialKnob) TryReadRxFrequency();
      if (!IgnoreDialKnob) TryReadTxFrequency();

      if (NeedToWriteRxFrequency()) TryWriteRxFrequency();
      if (NeedToWriteTxFrequency()) TryWriteTxFrequency();

      if (NeedToWriteRxMode()) TryWriteRxMode();
      if (NeedToWriteTxMode()) TryWriteTxMode();
    }

    protected override bool Setup()
    {
      try
      {
        switch (CatMode)
        {
          case CatMode.RxOnly:
          case CatMode.TxOnly:
          case CatMode.Simplex: return SendWriteCommands(Cmds.setup_simplex); 
          case CatMode.Split:   return SendWriteCommands(Cmds.setup_split); 
          case CatMode.Duplex:  return SendWriteCommands(Cmds.setup_duplex); 
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Failed to set up radio {RadioInfo.radio}");
      }

      return false;
    }




    //----------------------------------------------------------------------------------------------
    //                                    read frequencies
    //----------------------------------------------------------------------------------------------
    private void TryReadRxFrequency()
    {
      string command = GetReadRxFrequencyCommand();
      if (command == string.Empty) return;

      long frequency = ReadFrequency(command);

      bool changed = LastReadRxFrequency != 0 &&     // first read - ignore, no previous value
        IsDiff(frequency, LastReadRxFrequency) &&    // same freq as before, no change
        IsDiff(frequency, LastWrittenRxFrequency) && // new freq was written, ingnore
        IsDiff(frequency, LastWrittenTxFrequency);   // tx freq read by accident, ignore

      LastReadRxFrequency = frequency;
      if (changed) OnRxFrequencyChanged();
    }
    

    private void TryReadTxFrequency()
    {
      string command = GetReadTxFrequencyCommand();
      if (command == string.Empty) return;

      long frequency = ReadFrequency(command);

      bool changed = LastReadTxFrequency != 0 &&
        IsDiff(frequency, LastReadTxFrequency) &&
        IsDiff(frequency, LastWrittenTxFrequency) &&
        IsDiff(frequency, LastWrittenRxFrequency);

      LastReadTxFrequency = frequency;
      if (changed) OnTxFrequencyChanged();
    }

    private string GetReadRxFrequencyCommand()
    {
      if (!HasCapability(Caps.read_main_frequency)) return string.Empty;
      return Cmds.read_main_frequency!;
    }

    private string GetReadTxFrequencyCommand()
    {
      switch (CatMode)
      {
        case CatMode.TxOnly:
        case CatMode.Simplex:
          if (HasCapability(Caps.read_main_frequency))
            return Cmds.read_main_frequency!;
          break;
      
        case CatMode.Split:          
        case CatMode.Duplex:
          if (HasCapability(Caps.read_split_frequency))
            return Cmds.read_split_frequency!;
          break;
      }

      return string.Empty;
    }

    private long ReadFrequency(string command)
    {
      var reply = SendReadCommand(command);
      if (reply == null) return 0;
      if (!long.TryParse(reply, CultureInfo.InvariantCulture, out long frequency)) BadReply(reply);
      return RoundTo10(frequency);
    }
    



    //----------------------------------------------------------------------------------------------
    //                                 write frequencies
    //----------------------------------------------------------------------------------------------
    private void TryWriteRxFrequency()
    {
      // another thread may change RequestedRxFrequency while we are writing, use a local copy
      long frequency = RequestedRxFrequency;

      string command = GetWriteRxFrequencyCommand(frequency);
      if (command == string.Empty) return;

      SendWriteCommand(command);
      LastWrittenRxFrequency = frequency;
      LogFreqs("Rx frequency written");
    }

    private void TryWriteTxFrequency()
    {
      if (!NeedToWriteTxFrequency()) return;

      long frequency = RequestedTxFrequency;
      string command = GetWriteTxFrequencyCommand(frequency);
      if (command == string.Empty) return;

      SendWriteCommand(command);
      LastWrittenTxFrequency = frequency;
      LogFreqs("Tx frequency written");
    }

    private string GetWriteRxFrequencyCommand(long frequency)
    {
      if (CatMode == CatMode.TxOnly) return string.Empty;

      if (!HasCapability(Caps.set_main_frequency)) return string.Empty;
      return Cmds.set_main_frequency!.Replace("{frequency}", $"{frequency}");

    }

    private string GetWriteTxFrequencyCommand(long frequency)
    {
      if (CatMode == CatMode.RxOnly) return string.Empty;

      if (HasCapability(Caps.set_split_frequency))
          return Cmds.set_split_frequency!.Replace("{frequency}", $"{frequency}");

      return string.Empty;
    }

    // for FT-817: cannot set split frequency/mode and cannot set main frequency/mode when transmitting,
    // set it immediately before switching to transmit mode
    private void TryWriteTxFreqModeBeforePtt()
    {
      LogPtt("Writing TX frequency and mode before PTT ON");
      TryWriteTxFrequency();
      TryWriteTxMode();
    }




    //----------------------------------------------------------------------------------------------
    //                                       mode
    //----------------------------------------------------------------------------------------------
    private void TryWriteRxMode()
    {
      if (!HasCapability(Caps.set_main_mode)) return;

      string mode = $"{RequestedRxMode}".Replace("_D", "");
      string command = Cmds.set_main_mode!.Replace("{mode}", mode);
      SendWriteCommand(command);
      LastWrittenRxMode = RequestedRxMode;
    }

    private void TryWriteTxMode()
    {
      string command = GetWriteTxModeCommand();
      if (command == string.Empty) return;

      SendWriteCommand(command);
      LastWrittenTxMode = RequestedTxMode;
    }

    private string GetWriteTxModeCommand()
    {
      string mode = $"{RequestedTxMode}".Replace("_D", "");

      if (CatMode == CatMode.RxOnly) return string.Empty;

      if (HasCapability(Caps.set_split_mode))
          return Cmds.set_split_mode!.Replace("{mode}", mode);

      return string.Empty;
    }




    //----------------------------------------------------------------------------------------------
    //                                         ptt
    //----------------------------------------------------------------------------------------------
    internal bool CanPtt()
    {
      return CatMode != CatMode.RxOnly &&
             Cmds.set_ptt_on != null &&
             Cmds.set_ptt_off != null;
    }

    private void ReadPtt()
    {
      PttChanged = false;
      if (Cmds.read_ptt == null) return;

      var reply = SendReadCommand(Cmds.read_ptt!);
      bool newPtt = reply == "1";

      PttChanged = newPtt != Ptt;
      Ptt = newPtt;

      if (PttChanged)
        if (Ptt == true) LastWrittenTxFrequency = 0; else LastWrittenRxFrequency = 0;


          LogPtt($"ReadPtt: {Ptt} (changed={PttChanged})");
    }

    private void TryWritePtt()
    {
      if (CatMode == CatMode.RxOnly) return;
      if (!RequestedPtt.HasValue) return;
      if (RequestedPtt == Ptt) return;

      if (RequestedPtt == false && Cmds.set_ptt_off != null)
        SendWriteCommand(Cmds.set_ptt_off);

      if (RequestedPtt == true && Cmds.set_ptt_on != null)
      {
        TryWriteTxFreqModeBeforePtt();
        SendWriteCommand(Cmds.set_ptt_on);
      }

      RequestedPtt = null;
    }




    //----------------------------------------------------------------------------------------------
    //                                  check conditions
    //----------------------------------------------------------------------------------------------
    private bool HasCapability(string[] capability)
    {
      return
        (capability.Contains("when_receiving") && capability.Contains("when_transmitting"))
        ||
        (capability.Contains("when_receiving") && Ptt == false)
        ||
        (capability.Contains("when_transmitting") && Ptt == true);
    }


    private bool NeddToWriteTxFreqModeBeforePtt()
    {
      // not simplex
      if (CatMode != CatMode.Simplex) return false;

      // not swsitching to tx
      if (!PttChanged || Ptt != true) return false;

      // nothing to write
      if (RequestedTxFrequency == 0 && !RequestedTxMode.HasValue) return false;

      // can write when transmitting
      if (Caps.set_main_frequency.Contains("when_transmitting") && Caps.set_main_mode.Contains("when_transmitting"))
        return false;


      return true;

    }

    private bool NeedToWriteRxFrequency()
    {
      if (CatMode == CatMode.TxOnly) return false;
      if (RequestedRxFrequency == 0) return false; // never assigned
      if (CatMode == CatMode.Simplex && PttChanged) return true;
      return IsDiff(RequestedRxFrequency, LastWrittenRxFrequency);
    }

    private bool NeedToWriteTxFrequency()
    {
      if (CatMode == CatMode.RxOnly) return false;
      if (RequestedTxFrequency == 0) return false;
      if (CatMode == CatMode.Simplex && PttChanged) return true;
      return IsDiff(RequestedTxFrequency, LastWrittenTxFrequency);
    }

    private bool NeedToWriteRxMode()
    {
      if (CatMode == CatMode.TxOnly) return false;
      if (!RequestedRxMode.HasValue) return false;
      if (CatMode == CatMode.Simplex && PttChanged) return true;
      return RequestedRxMode != LastWrittenRxMode;
    }

    private bool NeedToWriteTxMode()
    {
      if (CatMode == CatMode.RxOnly) return false;
      if (!RequestedTxMode.HasValue) return false;
      if (CatMode == CatMode.Simplex && PttChanged) return true;
      return RequestedTxMode != LastWrittenTxMode;
    }

    private bool IsDiff(long freq1, long freq2)
    {
      return Math.Abs(freq1 - freq2) > 0;
    }




    //----------------------------------------------------------------------------------------------
    //                                      events
    //----------------------------------------------------------------------------------------------
    protected void OnRxFrequencyChanged()
    {
      LogFreqs($"OnRxFrequencyChanged");
      syncContext.Post(s => RxTuned?.Invoke(this, EventArgs.Empty), null);
    }

    protected void OnTxFrequencyChanged()
    {
      LogFreqs($"OnTxFrequencyChanged");
      syncContext.Post(s => TxTuned?.Invoke(this, EventArgs.Empty), null);
    }
  }
}

using VE3NEA;

namespace SkyRoof
{
  // meaningful names for the WsjtxQsoState enum
  public enum Ft4MessageType {Unknown, DE, dB, R_dB, RR73, _73, CQ }

  public class Ft4QsoSequencer
  {
    private string mySquare;
    private string myCall;
    private string?[] messages = new string[7];

    public string MyCall { get => myCall; set => SetMyCall(value); }
    public string MySquare{ get => mySquare; set => SetMySquare(value); }

    public string? LastHisCall {get; private set; }

    public string? HisCall { get; private set; }
    public string? HisSquare { get; private set; }
    public int? MySnr { get; private set; }
    public int? HisSnr { get; private set; }

    public Ft4MessageType MessageType { get; private set; }
    public string? Message => messages[(int)MessageType];

    public int RxAudioFrequency { get; private set; } = NativeFT4Coder.DEFAULT_AUDIO_FREQUENCY;

    public Ft4QsoSequencer(string call, string square)
    {
      myCall = call;
      mySquare = square[..4];
      Reset();
    }

    public void Reset()
    {
      if (!string.IsNullOrEmpty(HisCall)) LastHisCall = HisCall;

      HisCall = null;
      HisSquare = null;
      HisSnr = null;
      MySnr = null;
      GenerateMessages();
      MessageType = Ft4MessageType.CQ;
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                           update state
    //--------------------------------------------------------------------------------------------------------------
    // ProcessMessage is called in two cases:
    //   - I received a message addressed to me (forceReply=false)
    //   - I clicked on a message, addressed to me or not (forceReply=true)
    public bool ProcessMessage(DecodedItem item, bool forceReply)
    {
      // I am in the middle of a qso and someone else calls me
      if (!forceReply && HisCall != null && item.Parse.DECallsign != HisCall) return false;

      // always ignore 73
      if ((Ft4MessageType)item.Parse.QsoState == Ft4MessageType._73) return false;

      // extract info from his message
      HisSnr = item.Decode.Snr;
      RxAudioFrequency = (int)item.Decode.OffsetFrequencyHz;
      var hisMessageType = (Ft4MessageType)item.Parse.QsoState;
      HisCall = item.Parse.DECallsign;
      if (item.Parse.DXCallsign == MyCall && !string.IsNullOrEmpty(item.Parse.Report) && item.Parse.Report != "RR73")
        MySnr = ReportToInt(item.Parse.Report);
      if (!string.IsNullOrEmpty(item.Parse.GridSquare)) HisSquare = item.Parse.GridSquare;

      GenerateMessages();

      // I am calling him
      if (item.Parse.DXCallsign != MyCall)
        MessageType = Ft4MessageType.DE;

      // reply received during a qso, or I clicked on his message to me that was part of our qso
      else
        // cq: not possible if dxcallsign==mycall
        // 73: was handled above
        // the rest: reply with next message type
        MessageType = hisMessageType + 1;

      return true;
    }

    // user clicked on a message type button
    public bool ForceMessage(Ft4MessageType messageType)
    {
      if (!IsMessageAvailable(messageType)) return false;
      MessageType = messageType;
      return true;
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                           helper funcs
    //--------------------------------------------------------------------------------------------------------------
    private void SetMyCall(string value)
    {
      myCall = value;
      GenerateMessages();
    }

    private void SetMySquare(string value)
    {
      mySquare = value[..4];
      GenerateMessages();
    }

    private int? ReportToInt(string report)
    {
      if (report.StartsWith("R")) report = report[1..];
      return int.Parse(report);
    }

    public string IntToReport(int? snr)
    {
      if (snr == null) return "";

      snr = Math.Min(49, Math.Max(-24, snr.Value));
      string result = $"{snr:D2}";
      if (snr >= 0) result = "+" + result;
      return result;
    }

    private void GenerateMessages()
    {
      messages[(int)Ft4MessageType.CQ] = $"CQ {myCall} {mySquare}";

      if (HisCall != null)
      {
        messages[(int)Ft4MessageType.DE] = $"{HisCall} {myCall} {mySquare}";
        messages[(int)Ft4MessageType.dB] = $"{HisCall} {myCall} {IntToReport((int)HisSnr!)}";
        messages[(int)Ft4MessageType.R_dB] = $"{HisCall} {myCall} R{IntToReport((int)HisSnr!)}";
        messages[(int)Ft4MessageType.RR73] = $"{HisCall} {myCall} RR73";
        messages[(int)Ft4MessageType._73] = $"{HisCall} {myCall} 73";
      }
      else
      {
        for (Ft4MessageType i = Ft4MessageType.Unknown; i <= Ft4MessageType._73; i++)
          messages[(int)i] = null;

        if (LastHisCall != null) messages[(int)Ft4MessageType._73] = $"{LastHisCall} {myCall} 73";
      }
    }

    public bool IsMessageAvailable(Ft4MessageType type)
    {
      return messages[(int)type] != null;
    }
  }
}

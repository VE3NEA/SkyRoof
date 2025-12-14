using WsjtxUtils.WsjtxMessages;
using WsjtxUtils.WsjtxMessages.Messages;

namespace SkyRoof
{
  internal class WritableStatus : Status, IWsjtxDirectionIn
  {
    public override void WriteMessage(WsjtxMessageWriter messageWriter)
    {
      base.WriteMessage(messageWriter);
      messageWriter.WriteUInt64(DialFrequencyInHz);
      messageWriter.WriteString(Mode);
      messageWriter.WriteString(DXCall);
      messageWriter.WriteString(Report);
      messageWriter.WriteString(TXMode);
      messageWriter.WriteBool(TXEnabled);
      messageWriter.WriteBool(Transmitting);
      messageWriter.WriteBool(Decoding);
      messageWriter.WriteUInt32(RXOffsetFrequencyHz);
      messageWriter.WriteUInt32(TXOffsetFrequencyHz);
      messageWriter.WriteString(DECall);
      messageWriter.WriteString(DEGrid);
      messageWriter.WriteString(DXGrid);
      messageWriter.WriteBool(TXWatchdog);
      messageWriter.WriteString(SubMode);
      messageWriter.WriteBool(FastMode);
      messageWriter.WriteEnum(SpecialOperationMode);
      messageWriter.WriteUInt32(FrequencyTolerance);
      messageWriter.WriteUInt32(TRPeriod);
      messageWriter.WriteString(ConfigurationName);
      messageWriter.WriteString(TXMessage);
    }
  }

  internal class WritableDecode : Decode, IWsjtxDirectionIn
  {
    public override void WriteMessage(WsjtxMessageWriter messageWriter)
    {
      base.WriteMessage(messageWriter);
      messageWriter.WriteBool(New);
      messageWriter.WriteUInt32(Time);
      messageWriter.WriteInt32(Snr);
      messageWriter.WriteDouble(OffsetTimeSeconds);
      messageWriter.WriteUInt32(OffsetFrequencyHz);
      messageWriter.WriteString(Mode);
      messageWriter.WriteString(Message);
      messageWriter.WriteBool(LowConfidence);
      messageWriter.WriteBool(OffAir);
    }
  }
}

namespace SkyRoof
{
  public class DecodeEventArgs : EventArgs
  {
    public string Messages;
    public DateTime Utc;

    public DecodeEventArgs(string messages, DateTime utc)
    {
      Messages = messages;
      Utc = utc;
    }
  }
}

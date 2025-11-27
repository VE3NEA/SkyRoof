using MathNet.Numerics;
using Serilog;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class Ft4ConsolePanel : DockContent
  {
    private readonly Context ctx;
    public readonly Ft4Decoder Ft4Decoder = new();

    public Ft4ConsolePanel()
    {
      InitializeComponent();
    }

    public Ft4ConsolePanel(Context ctx)
    {
      this.ctx = ctx;
      Log.Information("Creating Ft4ConsolePanel");
      InitializeComponent();

      ctx.Ft4ConsolePanel = this;
      ctx.MainForm.Ft4ConsoleMNU.Checked = true;

      Ft4Decoder.SlotDecoded += Ft4Decoder_SlotDecoded;
    }

    private void Ft4Decoder_SlotDecoded(object? sender, DecodeEventArgs e)
    {
      if (e.Messages.Length > 0)
          richTextBox1.BeginInvoke(() => { 
            richTextBox1.AppendText($"{e.Utc:HH:mm:ss.f}\n{e.Messages}\n");
          });
    }

    private void Ft4ConsolePanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing Ft4ConsolePanel");
      ctx.Ft4ConsolePanel = null;
      ctx.MainForm.Ft4ConsoleMNU.Checked = false;
    }
  }
}

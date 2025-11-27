using Serilog;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class Ft4ConsolePanel : DockContent
  {
    private readonly Context ctx;

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
    }

    private void Ft4ConsolePanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing Ft4ConsolePanel");
      ctx.Ft4ConsolePanel = null;
      ctx.MainForm.Ft4ConsoleMNU.Checked = false;
    }
  }
}

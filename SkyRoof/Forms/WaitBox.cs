using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;

namespace VE3NEA
{
  public partial class WaitBox : Form
  {
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();

    private const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;

    public WaitBox()
    {
      InitializeComponent();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);

      // Windows fades a new top level window in over about 200 ms, and the box often lives for
      // less than that: a cached FFTW plan is built in a few tens of milliseconds. It would then
      // never reach full opacity and only the pixels behind it would ever show. With the
      // transition off it appears at once, and Run() below makes sure it appears painted.
      try
      {
        int disabled = 1;
        DwmSetWindowAttribute(Handle, DWMWA_TRANSITIONS_FORCEDISABLED, ref disabled, sizeof(int));
      }
      catch (Exception ex)
      {
        // dwmapi may be missing under Wine, in which case the box just keeps the fade
        Log.Warning(ex, "DwmSetWindowAttribute failed");
      }
    }

    /// <summary>
    /// Shows a wait box, runs <paramref name="action"/>, then closes the box.
    /// Use around slow operations such as FFTW plan creation. Call on the UI thread.
    /// </summary>
    public static void Run(Action action)
    {
      var box = new WaitBox();

      // Windows fills a window with its class background brush when it first appears, and that
      // brush is the OS window color - white on a machine whose Windows theme is light, whatever
      // the application's own color mode says. The caller then blocks the UI thread, so the box
      // can sit on screen as painted by Windows rather than by us: a white pane under a dark
      // title bar, with no text in it. Showing it fully transparent hides that fill; the box is
      // then painted, and only the painted box is faded in.
      box.Opacity = 0;
      box.Show();
      box.Refresh();
      Application.DoEvents();
      box.Opacity = 1;

      // and wait for the composition engine to put it on screen before the wait begins
      try { DwmFlush(); } catch (Exception ex) { Log.Warning(ex, "DwmFlush failed"); }
      try
      {
        action();
      }
      finally
      {
        box.Close();
        box.Dispose();
      }
    }
  }
}

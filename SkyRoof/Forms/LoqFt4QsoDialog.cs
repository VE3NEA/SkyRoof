
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace SkyRoof
{
  public partial class LoqFt4QsoDialog : Form
  {
    private Context ctx;
    private QsoInfo qso;
    private static Point LastLocation = Point.Empty;

    public LoqFt4QsoDialog(Context ctx, QsoInfo qso)
    {
      InitializeComponent();

      this.ctx = ctx;
      this.qso = qso;
      SetRandomLocation();
    }

    internal static void PopUp(Context ctx, QsoInfo qso)
    {
      var dialog = new LoqFt4QsoDialog(ctx, qso);
      dialog.label1.Text = $"Save FT4 QSO with {qso.Call}?";
      dialog.Show(ctx.MainForm);
    }

    private void SetRandomLocation()
    {
      if (LastLocation == Point.Empty)
        LastLocation = new(
          (Screen.PrimaryScreen!.WorkingArea.Width - Size.Width) / 2,
          (Screen.PrimaryScreen!.WorkingArea.Height - Size.Width) / 2
          );

      Random rand = new Random();

      Location =  new(
        LastLocation.X + rand.Next(-50, 50),
        LastLocation.Y + rand.Next(-50, 50)
        );
    }

    private void SaveBtn_Click(object sender, EventArgs e)
    {
      ctx.Ft4ConsolePanel?.WsjtxUdpSender?.SendLogQsoMessage(qso);
      ctx.LoggerInterface.SaveQso(qso);
      Hide();
    }

    private void EditBtn_Click(object sender, EventArgs e)
    {
      if (ctx.QsoEntryPanel == null)
      {
        ctx.MainForm.ShowFloatingPanel(new QsoEntryPanel(ctx));
        ctx.QsoEntryPanel!.ShouldClose = true;
      }
      Hide();
      ctx.QsoEntryPanel!.SetQsoInfo(qso);
      ctx.QsoEntryPanel!.Focus();
    }
    private void LoqFt4QsoDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (e.CloseReason == CloseReason.UserClosing)
      {
        e.Cancel = true;
        Hide();
      }
    }

    private void CancelBtn_Click(object sender, EventArgs e)
    {
      Hide();
    }

    private void LoqFt4QsoDialog_Move(object sender, EventArgs e)
    {
      LastLocation = Location;
    }
  }
}

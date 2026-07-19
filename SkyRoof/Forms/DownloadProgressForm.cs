using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Serilog;

namespace SkyRoof
{
  /// <summary>Modal progress dialog for the FM speech model download (integration plan C1): runs
  /// <see cref="FmModelDownloader.DownloadAndInstallAsync"/> while showing a progress bar, and reports
  /// whether the install succeeded. Built in code (no designer) so it stays self-contained.</summary>
  public sealed class DownloadProgressForm : Form
  {
    private readonly ProgressBar bar;
    private readonly Label label;
    private readonly CancellationTokenSource cts = new();
    private bool success;

    private DownloadProgressForm()
    {
      Text = "FM Speech Model";
      FormBorderStyle = FormBorderStyle.FixedDialog;
      StartPosition = FormStartPosition.CenterParent;
      MinimizeBox = false;
      MaximizeBox = false;
      ClientSize = new Size(400, 110);

      label = new Label { Left = 12, Top = 14, Width = 376, Text = "Downloading FM speech recognition model…" };
      bar = new ProgressBar { Left = 12, Top = 42, Width = 376, Height = 22, Minimum = 0, Maximum = 100 };
      var cancel = new Button { Text = "Cancel", Left = 313, Top = 74, Width = 75, DialogResult = DialogResult.Cancel };
      cancel.Click += (s, e) => cts.Cancel();

      Controls.Add(label);
      Controls.Add(bar);
      Controls.Add(cancel);
      CancelButton = cancel;
    }

    /// <summary>Show the modal download dialog over <paramref name="parent"/>; returns true when the model
    /// was downloaded, verified, and installed.</summary>
    public static bool Install(IWin32Window parent)
    {
      using var dlg = new DownloadProgressForm();
      dlg.ShowDialog(parent);
      return dlg.success;
    }

    protected override async void OnShown(EventArgs e)
    {
      base.OnShown(e);
      var progress = new Progress<int>(p =>
      {
        bar.Value = Math.Clamp(p, 0, 100);
        label.Text = $"Downloading FM speech recognition model…  {p}%";
      });
      try
      {
        await FmModelDownloader.DownloadAndInstallAsync(TelemetryPanel.FmModelDir, progress, cts.Token);
        success = true;
      }
      catch (OperationCanceledException)
      {
        success = false;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "FM speech model download failed");
        MessageBox.Show(this, "Failed to download the FM speech model:\n\n" + ex.Message,
          "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        success = false;
      }
      Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
      cts.Cancel();
      base.OnFormClosing(e);
    }
  }
}

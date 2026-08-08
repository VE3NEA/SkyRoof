using System.Drawing.Drawing2D;
using Serilog;
using VE3NEA.SkySSTV;

namespace SkyRoof
{
  // Modal post-filter editor for a completed SSTV image (sstv-denoise-plan.md §8, D2/D12). The filter runs
  // on the RAW reconstruction carried by the image event, never on the picture currently displayed, so
  // re-applying at new settings always starts from the original and no amount of pressing Apply can compound
  // one filter onto another. That is also why there is no Undo button: selecting None IS the undo.
  //
  // Explicit Apply rather than live re-filtering (D12, closed 2026-08-08 on measured runtimes): the worst
  // case in the mode table is PD290 at 0.73 s two-pass NLM, and Robot 36 — 212 of 214 corpus images — is
  // ~0.2 s. Sub-second is why there is a wait cursor here and no progress bar, and why the library needs no
  // progress or cancellation hook.
  //
  // Settings are deliberately NOT remembered between images (user, 2026-08-07). The right strength depends
  // on the capture — §9.3 measured the working window moving with SNR — so a value carried over from a
  // different pass is a misleading starting point rather than a convenience.
  public partial class SstvDenoiseDialog : Form
  {
    // the raw, unfiltered reconstruction: the source every Apply starts from
    private SstvImagePlanes Planes = null!;

    // the picture as it was before the dialog opened, shown for None. Not planes.ToRgb(): the planes are
    // byte-quantized while the decoder's own image converts from unquantized doubles, so this is the exact
    // original and cancelling out of the dialog is exact rather than nearly exact.
    private RgbImage Original = null!;

    // the rendering currently on display, and what OK hands back
    public RgbImage Result { get; private set; } = null!;

    // the settings that produced Result, for the caller's status line
    public SstvDenoiseOptions Options { get; private set; } = new();

    // control the dialog is centered over; null falls back to the designer's StartPosition
    private Control? AnchorControl;

    // guards the radio/checkbox handlers while the controls are being loaded from the defaults
    private bool Loading;

    public SstvDenoiseDialog()
    {
      InitializeComponent();
    }

    /// <summary>Open on one image. <paramref name="planes"/> is the event's raw reconstruction and
    /// <paramref name="original"/> the picture the panel is showing.</summary>
    public DialogResult Open(SstvImagePlanes planes, RgbImage original, string caption, Control? anchor = null)
    {
      Planes = planes;
      Original = original;
      Result = original;
      AnchorControl = anchor;
      if (anchor != null) StartPosition = FormStartPosition.Manual;

      Text = $"Denoise Image — {caption}";
      LoadDefaults();
      ShowImage(original, "original");
      return ShowDialog();
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (AnchorControl == null || !AnchorControl.IsHandleCreated) return;

      var r = AnchorControl.RectangleToScreen(AnchorControl.ClientRectangle);
      var wa = Screen.FromRectangle(r).WorkingArea;
      int x = Math.Max(wa.Left, Math.Min(r.Left + (r.Width - Width) / 2, wa.Right - Width));
      int y = Math.Max(wa.Top, Math.Min(r.Top + (r.Height - Height) / 2, wa.Bottom - Height));
      Location = new Point(x, y);
    }


    //----------------------------------------------------------------------------------------------
    //                                          controls
    //----------------------------------------------------------------------------------------------
    // The controls open on the library defaults, which are the settled values of the plan's §9 sweeps and
    // are also what the decode path runs (D22). So the dialog opens showing exactly what produced the
    // picture behind it, and every control is a departure from that.
    private void LoadDefaults()
    {
      var d = new SstvDenoiseOptions();
      Loading = true;

      NoneRadio.Checked = false;
      WienerRadio.Checked = false;
      NlmRadio.Checked = true;

      WienerWidthSpinner.Value = d.WienerWindowW;
      WienerHeightSpinner.Value = d.WienerWindowH;
      WienerFloorSpinner.Value = (decimal)d.WienerGainFloor;
      WienerChromaSpinner.Value = (decimal)d.WienerChromaK;

      NlmStrengthSpinner.Value = (decimal)d.NlmSig;
      NlmPatchSpinner.Value = d.NlmPatchWing;
      NlmSearchSpinner.Value = d.NlmSearchWing;
      NlmChromaSpinner.Value = (decimal)d.NlmChromaK;
      NlmTwoPassCheckBox.Checked = d.NlmTwoPass;

      SkipNoiseBandsCheckBox.Checked = d.SkipNoiseOnlyBands;

      Loading = false;
      EnableControls();
    }

    // only the selected filter's group is live: the other group's numbers describe a filter that is not
    // running, and leaving them editable invites the operator to tune something with no effect
    private void EnableControls()
    {
      WienerGroupBox.Enabled = WienerRadio.Checked;
      NlmGroupBox.Enabled = NlmRadio.Checked;
      // the noise-band gate is not a property of either filter — it decides which ROWS are filtered at all,
      // and both methods honor it — so it sits outside both groups and follows neither
      SkipNoiseBandsCheckBox.Enabled = !NoneRadio.Checked;
      ApplyBtn.Enabled = true;
    }

    private void MethodRadio_CheckedChanged(object sender, EventArgs e)
    {
      if (Loading) return;
      EnableControls();
    }

    /// <summary>The settings the controls currently hold. Everything the plan left as a probe sweep axis —
    /// the §9.1 mapping law, the chroma arms, the second-pass constants — keeps its default here: those are
    /// measurement arms, and exposing them would make this a research console rather than a picture
    /// control.</summary>
    private SstvDenoiseOptions CurrentOptions() => new()
    {
      Method = NoneRadio.Checked ? SstvDenoiseMethod.None
             : WienerRadio.Checked ? SstvDenoiseMethod.Wiener
             : SstvDenoiseMethod.Nlm,

      WienerWindowW = (int)WienerWidthSpinner.Value,
      WienerWindowH = (int)WienerHeightSpinner.Value,
      WienerGainFloor = (double)WienerFloorSpinner.Value,
      WienerChromaK = (double)WienerChromaSpinner.Value,

      NlmSig = (double)NlmStrengthSpinner.Value,
      NlmPatchWing = (int)NlmPatchSpinner.Value,
      NlmSearchWing = (int)NlmSearchSpinner.Value,
      NlmChromaK = (double)NlmChromaSpinner.Value,
      NlmTwoPass = NlmTwoPassCheckBox.Checked,

      SkipNoiseOnlyBands = SkipNoiseBandsCheckBox.Checked
    };


    //----------------------------------------------------------------------------------------------
    //                                           filtering
    //----------------------------------------------------------------------------------------------
    private void ApplyBtn_Click(object sender, EventArgs e)
    {
      var options = CurrentOptions();

      if (options.Method == SstvDenoiseMethod.None)
      {
        Options = options;
        Result = Original;
        ShowImage(Original, "original");
        return;
      }

      // sub-second even for PD 290, so a wait cursor is the whole progress indication (D12)
      Cursor = Cursors.WaitCursor;
      try
      {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var filtered = Planes.Denoise(options).ToRgb();
        sw.Stop();

        Options = options;
        Result = filtered;
        string name = options.Method == SstvDenoiseMethod.Wiener ? "Wiener" : "non-local means";
        ShowImage(filtered, $"{name}, {sw.Elapsed.TotalSeconds:0.00} s");
      }
      catch (Exception ex)
      {
        Log.Error(ex, "SSTV denoise failed");
        MessageBox.Show(this, ex.Message, "Denoise Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
      finally
      {
        Cursor = Cursors.Default;
      }
    }

    private void OkBtn_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.OK;
      Close();
    }


    //----------------------------------------------------------------------------------------------
    //                                            preview
    //----------------------------------------------------------------------------------------------
    private void ShowImage(RgbImage image, string status)
    {
      var old = PreviewBox.Image;
      PreviewBox.Image = Scale2x(image);
      PreviewBox.Size = PreviewBox.Image.Size;
      old?.Dispose();
      CenterPreview();
      StatusLabel.Text = $"{image.Width} x {image.Height}  —  {status}";
    }

    private void PreviewPanel_ClientSizeChanged(object sender, EventArgs e) => CenterPreview();

    // center the picture while it is smaller than the pane, and pin it to the top left once it is larger and
    // the pane has become a scrolling viewport. The offset by AutoScrollPosition is what makes Location mean
    // the same thing in both cases: on a scrolled panel it is measured from the scrolled origin.
    private void CenterPreview()
    {
      if (PreviewBox.Image == null) return;
      var client = PreviewPanel.ClientSize;
      int x = Math.Max(0, (client.Width - PreviewBox.Width) / 2) + PreviewPanel.AutoScrollPosition.X;
      int y = Math.Max(0, (client.Height - PreviewBox.Height) / 2) + PreviewPanel.AutoScrollPosition.Y;
      PreviewBox.Location = new Point(x, y);
    }

    /// <summary>Pixel-double the picture for display. NEAREST NEIGHBOUR deliberately: this dialog exists to
    /// judge what a smoother did to fine detail, and any interpolating resampler would add smoothing of its
    /// own to the very thing being judged. Doubling rather than 1:1 because the artifacts at stake —
    /// residual speckle, the dash texture of §9.7, a stroke thinned by the Wiener — are one to three pixels
    /// across and simply are not resolvable at 1:1 on a modern display.</summary>
    private static Bitmap Scale2x(RgbImage image)
    {
      using var src = image.ToBitmap();
      var dst = new Bitmap(src.Width * 2, src.Height * 2);
      using var g = Graphics.FromImage(dst);
      g.InterpolationMode = InterpolationMode.NearestNeighbor;
      g.PixelOffsetMode = PixelOffsetMode.Half;
      g.DrawImage(src, 0, 0, dst.Width, dst.Height);
      return dst;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
      base.OnFormClosed(e);
      var img = PreviewBox.Image;
      PreviewBox.Image = null;
      img?.Dispose();
    }
  }
}

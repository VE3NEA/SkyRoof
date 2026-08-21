using System;
using System.Linq;
using Serilog;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  // Status panel for the current group's auto-selection (design-docs/auto_selection_plan.md §3.1).
  // Shows the current group, an ON/OFF toggle for the engine, Edit/Clear buttons, and live status.
  public partial class AutoSelectionPanel : DockContent
  {
    private readonly Context ctx = null!;

    // set while UpdateStatus writes control state, so the resulting events do not fight the user
    private bool updating;

    //----------------------------------------------------------------------------------------------
    //                                        startup
    //----------------------------------------------------------------------------------------------
    public AutoSelectionPanel()
    {
      InitializeComponent();
    }

    public AutoSelectionPanel(Context ctx)
    {
      InitializeComponent();

      this.ctx = ctx;
      Log.Information("Creating AutoSelectionPanel");

      ctx.AutoSelectionPanel = this;
      ctx.MainForm.AutoSelectionMNU.Checked = true;

      var tips = new VE3NEA.ToolTipEx();
      tips.SetToolTip(EnableCheck, "Turn automatic satellite selection on or off for the current group");
      tips.SetToolTip(EditBtn, "Edit the schedule of passes for the current group");

      UpdateStatus();
    }

    private void AutoSelectionPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing AutoSelectionPanel");

      // closing the panel stops the rotation - there is no longer anywhere to monitor or control it
      bool wasEnabled = ctx.AutoSelector.Enabled;
      ctx.AutoSelector.SetEnabled(false);

      ctx.AutoSelectionPanel = null;
      ctx.MainForm.AutoSelectionMNU.Checked = false;

      if (wasEnabled)
        MessageBox.Show(ctx.MainForm, "Auto-selection stopped because the Auto Selection panel was closed.",
          "Auto Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }




    //----------------------------------------------------------------------------------------------
    //                                      button events
    //----------------------------------------------------------------------------------------------
    private void EnableCheck_CheckedChanged(object sender, EventArgs e)
    {
      if (updating) return;

      ctx.AutoSelector.SetEnabled(EnableCheck.Checked);
      UpdateStatus();
    }

    private void EditBtn_Click(object sender, EventArgs e)
    {
      bool trackedBefore = ctx.AutoSelector.CurrentSchedule?.TrackAntenna == true;

      using var form = new AutoSelectionConfigForm(ctx);
      if (form.ShowDialog(this) == DialogResult.OK)
      {
        ctx.AutoSelector.ReapplyActivePass();               // apply a transmitter change to the active sat now
        ctx.AutoSelector.SetEnabled(true);                  // saving the schedule with OK starts auto-selection
        ctx.AutoSelector.ApplyTrackAntennaChange(trackedBefore);   // start or stop the antenna if the option changed
        UpdateStatus();
      }
    }




    //----------------------------------------------------------------------------------------------
    //                                        status
    //----------------------------------------------------------------------------------------------
    // refreshed once per second from MainForm.OneSecondTick
    public void UpdateStatus()
    {
      updating = true;

      bool canEnable = ctx.AutoSelector.CanEnable();
      EnableCheck.Enabled = canEnable;
      EnableCheck.Checked = ctx.AutoSelector.Enabled;
      EnableCheck.Text = $"Auto Selection: {(ctx.AutoSelector.Enabled ? "ON" : "OFF")}";

      // Highlight the toggle in lime while auto-selection is running, default look otherwise.
      // The lime is deliberately the same in both themes, but the themed button renderer paints
      // its own surface and ignores BackColor in the dark theme, so the running state also
      // switches to FlatStyle.Popup, which paints BackColor itself and gives the identical
      // #00FF00 in both themes. Its text must then be black: ControlText is white in the dark
      // theme, and white on lime is unreadable.
      bool running = ctx.AutoSelector.Enabled;
      EnableCheck.FlatStyle = running ? FlatStyle.Popup : FlatStyle.Standard;
      EnableCheck.UseVisualStyleBackColor = !running;
      EnableCheck.BackColor = running ? Color.Lime : SystemColors.Control;
      EnableCheck.ForeColor = running ? Color.Black : SystemColors.ControlText;

      var now = DateTime.UtcNow;

      var active = ctx.AutoSelector.ActivePass;
      if (active != null)
        ActiveLabel.Text = $"Active:  {active.Satellite.name}   LOS in {Utils.TimespanToString(active.EndTime - now, true)}";
      else
        ActiveLabel.Text = "Active:  none";

      var next = ctx.AutoSelector.GetNextSelection();
      if (next != null)
        NextLabel.Text = $"Next:  {next.Value.Pass.Satellite.name}   in {Utils.TimespanToString(next.Value.When - now, true)}";
      else
        NextLabel.Text = "Next:  none";

      if (ctx.AutoSelector.IsRecording)
        RecLabel.Text = $"Recording:  {RecordModeText(ctx.AutoSelector.SegmentRecordMode)}  ● {DateTime.Now - ctx.AutoSelector.SegmentStartLocal:mm\\:ss}";
      else
        RecLabel.Text = "Recording:  Off";

      updating = false;
    }

    private static string RecordModeText(RecordMode mode)
    {
      return mode switch
      {
        RecordMode.Audio => "Audio",
        RecordMode.Iq => "I/Q",
        _ => "Off"
      };
    }
  }
}

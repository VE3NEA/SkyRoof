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

      var tips = new ToolTip();
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
      using var form = new AutoSelectionConfigForm(ctx);
      if (form.ShowDialog(this) == DialogResult.OK)
      {
        ctx.AutoSelector.ReapplyActivePass();               // apply a transmitter change to the active sat now
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

      // highlight the toggle in lime while auto-selection is running, default look otherwise
      EnableCheck.UseVisualStyleBackColor = !ctx.AutoSelector.Enabled;
      EnableCheck.BackColor = ctx.AutoSelector.Enabled ? Color.Lime : SystemColors.Control;

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

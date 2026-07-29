using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CSCore.XAudio2;
using VE3NEA;

namespace SkyRoof
{
  // Config modal for the current group's auto-selection schedule (design-docs/auto_selection_plan.md §3.2).
  // A rotation tree (satellites as parents in priority order, their passes as leaves) with 3-state
  // checkboxes selects the passes to rotate through; per-satellite transmitter and record settings and
  // the overlap mode complete the schedule. OK writes Schedules[SelectedGroupId] (created on first Edit).
  public partial class AutoSelectionConfigForm : Form
  {
    // state-image indices for the 3-state node checkboxes
    private const int Unchecked = 0;
    private const int Checked = 1;
    private const int Indeterminate = 2;

    private readonly Context ctx;

    // per-satellite transmitter/record edits, keyed by sat_id, seeded for every non-geostationary group
    // satellite so the details combos always have a backing store even before a pass is ticked
    private readonly Dictionary<string, ScheduledSat> satEdits = new();

    // set while the form writes control state, so the resulting events are not read as user edits
    private bool updating;

    // the satellite whose details are shown in the right-hand pane, or null when a leaf is selected
    private SatnogsDbSatellite? currentSat;


    public AutoSelectionConfigForm(Context ctx)
    {
      InitializeComponent();

      this.ctx = ctx;
      RotationTree.StateImageList = BuildStateImages();

      RecordCombo.Items.AddRange(new object[] { "Off", "Audio", "I/Q" });
      TransmitterCombo.DisplayMember = "description";

      var tips = new ToolTip();
      tips.SetToolTip(RotationTree, "Check the passes to include in the rotation");
      tips.SetToolTip(MoveUpBtn, "Raise the selected satellite's priority");
      tips.SetToolTip(MoveDownBtn, "Lower the selected satellite's priority");
      tips.SetToolTip(TransmitterCombo, "Transmitter to tune when this satellite's pass is selected");
      tips.SetToolTip(RecordCombo, "Record each pass of this satellite as Audio, I/Q, or not at all");
      tips.SetToolTip(SelectAllBtn, "Check all passes of the current group");
      tips.SetToolTip(ClearBtn, "Uncheck all passes, clearing the schedule for the current group");

      // the checkbox is always usable, but warns in red that it does nothing until the rotator is enabled
      if (ctx.Settings.Rotator.Enabled)
        tips.SetToolTip(TrackAntennaCheckbox, "Rotate the antenna to point at and follow each selected pass");
      else
      {
        TrackAntennaCheckbox.ForeColor = Color.Red;
        tips.SetToolTip(TrackAntennaCheckbox,
          "Rotator control is disabled in Settings - antenna tracking will have no effect until you enable it");
      }

      LoadSchedule();
      UpdateDetails();
    }




    //----------------------------------------------------------------------------------------------
    //                                        load
    //----------------------------------------------------------------------------------------------
    // builds the tree and details state from the current group's schedule (an empty schedule when the
    // group has none yet), listing every non-geostationary group satellite as a parent in priority order
    private void LoadSchedule()
    {
      var schedule = ctx.AutoSelector.CurrentSchedule ?? new GroupSchedule();

      SetOverlapMode(schedule.OverlapMode);
      TrackAntennaCheckbox.Checked = schedule.TrackAntenna;

      var now = DateTime.UtcNow;
      var horizon = now.AddHours(48);

      // ticked passes and per-sat settings from the stored schedule
      var tickedKeys = new HashSet<(string, int)>(schedule.Passes.Select(p => (p.SatId, p.OrbitNumber)));

      // non-geostationary group satellites, ordered by schedule priority first, then the rest
      var groupSats = ctx.SatelliteSelector.GroupSatellites
        .Where(sat => !sat.Tracker.IsGeoStationary())
        .ToList();
      // scheduled satellites keep their priority order; the rest (all of them when there is no schedule)
      // are listed alphabetically
      var priority = schedule.Sats.Select((s, i) => (s.SatId, i)).ToDictionary(x => x.SatId, x => x.i);
      var orderedSats = groupSats
        .OrderBy(sat => priority.TryGetValue(sat.sat_id, out int i) ? i : int.MaxValue)
        .ThenBy(sat => sat.name)
        .ToList();

      RotationTree.BeginUpdate();
      foreach (var sat in orderedSats)
      {
        // seed the per-sat edit store from the schedule; for a satellite not yet in the schedule default
        // to the transmitter it last used (the same customization the manual selector reads), then its first
        var scheduled = schedule.Sats.FirstOrDefault(s => s.SatId == sat.sat_id);
        ctx.Settings.Satellites.SatelliteCustomizations.TryGetValue(sat.sat_id, out var cust);
        satEdits[sat.sat_id] = new ScheduledSat
        {
          SatId = sat.sat_id,
          TransmitterUuid = scheduled?.TransmitterUuid
            ?? cust?.SelectedTransmitterId
            ?? sat.Transmitters.FirstOrDefault()?.uuid,
          Record = scheduled?.Record ?? RecordMode.Off
        };

        var satNode = new TreeNode(sat.name) { Tag = sat };
        RotationTree.Nodes.Add(satNode);

        var passes = ctx.GroupPasses.Passes
          .Where(p => p.Satellite.sat_id == sat.sat_id && p.StartTime < horizon && p.EndTime > now)
          .OrderBy(p => p.StartTime);

        foreach (var pass in passes)
        {
          var leaf = new TreeNode(PassLabel(pass)) { Tag = pass };
          leaf.StateImageIndex = tickedKeys.Contains((pass.Satellite.sat_id, pass.OrbitNumber)) ? Checked : Unchecked;
          satNode.Nodes.Add(leaf);
        }

        UpdateParentState(satNode);
      }
      RotationTree.CollapseAll();                          // start with all satellites collapsed
      RotationTree.EndUpdate();

      if (RotationTree.Nodes.Count > 0) RotationTree.SelectedNode = RotationTree.Nodes[0];
    }

    private static string PassLabel(SatellitePass pass)
    {
      return $"{pass.StartTime.ToLocalTime():MM-dd HH:mm}   ·   {Utils.TimespanToString(pass.EndTime - pass.StartTime, false)}   ·   {pass.MaxElevation:F0}°";
    }

    // three checkbox glyphs (unchecked / checked / indeterminate) for the tree's StateImageList,
    // sliced from the 48x16 checkbox-3state strip (three 16x16 cells over a purple color key)
    private ImageList BuildStateImages()
    {
      var list = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
      var strip = Properties.Resources.checkbox_3state;
      for (int i = 0; i < 3; i++)
      {
        var glyph = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(glyph))
          g.DrawImage(strip, new Rectangle(0, 0, 16, 16), new Rectangle(i * 16, 0, 16, 16), GraphicsUnit.Pixel);
        glyph.MakeTransparent(Color.FromArgb(128, 0, 128));    // purple background -> transparent
        list.Images.Add(glyph);
      }
      return list;
    }




    //----------------------------------------------------------------------------------------------
    //                                     tree checkboxes
    //----------------------------------------------------------------------------------------------
    // toggle a node only when its state icon is clicked (the TreeView has no native indeterminate state)
    private void RotationTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
    {
      if (RotationTree.HitTest(e.Location).Location != TreeViewHitTestLocations.StateImage) return;

      if (e.Node.Level == 0) ToggleParent(e.Node);
      else ToggleLeaf(e.Node);

      // clicking the state-image region does not select the node, so the native TreeView never
      // repaints it after a checkbox toggle; force the change to show (batch buttons redraw via EndUpdate)
      RotationTree.Invalidate();
    }

    // a parent toggles all its leaves: checked/indeterminate -> clear all, unchecked -> check all
    private void ToggleParent(TreeNode parent)
    {
      int newState = parent.StateImageIndex == Checked ? Unchecked : Checked;
      foreach (TreeNode leaf in parent.Nodes) leaf.StateImageIndex = newState;
      parent.StateImageIndex = parent.Nodes.Count == 0 ? Unchecked : newState;
    }

    private void ToggleLeaf(TreeNode leaf)
    {
      leaf.StateImageIndex = leaf.StateImageIndex == Checked ? Unchecked : Checked;
      UpdateParentState(leaf.Parent);
    }

    // recomputes a parent's 3-state checkbox from how many of its leaves are checked
    private void UpdateParentState(TreeNode parent)
    {
      int total = parent.Nodes.Count;
      int checkedCount = parent.Nodes.Cast<TreeNode>().Count(n => n.StateImageIndex == Checked);

      parent.StateImageIndex = checkedCount == 0 ? Unchecked
        : checkedCount == total ? Checked
        : Indeterminate;
    }




    //----------------------------------------------------------------------------------------------
    //                                   satellite details
    //----------------------------------------------------------------------------------------------
    private void RotationTree_AfterSelect(object sender, TreeViewEventArgs e)
    {
      UpdateDetails();
    }

    // shows the transmitter/record settings of the selected satellite (a leaf selects its parent sat)
    private void UpdateDetails()
    {
      var node = RotationTree.SelectedNode;
      bool isSatNode = node?.Level == 0;
      var satNode = isSatNode ? node : node?.Parent;
      currentSat = satNode?.Tag as SatnogsDbSatellite;

      // the satellite-scoped controls apply to a satellite, so they are disabled when a pass is selected
      SatGroupBox.Enabled = isSatNode;
      MoveUpBtn.Enabled = isSatNode;
      MoveDownBtn.Enabled = isSatNode;

      if (currentSat == null) return;

      var edit = satEdits[currentSat.sat_id];

      updating = true;
      TransmitterCombo.Items.Clear();
      TransmitterCombo.Items.AddRange(currentSat.Transmitters.ToArray());
      TransmitterCombo.SelectedItem = currentSat.Transmitters.FirstOrDefault(t => t.uuid == edit.TransmitterUuid)
        ?? currentSat.Transmitters.FirstOrDefault();
      RecordCombo.SelectedIndex = (int)edit.Record;
      updating = false;

      // reconcile the stored uuid with the transmitter actually shown, so a stale/unmatched seed does not
      // survive to the saved schedule (the combo change event is suppressed while updating)
      if (TransmitterCombo.SelectedItem is SatnogsDbTransmitter shownTx)
        edit.TransmitterUuid = shownTx.uuid;
    }

    private void TransmitterCombo_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (updating || currentSat == null) return;
      if (TransmitterCombo.SelectedItem is SatnogsDbTransmitter tx)
        satEdits[currentSat.sat_id].TransmitterUuid = tx.uuid;
    }

    private void RecordCombo_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (updating || currentSat == null) return;
      satEdits[currentSat.sat_id].Record = (RecordMode)RecordCombo.SelectedIndex;
    }




    //----------------------------------------------------------------------------------------------
    //                                   priority reorder
    //----------------------------------------------------------------------------------------------
    private void MoveUpBtn_Click(object sender, EventArgs e)
    {
      MoveSelectedSat(-1);
    }

    private void MoveDownBtn_Click(object sender, EventArgs e)
    {
      MoveSelectedSat(+1);
    }

    // moves the selected satellite (parent) node up or down one position in the priority order
    private void MoveSelectedSat(int delta)
    {
      var node = RotationTree.SelectedNode;
      if (node == null) return;
      if (node.Level == 1) node = node.Parent;

      int index = node.Index + delta;
      if (index < 0 || index >= RotationTree.Nodes.Count) return;

      RotationTree.BeginUpdate();
      RotationTree.Nodes.Remove(node);
      RotationTree.Nodes.Insert(index, node);
      RotationTree.EndUpdate();

      RotationTree.SelectedNode = node;
    }




    //----------------------------------------------------------------------------------------------
    //                                     overlap mode
    //----------------------------------------------------------------------------------------------
    private void SetOverlapMode(OverlapMode mode)
    {
      FinishCurrentRadio.Checked = mode == OverlapMode.FinishCurrent;
      HighestElevationRadio.Checked = mode == OverlapMode.HighestElevation;
      PriorityRadio.Checked = mode == OverlapMode.Priority;
    }

    private OverlapMode GetOverlapMode()
    {
      if (HighestElevationRadio.Checked) return OverlapMode.HighestElevation;
      if (PriorityRadio.Checked) return OverlapMode.Priority;
      return OverlapMode.FinishCurrent;
    }




    //----------------------------------------------------------------------------------------------
    //                                        save
    //----------------------------------------------------------------------------------------------
    // checks every pass in the tree, so all of the current group's passes join the rotation
    private void SelectAllBtn_Click(object sender, EventArgs e)
    {
      SelectAbove(0);
    }

    private void SelectAbove(int minDegrees)
    {
      RotationTree.BeginUpdate();
      foreach (TreeNode satNode in RotationTree.Nodes)
      {
        foreach (TreeNode leaf in satNode.Nodes)
          if ((leaf.Tag as SatellitePass)!.MaxElevation >= minDegrees)
            leaf.StateImageIndex = Checked;

        UpdateParentState(satNode);
      }

      RotationTree.EndUpdate();
    }

    // unchecks every pass in the tree; OK then removes the now-empty schedule for the current group.
    // with no schedule there is no priority, so the satellites are re-listed alphabetically
    private void ClearBtn_Click(object sender, EventArgs e)
    {
      if (MessageBox.Show(this, "Clear the schedule for the current group?", "Auto Selection",
        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

      RotationTree.BeginUpdate();
      foreach (TreeNode satNode in RotationTree.Nodes)
      {
        foreach (TreeNode leaf in satNode.Nodes) leaf.StateImageIndex = Unchecked;
        satNode.StateImageIndex = Unchecked;
      }

      var sorted = RotationTree.Nodes.Cast<TreeNode>().OrderBy(n => n.Text).ToArray();
      RotationTree.Nodes.Clear();
      RotationTree.Nodes.AddRange(sorted);
      RotationTree.EndUpdate();

      if (RotationTree.Nodes.Count > 0) RotationTree.SelectedNode = RotationTree.Nodes[0];
    }

    // writes the tree and details state back into the current group's schedule
    private void OkBtn_Click(object sender, EventArgs e)
    {
      var schedule = new GroupSchedule { OverlapMode = GetOverlapMode(), TrackAntenna = TrackAntennaCheckbox.Checked };

      foreach (TreeNode satNode in RotationTree.Nodes)
      {
        var sat = (SatnogsDbSatellite)satNode.Tag;
        var tickedLeaves = satNode.Nodes.Cast<TreeNode>().Where(n => n.StateImageIndex == Checked).ToList();

        // a satellite participates in the rotation only if at least one of its passes is ticked
        if (tickedLeaves.Count == 0) continue;

        // persist a transmitter the satellite actually has, so a stale/unset seed cannot be saved
        var edit = satEdits[sat.sat_id];
        if (edit.TransmitterUuid == null || sat.Transmitters.All(t => t.uuid != edit.TransmitterUuid))
          edit.TransmitterUuid = sat.Transmitters.FirstOrDefault()?.uuid;

        schedule.Sats.Add(edit);
        foreach (var leaf in tickedLeaves)
        {
          var pass = (SatellitePass)leaf.Tag;
          schedule.Passes.Add(new ScheduledPass { SatId = pass.Satellite.sat_id, OrbitNumber = pass.OrbitNumber });
        }
      }

      var groupId = ctx.Settings.Satellites.SelectedGroupId;
      if (schedule.Sats.Count > 0)
        ctx.Settings.Satellites.AutoSelection.Schedules[groupId] = schedule;
      else
        ctx.Settings.Satellites.AutoSelection.Schedules.Remove(groupId);

      DialogResult = DialogResult.OK;
    }




    //----------------------------------------------------------------------------------------------
    //                                  select above menu
    //----------------------------------------------------------------------------------------------
    private void DropdownBtn_MouseDown(object sender, MouseEventArgs e)
    {
      SelectAllPopupMenu.Show(DropdownBtn, new Point(DropdownBtn.Width, DropdownBtn.Height), ToolStripDropDownDirection.BelowLeft);
    }

    private void SelectAboveMnu_Click(object sender, EventArgs e)
    {
      int minDegrees = int.TryParse((sender as ToolStripMenuItem)?.Tag as string, out int result) ? result : 0;
      SelectAbove(minDegrees);
    }
  }
}

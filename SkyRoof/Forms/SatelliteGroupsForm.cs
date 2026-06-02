using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using SGPdotNET.Observation;
using SGPdotNET.TLE;
using WeifenLuo.WinFormsUI.Docking;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SkyRoof
{
  public partial class SatelliteGroupsForm : Form
  {
    List<ListViewItem> AllItems = new();
    List<ListViewItem> FilteredItems;
    private string TextToSearch = "";
    private SatelliteFlags SearchFlags;
    private int SortColumn;
    private Context ctx;
    private bool Changed;
    private bool UpdatingChecks;

    public SatelliteGroupsForm()
    {
      InitializeComponent();
    }


    //----------------------------------------------------------------------------------------------
    //                                 sat list and groups
    //----------------------------------------------------------------------------------------------
    public void SetList(Context context)
    {
      ctx = context;
      var updateTime = ctx.Settings.Satellites.LastDownloadTime;

      // satellites to listview
      foreach (var sat in ctx.SatnogsDb.Satellites) AllItems.Add(ItemFromSat(sat));
      ApplyFilter();
      UpdatedDateLabel.Text = $"Updated {updateTime:yyyy-MM-dd}";

      // groups to treeview
      foreach (var group in ctx.Settings.Satellites.SatelliteGroups)
      {
        var node = new TreeNode(group.Name);
        node.Tag = group;
        treeView1.Nodes.Add(node);
        foreach (var satId in group.SatelliteIds)
        {
          var sat = ctx.SatnogsDb.GetSatellite(satId);
          if (sat != null) node.Nodes.Add(NodeFromSat(sat));
        }
        node.Expand();
      }
    }

    private void SortSatellites(int column)
    {
      SortColumn = column;

      switch (column)
      {
        // 0 = Name, 1 = NORAD, 2 = Launched, 3 = Service
        case 0:
          FilteredItems = FilteredItems.OrderBy(s => ((SatnogsDbSatellite)s.Tag).name).ToList();
          break;
        case 1:
          FilteredItems = FilteredItems.OrderBy(s => ((SatnogsDbSatellite)s.Tag).norad_cat_id).ToList();
          break;
        case 2:
          FilteredItems = FilteredItems.OrderBy(s => ((SatnogsDbSatellite)s.Tag).launched).ToList();
          break;
        case 3:
          FilteredItems = FilteredItems.OrderBy(s => s.SubItems[3].Text).ToList();
          break;
      }

      listView1.Invalidate();
    }

    private ListViewItem ItemFromSat(SatnogsDbSatellite sat)
    {
      var item = new ListViewItem([
        sat.name,
        sat.norad_cat_id!.ToString(),
        $"{sat.launched:yyyy-MM-dd}",
        string.Join(", ", sat.Transmitters.Select(t => t.service).Where(s=>s != "Unknown").Distinct().Order()),
        ]);

      item.Tag = sat;
      item.Checked = IsMonitored(sat);

      // highlighting

      if (sat.Flags.HasFlag(SatelliteFlags.Uhf)) item.BackColor = Color.LightCyan;
      else if (sat.Flags.HasFlag(SatelliteFlags.Vhf)) item.BackColor = Color.LightGoldenrodYellow;

      if (sat.Flags.HasFlag(SatelliteFlags.Ham)) item.Font = new(item.Font, FontStyle.Bold);
      if (!sat.status.StartsWith("alive")) item.ForeColor = Color.Silver;
      else if (sat.Tle == null) item.Font = new(item.Font, FontStyle.Strikeout);

      return item;
    }

    private TreeNode NodeFromSat(SatnogsDbSatellite sat)
    {
      var node = new TreeNode(sat.name);
      node.Tag = sat;
      node.ToolTipText = sat.GetTooltipText();

      // highlighting
      if (sat.Flags.HasFlag(SatelliteFlags.Uhf)) node.BackColor = Color.LightCyan;
      else if (sat.Flags.HasFlag(SatelliteFlags.Vhf)) node.BackColor = Color.LightGoldenrodYellow;

      if (!sat.Flags.HasFlag(SatelliteFlags.Ham)) node.NodeFont = new(treeView1.Font, FontStyle.Regular);
      if (!sat.status.StartsWith("alive")) node.ForeColor = Color.Silver;
      else if (sat.Tle == null) node.NodeFont = new(treeView1.Font, FontStyle.Strikeout);

      return node;
    }

    private void SaveGroups()
    {
      ctx.Settings.Satellites.SatelliteGroups.Clear();

      foreach (var groupNode in treeView1.Nodes.Cast<TreeNode>())
      {
        var group = new SatelliteGroup();
        group.Name = groupNode.Text;
        group.SatelliteIds = groupNode.Nodes.Cast<TreeNode>().Select(n => ((SatnogsDbSatellite)n.Tag).sat_id).ToList();
        if (group.SatelliteIds.Count > 0) ctx.Settings.Satellites.SatelliteGroups.Add(group);
      }

      ctx.Settings.Satellites.Sanitize();

      Changed = false;

      DialogResult = DialogResult.OK;
    }




    //----------------------------------------------------------------------------------------------
    //                                     filter
    //----------------------------------------------------------------------------------------------
    private void ApplyFilter()
    {
      UpdatingChecks = true;
      FilteredItems = AllItems.Where(IsItemVisible).ToList();
      listView1.VirtualListSize = FilteredItems.Count;
      CountLabel.Text = $"{FilteredItems.Count} of {AllItems.Count}";
      SortSatellites(SortColumn);
      UpdatingChecks = false;
    }

    private bool IsItemVisible(ListViewItem item)
    {
      var sat = (SatnogsDbSatellite)item.Tag;

      bool statusOk =
        (AliveCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.Alive)) ||
        (FutureCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.Future)) ||
        (ReEnteredCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.ReEntered));

      bool hamOk =
      (HamCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.Ham)) ||
      (NonHamCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.NonHam));

      bool bandOk =
        (VhfCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.Vhf)) ||
        (UhfCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.Uhf)) ||
        (OtherBandsCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.OtherBands));

      bool txOk =
      (TransponderCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.Transponder)) ||
      (TransceiverCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.Transceiver)) ||
      (TransmitterCheckbox.Checked && sat.Flags.HasFlag(SatelliteFlags.Transmitter));


      bool filterOk = (TextToSearch == "") || (sat.SearchText.Contains(TextToSearch));

      return statusOk && hamOk && bandOk && txOk && filterOk;
    }

    private void FilterChanged(object sender, EventArgs e)
    {
      TextToSearch = SatnogsDbSatellite.MakeSearchText(FilterTextbox.Text);
      ApplyFilter();
    }




    //----------------------------------------------------------------------------------------------
    //                                     listview
    //----------------------------------------------------------------------------------------------
    private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
    {
      e.Item = FilteredItems[e.ItemIndex];
    }

    private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
    {
      SortSatellites(e.Column);
    }

    private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
    {
      if (UpdatingChecks) return;
      if (e.Item?.Tag is not SatnogsDbSatellite sat) return;
      SetMonitored(sat, e.Item.Checked);
    }

    // sat rename finished
    private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
    {
      // new name
      if (e.Label == null) return;
      string newName = e.Label.Trim();
      e.CancelEdit = newName == "";
      if (e.CancelEdit) return;

      // to sat object
      var sat = (SatnogsDbSatellite)FilteredItems[listView1.SelectedIndices[0]].Tag;
      RenameSat(sat, newName);

      // update name in treeview
      var allNodes = treeView1.Nodes.Cast<TreeNode>().SelectMany(n => n.Nodes.Cast<TreeNode>());
      var node = allNodes.Cast<TreeNode>().FirstOrDefault(n => n.Tag == sat);
      if (node != null) node.Text = newName;
    }




    //----------------------------------------------------------------------------------------------
    //                                     treeview
    //----------------------------------------------------------------------------------------------
    private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
      if (e.Node.Level == 0) return;
      treeView1.Nodes.Remove(e.Node);
    }

    private void treeView1_MouseDown(object sender, MouseEventArgs e)
    {
      // if right-clicked on an unselected node, select it
      treeView1.SelectedNode = treeView1.HitTest(e.X, e.Y).Node;

      // announce sat name
      //if (treeView1.SelectedNode != null && treeView1.SelectedNode.Level > 0)
      //  Announcer.SaySatName(((SatnogsDbSatellite)treeView1.SelectedNode.Tag).name);
    }

    // sat or group rename finished
    private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
    {
      // prwvent auto switching to edit mode
      treeView1.LabelEdit = false;

      // new name
      if (e.Label == null) return;
      string newName = e.Label.Trim();
      e.CancelEdit = newName == "";
      if (e.CancelEdit) return;

      // if group
      if (treeView1.SelectedNode.Level == 0)
      {
        Changed = true;
        return;
      }

      // if sat, update object
      var sat = (SatnogsDbSatellite)treeView1.SelectedNode.Tag;
      RenameSat(sat, newName);

      // update sat name in listview
      var item = AllItems.FirstOrDefault(item => item.Tag == sat);
      if (item != null)
      {
        item.Text = sat.name;
        listView1.Invalidate();
      }
    }





    //----------------------------------------------------------------------------------------------
    //                                     buttons
    //----------------------------------------------------------------------------------------------
    private void ClearSearchBtn_Click(object sender, EventArgs e)
    {
      FilterTextbox.Text = "";
    }

    private void AddGroupBtn_Click(object sender, EventArgs e)
    {
      var node = new TreeNode("New Group");
      treeView1.Nodes.Add(node);
      treeView1.SelectedNode = node;
      treeView1.LabelEdit = true;
      node.BeginEdit();

      Changed = true;
    }

    private void AddSatBtn_Click(object sender, EventArgs e)
    {
      var sats = listView1.SelectedIndices.Cast<int>().Select(i => (SatnogsDbSatellite)FilteredItems[i].Tag).ToList();
      var dst = treeView1.SelectedNode;
      AddSatellitesToTree(sats, dst);
    }

    private void DeleteSatBtn_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Level != 1) { Console.Beep(); return; }
      treeView1.Nodes.Remove(treeView1.SelectedNode);

      Changed = true;
    }

    private void OkBtn_Click(object sender, EventArgs e)
    {
      SaveGroups();
    }




    //----------------------------------------------------------------------------------------------
    //                                        menu
    //----------------------------------------------------------------------------------------------

    // keyboard shortcuts for menu items
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      if (listView1.Focused && keyData == Keys.Right || treeView1.Focused && keyData == Keys.Space)
      {
        AddSatBtn_Click(null, null);
        return true;
      }

      return base.ProcessCmdKey(ref msg, keyData);
    }

    private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
    {
      e.Cancel = treeView1.SelectedNode == null;
      ClearGroupMNU.Visible = treeView1.SelectedNode?.Level == 0;
      DetailsMNU2.Visible = treeView1.SelectedNode?.Level == 1;
    }

    private void RenameSatMNU_Click(object sender, EventArgs e)
    {
      var item = FilteredItems[listView1.SelectedIndices[0]];
      item.BeginEdit();
    }

    private void DetailsMNU_Click(object sender, EventArgs e)
    {
      var item = FilteredItems[listView1.SelectedIndices[0]];
      var sat = (SatnogsDbSatellite)item.Tag;

      ShowSatelliteDetails(sat);
    }

    private void DetailsMNU2_Click(object sender, EventArgs e)
    {
      if (treeView1.SelectedNode?.Level != 1) return;
      var sat = (SatnogsDbSatellite)treeView1.SelectedNode.Tag;

      ShowSatelliteDetails(sat);
    }

    private void ClearGroupMNU_Click(object sender, EventArgs e)
    {
      treeView1.SelectedNode.Nodes.Clear();
      Changed = true;
    }

    private void RenameMNU2_Click(object sender, EventArgs e)
    {
      treeView1.LabelEdit = true;
      treeView1.SelectedNode?.BeginEdit();
      Changed = true;
    }

    private void DeleteMNU2_Click(object sender, EventArgs e)
    {
      treeView1.Nodes.Remove(treeView1.SelectedNode);
      Changed = true;
    }






    //----------------------------------------------------------------------------------------------
    //                                    commands       
    //----------------------------------------------------------------------------------------------
    private void ShowSatelliteDetails(SatnogsDbSatellite sat)
    {
      SatelliteDetailsForm.ShowSatellite(sat, ParentForm);
    }

    private void RenameSat(SatnogsDbSatellite sat, string name)
    {
      sat.name = name;
      sat.BuildAllNames();

      // to customization storage
      var casts = ctx.Settings.Satellites.SatelliteCustomizations;
      casts.TryAdd(sat.sat_id, new());
      casts[sat.sat_id].sat_id = sat.sat_id;
      casts[sat.sat_id].Name = name;
    }

    private void SatelliteGroupsForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (Changed && MessageBox.Show("Save changes?", "SkyRoof", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        SaveGroups();
    }

    private bool IsMonitored(SatnogsDbSatellite sat) => IsMonitored(sat.sat_id);
    private bool IsMonitored(string satId) => ctx.Settings.Satellites.MonitoredSatelliteIds.Contains(satId);

    private void SetMonitored(SatnogsDbSatellite sat, bool monitored)
    {
      var list = ctx.Settings.Satellites.MonitoredSatelliteIds;
      if (monitored)
      {
        if (!list.Contains(sat.sat_id))
          list.Add(sat.sat_id); // add as lowest priority; can be reprioritized in the monitored panel
      }
      else
        list.RemoveAll(id => id == sat.sat_id);

      ctx.Settings.SaveToFile();
      ctx.MonitoredPasses?.FullRebuild();
      ctx.MonitoredSatellitesPanel?.RefreshList();

      listView1.Invalidate();
      Changed = true;
    }


    //----------------------------------------------------------------------------------------------
    //                                    drg and drop
    //----------------------------------------------------------------------------------------------
    private void treeView1_DragEnter(object sender, DragEventArgs e)
    {
      e.Effect = e.AllowedEffect;
    }

    private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
    {
      var effect = ModifierKeys.HasFlag(Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move;
      DoDragDrop(e.Item, DragDropEffects.All);
    }

    private void treeView1_DragOver(object sender, DragEventArgs e)
    {
      var dst = treeView1.GetNodeAt(treeView1.PointToClient(new Point(e.X, e.Y)));
      treeView1.SelectedNode = dst;

      if (CanDrop(e.Data, dst))
        e.Effect = ModifierKeys.HasFlag(Keys.Control) ? DragDropEffects.Copy : DragDropEffects.Move;
      else
        e.Effect = DragDropEffects.None;
    }

    private void treeView1_DragDrop(object sender, DragEventArgs e)
    {
      var point = treeView1.PointToClient(new Point(e.X, e.Y));
      var dst = treeView1.GetNodeAt(point);
      var node = e.Data.GetData(typeof(TreeNode)) as TreeNode;
      var sats = e.Data.GetData(typeof(List<SatnogsDbSatellite>)) as List<SatnogsDbSatellite>;

      if (node != null) MoveOrCopyNode(node, dst, e.Effect);
      else if (sats != null) AddSatellitesToTree(sats, dst);
    }

    // aloow dragging from sat list
    private void listView1_ItemDrag(object sender, ItemDragEventArgs e)
    {
      var sats = listView1.SelectedIndices.Cast<int>().Select(idx => (SatnogsDbSatellite)FilteredItems[idx].Tag).ToList();
      DoDragDrop(sats, DragDropEffects.All);
    }

    private void AddSatellitesToTree(List<SatnogsDbSatellite> sats, TreeNode dst)
    {
      if (sats.Any(sat => !sat.Flags.HasFlag(SatelliteFlags.Alive)))
      {
        MessageBox.Show("You can only add alive satellites to groups.", "SkyRoof", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      // default to first group
      if (dst == null && treeView1.Nodes.Count > 0) dst = treeView1.Nodes[0];

      var existingNodes = dst.Level == 0 ? dst.Nodes : dst.Parent.Nodes;
      var existingSats = existingNodes.Cast<TreeNode>().Select(n => (SatnogsDbSatellite)n.Tag).ToList();
      var newNodes = sats.Except(existingSats).Select(NodeFromSat).ToList();

      foreach (var node in newNodes)
        if (dst.Level == 0) existingNodes.Add(node);
        else existingNodes.Insert(dst.Index, node);

      dst.Expand();
      Changed = true;
    }

    private void MoveOrCopyNode(TreeNode? src, TreeNode dst, DragDropEffects effect)
    {
      bool isGroup = src.Level == 0;

      if (effect == DragDropEffects.Move)
        src.Remove();
      else
      {
        src = src.Clone() as TreeNode;
        if (isGroup) src.Text += " (copy)";
      }

      // dragging group
      if (isGroup) treeView1.Nodes.Insert(dst.Index, src);

      // dragging sat to group
      else if (dst.Level == 0) dst.Nodes.Insert(dst.Index, src);

      // dragging sat to sibling
      else dst.Parent.Nodes.Insert(dst.Index, src);

      Changed = true;
    }

    private bool CanDrop(IDataObject? data, TreeNode dst)
    {
      if (dst == null) return false;
      var dstSats = (dst.Level == 1 ? dst.Parent : dst).Nodes.Cast<TreeNode>().Select(n => n.Tag);

      // dragging treeview node
      var src = data.GetData(typeof(TreeNode)) as TreeNode;
      if (src != null)
      {
        // do not drop on itself, its own parent or next node, do not drop group on sat
        bool ok = !dst.Equals(src) && !dst.Equals(src.Parent) && !dst.Equals(src.NextNode) && dst.Level <= src.Level;
        if (!ok) return false;

        // dragging group
        if (src.Level == 0) return true;

        // dropping sat on another group
        if (!dstSats.Contains(src.Tag)) return true;

        // dropping in sibling 
        if (dst.Level == 1) return dst.Parent.Equals(src.Parent);
      }

      // dragging multiple listview items
      var srcSats = data.GetData(typeof(List<SatnogsDbSatellite>)) as List<SatnogsDbSatellite>;
      if (srcSats == null) return false;
      return srcSats.Except(dstSats).Count() > 0;
    }


    //{!} for debugging
    private void toolStripMenuItem1_Click(object sender, EventArgs e)
    {
      var item = FilteredItems[listView1.SelectedIndices[0]];
      var sat = (SatnogsDbSatellite)item.Tag;
      Clipboard.SetText($"{sat?.norad_cat_id}");

    }
  }
}

namespace SkyRoof
{
  partial class SatelliteGroupsForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      components = new System.ComponentModel.Container();
      panel1 = new Panel();
      treeView2 = new TreeView();
      panel8 = new Panel();
      AddGroupBtn = new Button();
      DeleteSatBtn = new Button();
      AddSatBtn = new Button();
      panel2 = new Panel();
      treeView1 = new TreeView();
      GroupsTreePopupMenu = new ContextMenuStrip(components);
      RenameMNU2 = new ToolStripMenuItem();
      DeleteMNU2 = new ToolStripMenuItem();
      DetailsMNU2 = new ToolStripMenuItem();
      ClearGroupMNU = new ToolStripMenuItem();
      panel3 = new Panel();
      CancelBtn = new Button();
      OkBtn = new Button();
      label2 = new Label();
      SatelliteListPopupMenu = new ContextMenuStrip(components);
      RenameSatMNU = new ToolStripMenuItem();
      AddToGroupMNU = new ToolStripMenuItem();
      DetailsMNU = new ToolStripMenuItem();
      toolStripMenuItem1 = new ToolStripMenuItem();
      panel5 = new Panel();
      listView1 = new ListView();
      columnHeader1 = new ColumnHeader();
      columnHeader2 = new ColumnHeader();
      columnHeader3 = new ColumnHeader();
      columnHeader4 = new ColumnHeader();
      panel7 = new Panel();
      CountLabel = new Label();
      panel6 = new Panel();
      flowLayoutPanel5 = new FlowLayoutPanel();
      label3 = new Label();
      FilterTextbox = new TextBox();
      ClearSearchBtn = new Button();
      UpdatedDateLabel = new Label();
      flowLayoutPanel4 = new FlowLayoutPanel();
      label5 = new Label();
      HamCheckbox = new CheckBox();
      NonHamCheckbox = new CheckBox();
      flowLayoutPanel3 = new FlowLayoutPanel();
      label4 = new Label();
      VhfCheckbox = new CheckBox();
      UhfCheckbox = new CheckBox();
      OtherBandsCheckbox = new CheckBox();
      flowLayoutPanel2 = new FlowLayoutPanel();
      label6 = new Label();
      TransponderCheckbox = new CheckBox();
      TransceiverCheckbox = new CheckBox();
      TransmitterCheckbox = new CheckBox();
      flowLayoutPanel1 = new FlowLayoutPanel();
      label7 = new Label();
      AliveCheckbox = new CheckBox();
      FutureCheckbox = new CheckBox();
      ReEnteredCheckbox = new CheckBox();
      label1 = new Label();
      toolTip1 = new ToolTip(components);
      panel1.SuspendLayout();
      panel8.SuspendLayout();
      panel2.SuspendLayout();
      GroupsTreePopupMenu.SuspendLayout();
      panel3.SuspendLayout();
      SatelliteListPopupMenu.SuspendLayout();
      panel5.SuspendLayout();
      panel7.SuspendLayout();
      panel6.SuspendLayout();
      flowLayoutPanel5.SuspendLayout();
      flowLayoutPanel4.SuspendLayout();
      flowLayoutPanel3.SuspendLayout();
      flowLayoutPanel2.SuspendLayout();
      flowLayoutPanel1.SuspendLayout();
      SuspendLayout();
      // 
      // panel1
      // 
      panel1.Controls.Add(treeView2);
      panel1.Controls.Add(panel8);
      panel1.Dock = DockStyle.Left;
      panel1.Location = new Point(564, 0);
      panel1.Name = "panel1";
      panel1.Size = new Size(33, 822);
      panel1.TabIndex = 1;
      // 
      // treeView2
      // 
      treeView2.Dock = DockStyle.Fill;
      treeView2.Location = new Point(32, 0);
      treeView2.Name = "treeView2";
      treeView2.Size = new Size(1, 822);
      treeView2.TabIndex = 3;
      // 
      // panel8
      // 
      panel8.Controls.Add(AddGroupBtn);
      panel8.Controls.Add(DeleteSatBtn);
      panel8.Controls.Add(AddSatBtn);
      panel8.Dock = DockStyle.Left;
      panel8.Location = new Point(0, 0);
      panel8.Name = "panel8";
      panel8.Size = new Size(32, 822);
      panel8.TabIndex = 4;
      // 
      // AddGroupBtn
      // 
      AddGroupBtn.Font = new Font("Segoe UI", 9F);
      AddGroupBtn.Location = new Point(2, 162);
      AddGroupBtn.Name = "AddGroupBtn";
      AddGroupBtn.Size = new Size(27, 25);
      AddGroupBtn.TabIndex = 3;
      AddGroupBtn.Text = "+";
      toolTip1.SetToolTip(AddGroupBtn, "Add Group");
      AddGroupBtn.UseVisualStyleBackColor = true;
      AddGroupBtn.Click += AddGroupBtn_Click;
      // 
      // DeleteSatBtn
      // 
      DeleteSatBtn.Font = new Font("Segoe UI", 9F);
      DeleteSatBtn.Location = new Point(2, 224);
      DeleteSatBtn.Name = "DeleteSatBtn";
      DeleteSatBtn.Size = new Size(27, 25);
      DeleteSatBtn.TabIndex = 2;
      DeleteSatBtn.Text = "<";
      toolTip1.SetToolTip(DeleteSatBtn, "Remove Satellite");
      DeleteSatBtn.UseVisualStyleBackColor = true;
      DeleteSatBtn.Click += DeleteSatBtn_Click;
      // 
      // AddSatBtn
      // 
      AddSatBtn.Font = new Font("Segoe UI", 9F);
      AddSatBtn.Location = new Point(2, 193);
      AddSatBtn.Name = "AddSatBtn";
      AddSatBtn.Size = new Size(27, 25);
      AddSatBtn.TabIndex = 1;
      AddSatBtn.Text = ">";
      toolTip1.SetToolTip(AddSatBtn, "Add Satellite");
      AddSatBtn.UseVisualStyleBackColor = true;
      AddSatBtn.Click += AddSatBtn_Click;
      // 
      // panel2
      // 
      panel2.Controls.Add(treeView1);
      panel2.Controls.Add(panel3);
      panel2.Controls.Add(label2);
      panel2.Dock = DockStyle.Fill;
      panel2.Location = new Point(597, 0);
      panel2.Name = "panel2";
      panel2.Size = new Size(219, 822);
      panel2.TabIndex = 2;
      // 
      // treeView1
      // 
      treeView1.AllowDrop = true;
      treeView1.ContextMenuStrip = GroupsTreePopupMenu;
      treeView1.Dock = DockStyle.Fill;
      treeView1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
      treeView1.HideSelection = false;
      treeView1.Location = new Point(0, 26);
      treeView1.Name = "treeView1";
      treeView1.ShowNodeToolTips = true;
      treeView1.Size = new Size(219, 766);
      treeView1.TabIndex = 1;
      treeView1.AfterLabelEdit += treeView1_AfterLabelEdit;
      treeView1.ItemDrag += treeView1_ItemDrag;
      treeView1.NodeMouseDoubleClick += treeView1_NodeMouseDoubleClick;
      treeView1.DragDrop += treeView1_DragDrop;
      treeView1.DragEnter += treeView1_DragEnter;
      treeView1.DragOver += treeView1_DragOver;
      treeView1.MouseDown += treeView1_MouseDown;
      // 
      // GroupsTreePopupMenu
      // 
      GroupsTreePopupMenu.Items.AddRange(new ToolStripItem[] { RenameMNU2, DeleteMNU2, DetailsMNU2, ClearGroupMNU });
      GroupsTreePopupMenu.Name = "SatelliteListPopupMenu";
      GroupsTreePopupMenu.Size = new Size(183, 92);
      GroupsTreePopupMenu.Opening += contextMenuStrip1_Opening;
      // 
      // RenameMNU2
      // 
      RenameMNU2.Name = "RenameMNU2";
      RenameMNU2.ShortcutKeyDisplayString = "";
      RenameMNU2.ShortcutKeys = Keys.F2;
      RenameMNU2.Size = new Size(182, 22);
      RenameMNU2.Text = "Rename";
      RenameMNU2.Click += RenameMNU2_Click;
      // 
      // DeleteMNU2
      // 
      DeleteMNU2.Name = "DeleteMNU2";
      DeleteMNU2.ShortcutKeys = Keys.Delete;
      DeleteMNU2.Size = new Size(182, 22);
      DeleteMNU2.Text = "Delete";
      DeleteMNU2.Click += DeleteMNU2_Click;
      // 
      // DetailsMNU2
      // 
      DetailsMNU2.Name = "DetailsMNU2";
      DetailsMNU2.ShortcutKeyDisplayString = "";
      DetailsMNU2.ShortcutKeys = Keys.Control | Keys.D;
      DetailsMNU2.Size = new Size(182, 22);
      DetailsMNU2.Text = "Details...";
      DetailsMNU2.Click += DetailsMNU2_Click;
      // 
      // ClearGroupMNU
      // 
      ClearGroupMNU.Name = "ClearGroupMNU";
      ClearGroupMNU.ShortcutKeys = Keys.Control | Keys.W;
      ClearGroupMNU.Size = new Size(182, 22);
      ClearGroupMNU.Text = "Clear Group";
      ClearGroupMNU.Click += ClearGroupMNU_Click;
      // 
      // panel3
      // 
      panel3.Controls.Add(CancelBtn);
      panel3.Controls.Add(OkBtn);
      panel3.Dock = DockStyle.Bottom;
      panel3.Location = new Point(0, 792);
      panel3.Name = "panel3";
      panel3.Size = new Size(219, 30);
      panel3.TabIndex = 0;
      // 
      // CancelBtn
      // 
      CancelBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      CancelBtn.Location = new Point(136, 3);
      CancelBtn.Name = "CancelBtn";
      CancelBtn.Size = new Size(75, 23);
      CancelBtn.TabIndex = 1;
      CancelBtn.Text = "Cancel";
      CancelBtn.UseVisualStyleBackColor = true;
      // 
      // OkBtn
      // 
      OkBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      OkBtn.Location = new Point(55, 3);
      OkBtn.Name = "OkBtn";
      OkBtn.Size = new Size(75, 23);
      OkBtn.TabIndex = 0;
      OkBtn.Text = "OK";
      OkBtn.UseVisualStyleBackColor = true;
      OkBtn.Click += OkBtn_Click;
      // 
      // label2
      // 
      label2.Dock = DockStyle.Top;
      label2.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
      label2.Location = new Point(0, 0);
      label2.Name = "label2";
      label2.Size = new Size(219, 26);
      label2.TabIndex = 3;
      label2.Text = " Satellite Groups";
      // 
      // SatelliteListPopupMenu
      // 
      SatelliteListPopupMenu.Items.AddRange(new ToolStripItem[] { RenameSatMNU, AddToGroupMNU, DetailsMNU, toolStripMenuItem1 });
      SatelliteListPopupMenu.Name = "SatelliteListPopupMenu";
      SatelliteListPopupMenu.Size = new Size(205, 114);
      // 
      // RenameSatMNU
      // 
      RenameSatMNU.Name = "RenameSatMNU";
      RenameSatMNU.ShortcutKeyDisplayString = "";
      RenameSatMNU.ShortcutKeys = Keys.F2;
      RenameSatMNU.Size = new Size(204, 22);
      RenameSatMNU.Text = "Rename";
      RenameSatMNU.Click += RenameSatMNU_Click;
      // 
      // AddToGroupMNU
      // 
      AddToGroupMNU.Name = "AddToGroupMNU";
      AddToGroupMNU.ShortcutKeyDisplayString = "";
      AddToGroupMNU.ShortcutKeys = Keys.Insert;
      AddToGroupMNU.Size = new Size(204, 22);
      AddToGroupMNU.Text = "Add to Group";
      AddToGroupMNU.Click += AddSatBtn_Click;
      // 
      // DetailsMNU
      // 
      DetailsMNU.Name = "DetailsMNU";
      DetailsMNU.ShortcutKeyDisplayString = "";
      DetailsMNU.ShortcutKeys = Keys.Control | Keys.D;
      DetailsMNU.Size = new Size(204, 22);
      DetailsMNU.Text = "Satellite Details...";
      DetailsMNU.Click += DetailsMNU_Click;
      // 
      // toolStripMenuItem1
      // 
      toolStripMenuItem1.Name = "toolStripMenuItem1";
      toolStripMenuItem1.ShortcutKeys = Keys.Control | Keys.C;
      toolStripMenuItem1.Size = new Size(204, 22);
      toolStripMenuItem1.Text = "Copy NORAD ID";
      toolStripMenuItem1.Click += toolStripMenuItem1_Click;
      // 
      // panel5
      // 
      panel5.Controls.Add(listView1);
      panel5.Controls.Add(panel7);
      panel5.Controls.Add(panel6);
      panel5.Controls.Add(label1);
      panel5.Dock = DockStyle.Left;
      panel5.Location = new Point(0, 0);
      panel5.Name = "panel5";
      panel5.Size = new Size(564, 822);
      panel5.TabIndex = 1;
      // 
      // listView1
      // 
      listView1.Activation = ItemActivation.OneClick;
      listView1.AllowDrop = true;
      listView1.CheckBoxes = true;
      listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
      listView1.ContextMenuStrip = SatelliteListPopupMenu;
      listView1.Dock = DockStyle.Fill;
      listView1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
      listView1.FullRowSelect = true;
      listView1.LabelEdit = true;
      listView1.Location = new Point(0, 127);
      listView1.Name = "listView1";
      listView1.ShowGroups = false;
      listView1.ShowItemToolTips = true;
      listView1.Size = new Size(564, 665);
      listView1.Sorting = SortOrder.Ascending;
      listView1.TabIndex = 7;
      listView1.UseCompatibleStateImageBehavior = false;
      listView1.View = View.Details;
      listView1.VirtualMode = true;
      listView1.AfterLabelEdit += listView1_AfterLabelEdit;
      listView1.ColumnClick += listView1_ColumnClick;
      listView1.ItemDrag += listView1_ItemDrag;
      listView1.RetrieveVirtualItem += listView1_RetrieveVirtualItem;
      listView1.ItemChecked += listView1_ItemChecked;
      listView1.DoubleClick += AddSatBtn_Click;
      // 
      // columnHeader1
      // 
      columnHeader1.Text = "Name";
      columnHeader1.Width = 180;
      // 
      // columnHeader2
      // 
      columnHeader2.Text = "NORAD ID";
      columnHeader2.Width = 80;
      // 
      // columnHeader3
      // 
      columnHeader3.Text = "Launched";
      columnHeader3.Width = 100;
      // 
      // columnHeader4
      // 
      columnHeader4.Text = "Service";
      columnHeader4.Width = 170;
      // 
      // panel7
      // 
      panel7.Controls.Add(CountLabel);
      panel7.Dock = DockStyle.Bottom;
      panel7.Location = new Point(0, 792);
      panel7.Name = "panel7";
      panel7.Size = new Size(564, 30);
      panel7.TabIndex = 6;
      // 
      // CountLabel
      // 
      CountLabel.AutoSize = true;
      CountLabel.Location = new Point(11, 7);
      CountLabel.Name = "CountLabel";
      CountLabel.Size = new Size(13, 15);
      CountLabel.TabIndex = 0;
      CountLabel.Text = "0";
      // 
      // panel6
      // 
      panel6.Controls.Add(flowLayoutPanel5);
      panel6.Controls.Add(flowLayoutPanel4);
      panel6.Controls.Add(flowLayoutPanel3);
      panel6.Controls.Add(flowLayoutPanel2);
      panel6.Controls.Add(flowLayoutPanel1);
      panel6.Dock = DockStyle.Top;
      panel6.Location = new Point(0, 21);
      panel6.Name = "panel6";
      panel6.Size = new Size(564, 106);
      panel6.TabIndex = 1;
      // 
      // flowLayoutPanel5
      // 
      flowLayoutPanel5.BackColor = Color.Gainsboro;
      flowLayoutPanel5.BorderStyle = BorderStyle.FixedSingle;
      flowLayoutPanel5.Controls.Add(label3);
      flowLayoutPanel5.Controls.Add(FilterTextbox);
      flowLayoutPanel5.Controls.Add(ClearSearchBtn);
      flowLayoutPanel5.Controls.Add(UpdatedDateLabel);
      flowLayoutPanel5.Location = new Point(7, 69);
      flowLayoutPanel5.Name = "flowLayoutPanel5";
      flowLayoutPanel5.Size = new Size(546, 30);
      flowLayoutPanel5.TabIndex = 1;
      // 
      // label3
      // 
      label3.AutoSize = true;
      label3.Dock = DockStyle.Left;
      label3.Location = new Point(3, 0);
      label3.Name = "label3";
      label3.Size = new Size(45, 29);
      label3.TabIndex = 11;
      label3.Text = "Search:";
      label3.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // FilterTextbox
      // 
      FilterTextbox.Location = new Point(54, 3);
      FilterTextbox.Name = "FilterTextbox";
      FilterTextbox.Size = new Size(167, 23);
      FilterTextbox.TabIndex = 1;
      FilterTextbox.TextChanged += FilterChanged;
      // 
      // ClearSearchBtn
      // 
      ClearSearchBtn.Font = new Font("Wingdings 2", 9F, FontStyle.Regular, GraphicsUnit.Point, 2);
      ClearSearchBtn.ForeColor = SystemColors.ControlDarkDark;
      ClearSearchBtn.Location = new Point(227, 3);
      ClearSearchBtn.Name = "ClearSearchBtn";
      ClearSearchBtn.Size = new Size(23, 22);
      ClearSearchBtn.TabIndex = 12;
      ClearSearchBtn.Text = "Ò";
      toolTip1.SetToolTip(ClearSearchBtn, "Clear Search String");
      ClearSearchBtn.UseVisualStyleBackColor = true;
      ClearSearchBtn.Click += ClearSearchBtn_Click;
      // 
      // UpdatedDateLabel
      // 
      UpdatedDateLabel.Location = new Point(256, 0);
      UpdatedDateLabel.Name = "UpdatedDateLabel";
      UpdatedDateLabel.Size = new Size(281, 23);
      UpdatedDateLabel.TabIndex = 13;
      UpdatedDateLabel.Text = "Updated";
      UpdatedDateLabel.TextAlign = ContentAlignment.BottomRight;
      // 
      // flowLayoutPanel4
      // 
      flowLayoutPanel4.BackColor = Color.Gainsboro;
      flowLayoutPanel4.BorderStyle = BorderStyle.FixedSingle;
      flowLayoutPanel4.Controls.Add(label5);
      flowLayoutPanel4.Controls.Add(HamCheckbox);
      flowLayoutPanel4.Controls.Add(NonHamCheckbox);
      flowLayoutPanel4.Location = new Point(347, 38);
      flowLayoutPanel4.Name = "flowLayoutPanel4";
      flowLayoutPanel4.Size = new Size(206, 25);
      flowLayoutPanel4.TabIndex = 16;
      // 
      // label5
      // 
      label5.AutoSize = true;
      label5.Dock = DockStyle.Left;
      label5.Location = new Point(3, 0);
      label5.Name = "label5";
      label5.Size = new Size(47, 25);
      label5.TabIndex = 6;
      label5.Text = "Service:";
      label5.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // HamCheckbox
      // 
      HamCheckbox.AutoSize = true;
      HamCheckbox.Checked = true;
      HamCheckbox.CheckState = CheckState.Checked;
      HamCheckbox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
      HamCheckbox.Location = new Point(56, 3);
      HamCheckbox.Name = "HamCheckbox";
      HamCheckbox.Size = new Size(52, 19);
      HamCheckbox.TabIndex = 7;
      HamCheckbox.Text = "Ham";
      HamCheckbox.UseVisualStyleBackColor = true;
      HamCheckbox.CheckedChanged += FilterChanged;
      // 
      // NonHamCheckbox
      // 
      NonHamCheckbox.AutoSize = true;
      NonHamCheckbox.Checked = true;
      NonHamCheckbox.CheckState = CheckState.Checked;
      NonHamCheckbox.Location = new Point(114, 3);
      NonHamCheckbox.Name = "NonHamCheckbox";
      NonHamCheckbox.Size = new Size(80, 19);
      NonHamCheckbox.TabIndex = 8;
      NonHamCheckbox.Text = "Non-Ham";
      NonHamCheckbox.UseVisualStyleBackColor = true;
      NonHamCheckbox.CheckedChanged += FilterChanged;
      // 
      // flowLayoutPanel3
      // 
      flowLayoutPanel3.BackColor = Color.Gainsboro;
      flowLayoutPanel3.BorderStyle = BorderStyle.FixedSingle;
      flowLayoutPanel3.Controls.Add(label4);
      flowLayoutPanel3.Controls.Add(VhfCheckbox);
      flowLayoutPanel3.Controls.Add(UhfCheckbox);
      flowLayoutPanel3.Controls.Add(OtherBandsCheckbox);
      flowLayoutPanel3.Location = new Point(281, 5);
      flowLayoutPanel3.Name = "flowLayoutPanel3";
      flowLayoutPanel3.Size = new Size(272, 25);
      flowLayoutPanel3.TabIndex = 15;
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Dock = DockStyle.Left;
      label4.Location = new Point(3, 0);
      label4.Name = "label4";
      label4.Size = new Size(42, 25);
      label4.TabIndex = 5;
      label4.Text = "Bands:";
      label4.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // VhfCheckbox
      // 
      VhfCheckbox.AutoSize = true;
      VhfCheckbox.BackColor = Color.Yellow;
      VhfCheckbox.Checked = true;
      VhfCheckbox.CheckState = CheckState.Checked;
      VhfCheckbox.Location = new Point(51, 3);
      VhfCheckbox.Name = "VhfCheckbox";
      VhfCheckbox.Size = new Size(48, 19);
      VhfCheckbox.TabIndex = 6;
      VhfCheckbox.Text = "VHF";
      toolTip1.SetToolTip(VhfCheckbox, "Downlink on 2m Band");
      VhfCheckbox.UseVisualStyleBackColor = false;
      VhfCheckbox.CheckedChanged += FilterChanged;
      // 
      // UhfCheckbox
      // 
      UhfCheckbox.AutoSize = true;
      UhfCheckbox.BackColor = Color.Aquamarine;
      UhfCheckbox.Checked = true;
      UhfCheckbox.CheckState = CheckState.Checked;
      UhfCheckbox.Location = new Point(105, 3);
      UhfCheckbox.Name = "UhfCheckbox";
      UhfCheckbox.Size = new Size(49, 19);
      UhfCheckbox.TabIndex = 7;
      UhfCheckbox.Text = "UHF";
      toolTip1.SetToolTip(UhfCheckbox, "Downlink on 70cm band");
      UhfCheckbox.UseVisualStyleBackColor = false;
      UhfCheckbox.CheckedChanged += FilterChanged;
      // 
      // OtherBandsCheckbox
      // 
      OtherBandsCheckbox.AutoSize = true;
      OtherBandsCheckbox.Checked = true;
      OtherBandsCheckbox.CheckState = CheckState.Checked;
      OtherBandsCheckbox.Location = new Point(160, 3);
      OtherBandsCheckbox.Name = "OtherBandsCheckbox";
      OtherBandsCheckbox.Size = new Size(56, 19);
      OtherBandsCheckbox.TabIndex = 8;
      OtherBandsCheckbox.Text = "Other";
      OtherBandsCheckbox.UseVisualStyleBackColor = true;
      OtherBandsCheckbox.CheckedChanged += FilterChanged;
      // 
      // flowLayoutPanel2
      // 
      flowLayoutPanel2.BackColor = Color.Gainsboro;
      flowLayoutPanel2.BorderStyle = BorderStyle.FixedSingle;
      flowLayoutPanel2.Controls.Add(label6);
      flowLayoutPanel2.Controls.Add(TransponderCheckbox);
      flowLayoutPanel2.Controls.Add(TransceiverCheckbox);
      flowLayoutPanel2.Controls.Add(TransmitterCheckbox);
      flowLayoutPanel2.Location = new Point(7, 38);
      flowLayoutPanel2.Name = "flowLayoutPanel2";
      flowLayoutPanel2.Size = new Size(332, 25);
      flowLayoutPanel2.TabIndex = 14;
      // 
      // label6
      // 
      label6.AutoSize = true;
      label6.Dock = DockStyle.Left;
      label6.Location = new Point(3, 0);
      label6.Name = "label6";
      label6.Size = new Size(40, 25);
      label6.TabIndex = 8;
      label6.Text = "Radio:";
      label6.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // TransponderCheckbox
      // 
      TransponderCheckbox.AutoSize = true;
      TransponderCheckbox.Checked = true;
      TransponderCheckbox.CheckState = CheckState.Checked;
      TransponderCheckbox.Location = new Point(49, 3);
      TransponderCheckbox.Name = "TransponderCheckbox";
      TransponderCheckbox.Size = new Size(91, 19);
      TransponderCheckbox.TabIndex = 5;
      TransponderCheckbox.Text = "Transponder";
      TransponderCheckbox.UseVisualStyleBackColor = true;
      TransponderCheckbox.CheckedChanged += FilterChanged;
      // 
      // TransceiverCheckbox
      // 
      TransceiverCheckbox.AutoSize = true;
      TransceiverCheckbox.Checked = true;
      TransceiverCheckbox.CheckState = CheckState.Checked;
      TransceiverCheckbox.Location = new Point(146, 3);
      TransceiverCheckbox.Name = "TransceiverCheckbox";
      TransceiverCheckbox.Size = new Size(84, 19);
      TransceiverCheckbox.TabIndex = 6;
      TransceiverCheckbox.Text = "Transceiver";
      TransceiverCheckbox.UseVisualStyleBackColor = true;
      TransceiverCheckbox.CheckedChanged += FilterChanged;
      // 
      // TransmitterCheckbox
      // 
      TransmitterCheckbox.AutoSize = true;
      TransmitterCheckbox.Checked = true;
      TransmitterCheckbox.CheckState = CheckState.Checked;
      TransmitterCheckbox.Location = new Point(236, 3);
      TransmitterCheckbox.Name = "TransmitterCheckbox";
      TransmitterCheckbox.Size = new Size(85, 19);
      TransmitterCheckbox.TabIndex = 7;
      TransmitterCheckbox.Text = "Transmitter";
      TransmitterCheckbox.UseVisualStyleBackColor = true;
      TransmitterCheckbox.CheckedChanged += FilterChanged;
      // 
      // flowLayoutPanel1
      // 
      flowLayoutPanel1.BackColor = Color.Gainsboro;
      flowLayoutPanel1.BorderStyle = BorderStyle.FixedSingle;
      flowLayoutPanel1.Controls.Add(label7);
      flowLayoutPanel1.Controls.Add(AliveCheckbox);
      flowLayoutPanel1.Controls.Add(FutureCheckbox);
      flowLayoutPanel1.Controls.Add(ReEnteredCheckbox);
      flowLayoutPanel1.Location = new Point(7, 5);
      flowLayoutPanel1.Name = "flowLayoutPanel1";
      flowLayoutPanel1.Size = new Size(266, 25);
      flowLayoutPanel1.TabIndex = 13;
      // 
      // label7
      // 
      label7.AutoSize = true;
      label7.Dock = DockStyle.Left;
      label7.Location = new Point(3, 0);
      label7.Name = "label7";
      label7.Size = new Size(42, 25);
      label7.TabIndex = 4;
      label7.Text = "Status:";
      label7.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // AliveCheckbox
      // 
      AliveCheckbox.AutoSize = true;
      AliveCheckbox.Checked = true;
      AliveCheckbox.CheckState = CheckState.Checked;
      AliveCheckbox.Location = new Point(51, 3);
      AliveCheckbox.Name = "AliveCheckbox";
      AliveCheckbox.Size = new Size(52, 19);
      AliveCheckbox.TabIndex = 0;
      AliveCheckbox.Text = "Alive";
      AliveCheckbox.UseVisualStyleBackColor = true;
      AliveCheckbox.CheckedChanged += FilterChanged;
      // 
      // FutureCheckbox
      // 
      FutureCheckbox.AutoSize = true;
      FutureCheckbox.Checked = true;
      FutureCheckbox.CheckState = CheckState.Checked;
      FutureCheckbox.Location = new Point(109, 3);
      FutureCheckbox.Name = "FutureCheckbox";
      FutureCheckbox.Size = new Size(60, 19);
      FutureCheckbox.TabIndex = 2;
      FutureCheckbox.Text = "Future";
      FutureCheckbox.UseVisualStyleBackColor = true;
      FutureCheckbox.CheckedChanged += FilterChanged;
      // 
      // ReEnteredCheckbox
      // 
      ReEnteredCheckbox.AutoSize = true;
      ReEnteredCheckbox.Location = new Point(175, 3);
      ReEnteredCheckbox.Name = "ReEnteredCheckbox";
      ReEnteredCheckbox.Size = new Size(84, 19);
      ReEnteredCheckbox.TabIndex = 3;
      ReEnteredCheckbox.Text = "Re-Entered";
      ReEnteredCheckbox.UseVisualStyleBackColor = true;
      ReEnteredCheckbox.CheckedChanged += FilterChanged;
      // 
      // label1
      // 
      label1.Dock = DockStyle.Top;
      label1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
      label1.Location = new Point(0, 0);
      label1.Name = "label1";
      label1.Size = new Size(564, 21);
      label1.TabIndex = 2;
      label1.Text = " Satellites";
      // 
      // SatelliteGroupsForm
      // 
      AcceptButton = OkBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = CancelBtn;
      ClientSize = new Size(816, 822);
      Controls.Add(panel2);
      Controls.Add(panel1);
      Controls.Add(panel5);
      KeyPreview = true;
      MaximizeBox = false;
      MinimizeBox = false;
      Name = "SatelliteGroupsForm";
      ShowIcon = false;
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterParent;
      Text = "Satellites and Groups";
      FormClosing += SatelliteGroupsForm_FormClosing;
      panel1.ResumeLayout(false);
      panel8.ResumeLayout(false);
      panel2.ResumeLayout(false);
      GroupsTreePopupMenu.ResumeLayout(false);
      panel3.ResumeLayout(false);
      SatelliteListPopupMenu.ResumeLayout(false);
      panel5.ResumeLayout(false);
      panel7.ResumeLayout(false);
      panel7.PerformLayout();
      panel6.ResumeLayout(false);
      flowLayoutPanel5.ResumeLayout(false);
      flowLayoutPanel5.PerformLayout();
      flowLayoutPanel4.ResumeLayout(false);
      flowLayoutPanel4.PerformLayout();
      flowLayoutPanel3.ResumeLayout(false);
      flowLayoutPanel3.PerformLayout();
      flowLayoutPanel2.ResumeLayout(false);
      flowLayoutPanel2.PerformLayout();
      flowLayoutPanel1.ResumeLayout(false);
      flowLayoutPanel1.PerformLayout();
      ResumeLayout(false);
    }

    #endregion
    //private SatnogsSatListControl satnogsSatListControl1;
    private Panel panel1;
    private Panel panel2;
    private TreeView treeView1;
    private Panel panel3;
    private Panel panel5;
    private Label label1;
    private Label label2;
    private Panel panel6;
    private FlowLayoutPanel flowLayoutPanel5;
    private Label label3;
    private TextBox FilterTextbox;
    private Button ClearSearchBtn;
    private Label UpdatedDateLabel;
    private FlowLayoutPanel flowLayoutPanel4;
    private Label label5;
    private CheckBox HamCheckbox;
    private CheckBox NonHamCheckbox;
    private FlowLayoutPanel flowLayoutPanel3;
    private Label label4;
    private CheckBox VhfCheckbox;
    private CheckBox UhfCheckbox;
    private CheckBox OtherBandsCheckbox;
    private FlowLayoutPanel flowLayoutPanel2;
    private Label label6;
    private CheckBox TransponderCheckbox;
    private CheckBox TransceiverCheckbox;
    private CheckBox TransmitterCheckbox;
    private FlowLayoutPanel flowLayoutPanel1;
    private Label label7;
    private CheckBox AliveCheckbox;
    private CheckBox FutureCheckbox;
    private CheckBox ReEnteredCheckbox;
    private Panel panel7;
    private Label CountLabel;
    public ListView listView1;
    private ColumnHeader columnHeader1;
    private ColumnHeader columnHeader2;
    private ColumnHeader columnHeader3;
    private ColumnHeader columnHeader4;
    private ContextMenuStrip SatelliteListPopupMenu;
    private ToolStripMenuItem RenameSatMNU;
    private ToolStripMenuItem DetailsMNU;
    private Button CancelBtn;
    private Button OkBtn;
    private TreeView treeView2;
    private Panel panel8;
    private Button AddGroupBtn;
    private Button DeleteSatBtn;
    private Button AddSatBtn;
    private ToolTip toolTip1;
    private ContextMenuStrip GroupsTreePopupMenu;
    private ToolStripMenuItem RenameMNU2;
    private ToolStripMenuItem DetailsMNU2;
    private ToolStripMenuItem DeleteMNU2;
    private ToolStripMenuItem ClearGroupMNU;
    private ToolStripMenuItem AddToGroupMNU;
    private ToolStripMenuItem toolStripMenuItem1;
  }
}
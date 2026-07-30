namespace SkyRoof
{
  partial class AutoSelectionConfigForm
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
      RotationTree = new TreeView();
      MoveUpBtn = new Button();
      MoveDownBtn = new Button();
      SatGroupBox = new GroupBox();
      TxLabel = new Label();
      TransmitterCombo = new ComboBox();
      RecLabel = new Label();
      RecordCombo = new ComboBox();
      ModeGroupBox = new GroupBox();
      FinishCurrentRadio = new RadioButton();
      HighestElevationRadio = new RadioButton();
      PriorityRadio = new RadioButton();
      TrackAntennaCheckbox = new CheckBox();
      OkBtn = new Button();
      CancelBtn = new Button();
      ClearBtn = new Button();
      SelectAllBtn = new Button();
      DropdownBtn = new Button();
      SelectAllPopupMenu = new ContextMenuStrip(components);
      SelectAllMnu = new ToolStripMenuItem();
      toolStripSeparator1 = new ToolStripSeparator();
      Select5Mnu = new ToolStripMenuItem();
      Select10Mnu = new ToolStripMenuItem();
      Select15Mnu = new ToolStripMenuItem();
      Select20Mnu = new ToolStripMenuItem();
      SatGroupBox.SuspendLayout();
      ModeGroupBox.SuspendLayout();
      SelectAllPopupMenu.SuspendLayout();
      SuspendLayout();
      // 
      // RotationTree
      // 
      RotationTree.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
      RotationTree.HideSelection = false;
      RotationTree.Location = new Point(12, 12);
      RotationTree.Name = "RotationTree";
      RotationTree.Size = new Size(360, 396);
      RotationTree.TabIndex = 0;
      RotationTree.AfterSelect += RotationTree_AfterSelect;
      RotationTree.NodeMouseClick += RotationTree_NodeMouseClick;
      // 
      // MoveUpBtn
      // 
      MoveUpBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      MoveUpBtn.Location = new Point(388, 12);
      MoveUpBtn.Name = "MoveUpBtn";
      MoveUpBtn.Size = new Size(90, 26);
      MoveUpBtn.TabIndex = 1;
      MoveUpBtn.Text = "Move Up";
      MoveUpBtn.UseVisualStyleBackColor = true;
      MoveUpBtn.Click += MoveUpBtn_Click;
      // 
      // MoveDownBtn
      // 
      MoveDownBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      MoveDownBtn.Location = new Point(388, 44);
      MoveDownBtn.Name = "MoveDownBtn";
      MoveDownBtn.Size = new Size(90, 26);
      MoveDownBtn.TabIndex = 2;
      MoveDownBtn.Text = "Move Down";
      MoveDownBtn.UseVisualStyleBackColor = true;
      MoveDownBtn.Click += MoveDownBtn_Click;
      // 
      // SatGroupBox
      // 
      SatGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      SatGroupBox.Controls.Add(TxLabel);
      SatGroupBox.Controls.Add(TransmitterCombo);
      SatGroupBox.Location = new Point(388, 84);
      SatGroupBox.Name = "SatGroupBox";
      SatGroupBox.Size = new Size(200, 76);
      SatGroupBox.TabIndex = 3;
      SatGroupBox.TabStop = false;
      SatGroupBox.Text = "Satellite";
      // 
      // TxLabel
      // 
      TxLabel.AutoSize = true;
      TxLabel.Location = new Point(10, 24);
      TxLabel.Name = "TxLabel";
      TxLabel.Size = new Size(70, 15);
      TxLabel.TabIndex = 0;
      TxLabel.Text = "Transmitter:";
      // 
      // TransmitterCombo
      // 
      TransmitterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
      TransmitterCombo.Location = new Point(10, 42);
      TransmitterCombo.Name = "TransmitterCombo";
      TransmitterCombo.Size = new Size(180, 23);
      TransmitterCombo.TabIndex = 0;
      TransmitterCombo.SelectedIndexChanged += TransmitterCombo_SelectedIndexChanged;
      //
      // ModeGroupBox
      //
      ModeGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      ModeGroupBox.Controls.Add(FinishCurrentRadio);
      ModeGroupBox.Controls.Add(HighestElevationRadio);
      ModeGroupBox.Controls.Add(PriorityRadio);
      ModeGroupBox.Location = new Point(388, 172);
      ModeGroupBox.Name = "ModeGroupBox";
      ModeGroupBox.Size = new Size(200, 108);
      ModeGroupBox.TabIndex = 4;
      ModeGroupBox.TabStop = false;
      ModeGroupBox.Text = "Overlapping Passes";
      // 
      // FinishCurrentRadio
      // 
      FinishCurrentRadio.AutoSize = true;
      FinishCurrentRadio.Location = new Point(10, 24);
      FinishCurrentRadio.Name = "FinishCurrentRadio";
      FinishCurrentRadio.Size = new Size(97, 19);
      FinishCurrentRadio.TabIndex = 0;
      FinishCurrentRadio.TabStop = true;
      FinishCurrentRadio.Text = "Finish current";
      FinishCurrentRadio.UseVisualStyleBackColor = true;
      // 
      // HighestElevationRadio
      // 
      HighestElevationRadio.AutoSize = true;
      HighestElevationRadio.Location = new Point(10, 50);
      HighestElevationRadio.Name = "HighestElevationRadio";
      HighestElevationRadio.Size = new Size(117, 19);
      HighestElevationRadio.TabIndex = 1;
      HighestElevationRadio.TabStop = true;
      HighestElevationRadio.Text = "Highest elevation";
      HighestElevationRadio.UseVisualStyleBackColor = true;
      // 
      // PriorityRadio
      // 
      PriorityRadio.AutoSize = true;
      PriorityRadio.Location = new Point(10, 76);
      PriorityRadio.Name = "PriorityRadio";
      PriorityRadio.Size = new Size(63, 19);
      PriorityRadio.TabIndex = 2;
      PriorityRadio.TabStop = true;
      PriorityRadio.Text = "Priority";
      PriorityRadio.UseVisualStyleBackColor = true;
      //
      // RecLabel
      //
      RecLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      RecLabel.AutoSize = true;
      RecLabel.Location = new Point(399, 292);
      RecLabel.Name = "RecLabel";
      RecLabel.Size = new Size(115, 15);
      RecLabel.TabIndex = 5;
      RecLabel.Text = "Record selected passes:";
      //
      // RecordCombo
      //
      RecordCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      RecordCombo.DropDownStyle = ComboBoxStyle.DropDownList;
      RecordCombo.Location = new Point(398, 310);
      RecordCombo.Name = "RecordCombo";
      RecordCombo.Size = new Size(180, 23);
      RecordCombo.TabIndex = 6;
      //
      // TrackAntennaCheckbox
      //
      TrackAntennaCheckbox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      TrackAntennaCheckbox.AutoSize = true;
      TrackAntennaCheckbox.Location = new Point(399, 345);
      TrackAntennaCheckbox.Name = "TrackAntennaCheckbox";
      TrackAntennaCheckbox.Size = new Size(175, 19);
      TrackAntennaCheckbox.TabIndex = 7;
      TrackAntennaCheckbox.Text = "Track antenna during passes";
      TrackAntennaCheckbox.UseVisualStyleBackColor = true;
      // 
      // OkBtn
      // 
      OkBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      OkBtn.Location = new Point(388, 412);
      OkBtn.Name = "OkBtn";
      OkBtn.Size = new Size(90, 26);
      OkBtn.TabIndex = 8;
      OkBtn.Text = "OK";
      OkBtn.UseVisualStyleBackColor = true;
      OkBtn.Click += OkBtn_Click;
      // 
      // CancelBtn
      // 
      CancelBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      CancelBtn.DialogResult = DialogResult.Cancel;
      CancelBtn.Location = new Point(498, 412);
      CancelBtn.Name = "CancelBtn";
      CancelBtn.Size = new Size(90, 26);
      CancelBtn.TabIndex = 9;
      CancelBtn.Text = "Cancel";
      CancelBtn.UseVisualStyleBackColor = true;
      // 
      // ClearBtn
      // 
      ClearBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      ClearBtn.Location = new Point(498, 372);
      ClearBtn.Name = "ClearBtn";
      ClearBtn.Size = new Size(90, 26);
      ClearBtn.TabIndex = 11;
      ClearBtn.Text = "Clear";
      ClearBtn.UseVisualStyleBackColor = true;
      ClearBtn.Click += ClearBtn_Click;
      // 
      // SelectAllBtn
      // 
      SelectAllBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      SelectAllBtn.Location = new Point(388, 372);
      SelectAllBtn.Name = "SelectAllBtn";
      SelectAllBtn.Size = new Size(78, 26);
      SelectAllBtn.TabIndex = 10;
      SelectAllBtn.Text = "Select All";
      SelectAllBtn.UseVisualStyleBackColor = true;
      SelectAllBtn.Click += SelectAllBtn_Click;
      // 
      // DropdownBtn
      // 
      DropdownBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      DropdownBtn.Font = new Font("Wingdings 3", 8.25F);
      DropdownBtn.Location = new Point(462, 372);
      DropdownBtn.Name = "DropdownBtn";
      DropdownBtn.Size = new Size(16, 26);
      DropdownBtn.TabIndex = 12;
      DropdownBtn.Text = "q";
      DropdownBtn.UseVisualStyleBackColor = true;
      DropdownBtn.MouseDown += DropdownBtn_MouseDown;
      // 
      // SelectAllPopupMenu
      // 
      SelectAllPopupMenu.Items.AddRange(new ToolStripItem[] { SelectAllMnu, toolStripSeparator1, Select5Mnu, Select10Mnu, Select15Mnu, Select20Mnu });
      SelectAllPopupMenu.Name = "ClearAllMNU";
      SelectAllPopupMenu.Size = new Size(137, 120);
      // 
      // SelectAllMnu
      // 
      SelectAllMnu.Name = "SelectAllMnu";
      SelectAllMnu.Size = new Size(136, 22);
      SelectAllMnu.Tag = "0";
      SelectAllMnu.Text = "Select All";
      SelectAllMnu.Click += SelectAllBtn_Click;
      // 
      // toolStripSeparator1
      // 
      toolStripSeparator1.Name = "toolStripSeparator1";
      toolStripSeparator1.Size = new Size(133, 6);
      // 
      // Select5Mnu
      // 
      Select5Mnu.Name = "Select5Mnu";
      Select5Mnu.Size = new Size(136, 22);
      Select5Mnu.Tag = "5";
      Select5Mnu.Text = "Select > 5º";
      Select5Mnu.Click += SelectAboveMnu_Click;
      // 
      // Select10Mnu
      // 
      Select10Mnu.Name = "Select10Mnu";
      Select10Mnu.Size = new Size(136, 22);
      Select10Mnu.Tag = "10";
      Select10Mnu.Text = "Select > 10º";
      Select10Mnu.Click += SelectAboveMnu_Click;
      // 
      // Select15Mnu
      // 
      Select15Mnu.Name = "Select15Mnu";
      Select15Mnu.Size = new Size(136, 22);
      Select15Mnu.Tag = "15";
      Select15Mnu.Text = "Select > 15º";
      Select15Mnu.Click += SelectAboveMnu_Click;
      // 
      // Select20Mnu
      // 
      Select20Mnu.Name = "Select20Mnu";
      Select20Mnu.Size = new Size(136, 22);
      Select20Mnu.Tag = "20";
      Select20Mnu.Text = "Select > 20º";
      Select20Mnu.Click += SelectAboveMnu_Click;
      // 
      // AutoSelectionConfigForm
      // 
      AcceptButton = OkBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = CancelBtn;
      ClientSize = new Size(600, 450);
      Controls.Add(DropdownBtn);
      Controls.Add(RotationTree);
      Controls.Add(MoveUpBtn);
      Controls.Add(MoveDownBtn);
      Controls.Add(SatGroupBox);
      Controls.Add(ModeGroupBox);
      Controls.Add(RecLabel);
      Controls.Add(RecordCombo);
      Controls.Add(TrackAntennaCheckbox);
      Controls.Add(OkBtn);
      Controls.Add(CancelBtn);
      Controls.Add(ClearBtn);
      Controls.Add(SelectAllBtn);
      Font = new Font("Segoe UI", 9F);
      MinimizeBox = false;
      Name = "AutoSelectionConfigForm";
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterParent;
      Text = "Auto Selection Schedule";
      SatGroupBox.ResumeLayout(false);
      SatGroupBox.PerformLayout();
      ModeGroupBox.ResumeLayout(false);
      ModeGroupBox.PerformLayout();
      SelectAllPopupMenu.ResumeLayout(false);
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private TreeView RotationTree;
    private Button MoveUpBtn;
    private Button MoveDownBtn;
    private GroupBox SatGroupBox;
    private Label TxLabel;
    private ComboBox TransmitterCombo;
    private Label RecLabel;
    private ComboBox RecordCombo;
    private GroupBox ModeGroupBox;
    private RadioButton FinishCurrentRadio;
    private RadioButton HighestElevationRadio;
    private RadioButton PriorityRadio;
    private CheckBox TrackAntennaCheckbox;
    private Button OkBtn;
    private Button CancelBtn;
    private Button ClearBtn;
    private Button SelectAllBtn;
    private Button button1;
    private Button DropdownBtn;
    private ContextMenuStrip MenuStrip;
    private ToolStripMenuItem ClearAllMNU;
    private ContextMenuStrip SelectAllPopupMenu;
    private ToolStripMenuItem SelectAllMnu;
    private ToolStripMenuItem SelectAllMNU;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripMenuItem Select5MNU;
    private ToolStripMenuItem Select10MNU;
    private ToolStripMenuItem Select15MNU;
    private ToolStripMenuItem Select20MNU;
    private ToolStripMenuItem Select5Mnu;
    private ToolStripMenuItem Select10Mnu;
    private ToolStripMenuItem Select15Mnu;
    private ToolStripMenuItem Select20Mnu;
  }
}

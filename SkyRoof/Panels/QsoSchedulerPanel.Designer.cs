namespace SkyRoof
{
  partial class QsoSchedulerPanel
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
      dockPanel1 = new WeifenLuo.WinFormsUI.Docking.DockPanel();
      label1 = new Label();
      label2 = new Label();
      SatelliteComboBox = new ComboBox();
      DxSquareEdit = new TextBox();
      PredictionList = new VE3NEA.ListViewEx();
      columnHeader1 = new ColumnHeader();
      SuspendLayout();
      // 
      // dockPanel1
      // 
      dockPanel1.Dock = DockStyle.Top;
      dockPanel1.Location = new Point(0, 0);
      dockPanel1.Name = "dockPanel1";
      dockPanel1.Size = new Size(600, 36);
      dockPanel1.TabIndex = 0;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(8, 8);
      label1.Name = "label1";
      label1.Size = new Size(48, 15);
      label1.TabIndex = 1;
      label1.Text = "Satellite";
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(211, 9);
      label2.Name = "label2";
      label2.Size = new Size(86, 15);
      label2.TabIndex = 2;
      label2.Text = "DX Grid Square";
      // 
      // SatelliteComboBox
      // 
      SatelliteComboBox.DisplayMember = "name";
      SatelliteComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      SatelliteComboBox.FormattingEnabled = true;
      SatelliteComboBox.Location = new Point(62, 5);
      SatelliteComboBox.Name = "SatelliteComboBox";
      SatelliteComboBox.Size = new Size(134, 23);
      SatelliteComboBox.TabIndex = 12;
      SatelliteComboBox.SelectedIndexChanged += SatelliteComboBox_SelectedIndexChanged;
      // 
      // DxSquareEdit
      // 
      DxSquareEdit.CharacterCasing = CharacterCasing.Upper;
      DxSquareEdit.Location = new Point(303, 6);
      DxSquareEdit.Name = "DxSquareEdit";
      DxSquareEdit.Size = new Size(68, 23);
      DxSquareEdit.TabIndex = 13;
      DxSquareEdit.TextChanged += DxSquareEdit_TextChanged;
      // 
      // PredictionList
      // 
      PredictionList.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
      PredictionList.Dock = DockStyle.Fill;
      PredictionList.FullRowSelect = true;
      PredictionList.HeaderStyle = ColumnHeaderStyle.None;
      PredictionList.Location = new Point(0, 36);
      PredictionList.MultiSelect = false;
      PredictionList.Name = "PredictionList";
      PredictionList.OwnerDraw = true;
      PredictionList.ShowItemToolTips = true;
      PredictionList.Size = new Size(600, 364);
      PredictionList.TabIndex = 14;
      PredictionList.UseCompatibleStateImageBehavior = false;
      PredictionList.View = View.Details;
      PredictionList.VirtualMode = true;
      PredictionList.DrawSubItem += PredictionList_DrawSubItem;
      PredictionList.RetrieveVirtualItem += PredictionList_RetrieveVirtualItem;
      PredictionList.Resize += PredictionList_Resize;
      PredictionList.MouseDown += PredictionList_MouseDown;
      // 
      // QsoSchedulerPanel
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(600, 400);
      Controls.Add(PredictionList);
      Controls.Add(DxSquareEdit);
      Controls.Add(SatelliteComboBox);
      Controls.Add(label2);
      Controls.Add(label1);
      Controls.Add(dockPanel1);
      Name = "QsoSchedulerPanel";
      Text = "QSO Scheduler";
      FormClosing += QsoScheduler_FormClosing;
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private WeifenLuo.WinFormsUI.Docking.DockPanel dockPanel1;
    private Label label1;
    private Label label2;
    private ComboBox SatelliteComboBox;
    private TextBox DxSquareEdit;
    private VE3NEA.ListViewEx PredictionList;
    private ColumnHeader columnHeader1;
  }
}

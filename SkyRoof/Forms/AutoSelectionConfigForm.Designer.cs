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
      OkBtn = new Button();
      CancelBtn = new Button();
      ClearBtn = new Button();
      SelectAllBtn = new Button();
      SatGroupBox.SuspendLayout();
      ModeGroupBox.SuspendLayout();
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
      SatGroupBox.Controls.Add(RecLabel);
      SatGroupBox.Controls.Add(RecordCombo);
      SatGroupBox.Location = new Point(388, 84);
      SatGroupBox.Name = "SatGroupBox";
      SatGroupBox.Size = new Size(200, 124);
      SatGroupBox.TabIndex = 3;
      SatGroupBox.TabStop = false;
      SatGroupBox.Text = "Satellite";
      //
      // TxLabel
      //
      TxLabel.AutoSize = true;
      TxLabel.Location = new Point(10, 24);
      TxLabel.Name = "TxLabel";
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
      // RecLabel
      //
      RecLabel.AutoSize = true;
      RecLabel.Location = new Point(10, 74);
      RecLabel.Name = "RecLabel";
      RecLabel.Text = "Record:";
      //
      // RecordCombo
      //
      RecordCombo.DropDownStyle = ComboBoxStyle.DropDownList;
      RecordCombo.Location = new Point(10, 92);
      RecordCombo.Name = "RecordCombo";
      RecordCombo.Size = new Size(180, 23);
      RecordCombo.TabIndex = 1;
      RecordCombo.SelectedIndexChanged += RecordCombo_SelectedIndexChanged;
      //
      // ModeGroupBox
      //
      ModeGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      ModeGroupBox.Controls.Add(FinishCurrentRadio);
      ModeGroupBox.Controls.Add(HighestElevationRadio);
      ModeGroupBox.Controls.Add(PriorityRadio);
      ModeGroupBox.Location = new Point(388, 220);
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
      PriorityRadio.TabIndex = 2;
      PriorityRadio.TabStop = true;
      PriorityRadio.Text = "Priority";
      PriorityRadio.UseVisualStyleBackColor = true;
      //
      // OkBtn
      //
      OkBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      OkBtn.Location = new Point(388, 382);
      OkBtn.Name = "OkBtn";
      OkBtn.Size = new Size(90, 26);
      OkBtn.TabIndex = 5;
      OkBtn.Text = "OK";
      OkBtn.UseVisualStyleBackColor = true;
      OkBtn.Click += OkBtn_Click;
      //
      // CancelBtn
      //
      CancelBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      CancelBtn.DialogResult = DialogResult.Cancel;
      CancelBtn.Location = new Point(498, 382);
      CancelBtn.Name = "CancelBtn";
      CancelBtn.Size = new Size(90, 26);
      CancelBtn.TabIndex = 6;
      CancelBtn.Text = "Cancel";
      CancelBtn.UseVisualStyleBackColor = true;
      //
      // SelectAllBtn
      //
      SelectAllBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      SelectAllBtn.Location = new Point(388, 344);
      SelectAllBtn.Name = "SelectAllBtn";
      SelectAllBtn.Size = new Size(90, 26);
      SelectAllBtn.TabIndex = 7;
      SelectAllBtn.Text = "Select All";
      SelectAllBtn.UseVisualStyleBackColor = true;
      SelectAllBtn.Click += SelectAllBtn_Click;
      //
      // ClearBtn
      //
      ClearBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      ClearBtn.Location = new Point(498, 344);
      ClearBtn.Name = "ClearBtn";
      ClearBtn.Size = new Size(90, 26);
      ClearBtn.TabIndex = 8;
      ClearBtn.Text = "Clear";
      ClearBtn.UseVisualStyleBackColor = true;
      ClearBtn.Click += ClearBtn_Click;
      //
      // AutoSelectionConfigForm
      //
      AcceptButton = OkBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = CancelBtn;
      ClientSize = new Size(600, 420);
      Controls.Add(RotationTree);
      Controls.Add(MoveUpBtn);
      Controls.Add(MoveDownBtn);
      Controls.Add(SatGroupBox);
      Controls.Add(ModeGroupBox);
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
      ResumeLayout(false);
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
    private Button OkBtn;
    private Button CancelBtn;
    private Button ClearBtn;
    private Button SelectAllBtn;
  }
}

namespace SkyRoof
{
  partial class QsoEntryPanel
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
      flowLayoutPanel1 = new FlowLayoutPanel();
      panel1 = new Panel();
      UtcFrame = new Panel();
      UtcPicker = new DateTimePicker();
      UtcLabel = new Label();
      panel2 = new Panel();
      BandFrame = new Panel();
      BandComboBox = new ComboBox();
      label10 = new Label();
      panel3 = new Panel();
      ModeFrame = new Panel();
      ModeComboBox = new ComboBox();
      label11 = new Label();
      panel4 = new Panel();
      SatFrame = new Panel();
      SatComboBox = new ComboBox();
      label12 = new Label();
      panel5 = new Panel();
      CallFrame = new Panel();
      CallEdit = new TextBox();
      label13 = new Label();
      panel6 = new Panel();
      GridFrame = new Panel();
      GridEdit = new TextBox();
      label14 = new Label();
      panel11 = new Panel();
      StateFrame = new Panel();
      StateComboBox = new ComboBox();
      label1 = new Label();
      panel7 = new Panel();
      SentFrame = new Panel();
      SentEdit = new TextBox();
      label15 = new Label();
      panel8 = new Panel();
      RecvFrame = new Panel();
      RecvEdit = new TextBox();
      label16 = new Label();
      panel9 = new Panel();
      NameFrame = new Panel();
      NameEdit = new TextBox();
      label17 = new Label();
      panel12 = new Panel();
      NotesFrame = new Panel();
      NotesEdit = new TextBox();
      label2 = new Label();
      ButtonsPanel = new Panel();
      ClearBtn = new Button();
      SaveBtn = new Button();
      toolTip1 = new ToolTip(components);
      flowLayoutPanel1.SuspendLayout();
      panel1.SuspendLayout();
      UtcFrame.SuspendLayout();
      panel2.SuspendLayout();
      BandFrame.SuspendLayout();
      panel3.SuspendLayout();
      ModeFrame.SuspendLayout();
      panel4.SuspendLayout();
      SatFrame.SuspendLayout();
      panel5.SuspendLayout();
      CallFrame.SuspendLayout();
      panel6.SuspendLayout();
      GridFrame.SuspendLayout();
      panel11.SuspendLayout();
      StateFrame.SuspendLayout();
      panel7.SuspendLayout();
      SentFrame.SuspendLayout();
      panel8.SuspendLayout();
      RecvFrame.SuspendLayout();
      panel9.SuspendLayout();
      NameFrame.SuspendLayout();
      panel12.SuspendLayout();
      NotesFrame.SuspendLayout();
      ButtonsPanel.SuspendLayout();
      SuspendLayout();
      // 
      // flowLayoutPanel1
      // 
      flowLayoutPanel1.AutoSize = true;
      flowLayoutPanel1.Controls.Add(panel1);
      flowLayoutPanel1.Controls.Add(panel2);
      flowLayoutPanel1.Controls.Add(panel3);
      flowLayoutPanel1.Controls.Add(panel4);
      flowLayoutPanel1.Controls.Add(panel5);
      flowLayoutPanel1.Controls.Add(panel6);
      flowLayoutPanel1.Controls.Add(panel11);
      flowLayoutPanel1.Controls.Add(panel7);
      flowLayoutPanel1.Controls.Add(panel8);
      flowLayoutPanel1.Controls.Add(panel9);
      flowLayoutPanel1.Controls.Add(panel12);
      flowLayoutPanel1.Controls.Add(ButtonsPanel);
      flowLayoutPanel1.Dock = DockStyle.Fill;
      flowLayoutPanel1.Location = new Point(0, 0);
      flowLayoutPanel1.Name = "flowLayoutPanel1";
      flowLayoutPanel1.Size = new Size(734, 128);
      flowLayoutPanel1.TabIndex = 0;
      // 
      // panel1
      // 
      panel1.BackColor = Theme.QsoCard;
      panel1.Controls.Add(UtcFrame);
      panel1.Controls.Add(UtcLabel);
      panel1.Location = new Point(3, 3);
      panel1.Name = "panel1";
      panel1.Size = new Size(177, 36);
      panel1.TabIndex = 0;
      // 
      // UtcFrame
      // 
      UtcFrame.BackColor = Theme.QsoFieldEdited;
      UtcFrame.Controls.Add(UtcPicker);
      UtcFrame.Location = new Point(43, 6);
      UtcFrame.Name = "UtcFrame";
      UtcFrame.Size = new Size(132, 27);
      UtcFrame.TabIndex = 24;
      // 
      // UtcPicker
      // 
      UtcPicker.CustomFormat = "MM/dd HH:mm";
      UtcPicker.Font = new Font("Courier New", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
      UtcPicker.Format = DateTimePickerFormat.Custom;
      UtcPicker.Location = new Point(2, 2);
      UtcPicker.Name = "UtcPicker";
      UtcPicker.Size = new Size(128, 23);
      UtcPicker.TabIndex = 1;
      UtcPicker.ValueChanged += Field_Changed;
      UtcPicker.KeyDown += UtcPicker_KeyDown;
      // 
      // UtcLabel
      // 
      UtcLabel.AutoSize = true;
      UtcLabel.Location = new Point(3, 11);
      UtcLabel.Name = "UtcLabel";
      UtcLabel.Size = new Size(28, 15);
      UtcLabel.TabIndex = 21;
      UtcLabel.Text = "UTC";
      UtcLabel.TextAlign = ContentAlignment.MiddleLeft;
      UtcLabel.MouseClick += Utclabel_MouseClick;
      // 
      // panel2
      // 
      panel2.BackColor = Theme.QsoCard;
      panel2.Controls.Add(BandFrame);
      panel2.Controls.Add(label10);
      panel2.Location = new Point(186, 3);
      panel2.Name = "panel2";
      panel2.Size = new Size(177, 36);
      panel2.TabIndex = 1;
      // 
      // BandFrame
      // 
      BandFrame.BackColor = Theme.QsoFieldEdited;
      BandFrame.Controls.Add(BandComboBox);
      BandFrame.Location = new Point(43, 4);
      BandFrame.Name = "BandFrame";
      BandFrame.Size = new Size(132, 30);
      BandFrame.TabIndex = 24;
      // 
      // BandComboBox
      // 
      BandComboBox.AutoCompleteCustomSource.AddRange(new string[] { "2m", "70cm", "23cm", "13cm" });
      BandComboBox.Font = new Font("Courier New", 12F);
      BandComboBox.FormattingEnabled = true;
      BandComboBox.Location = new Point(2, 2);
      BandComboBox.Name = "BandComboBox";
      BandComboBox.Size = new Size(128, 26);
      BandComboBox.TabIndex = 23;
      BandComboBox.TextChanged += Field_Changed;
      // 
      // label10
      // 
      label10.AutoSize = true;
      label10.Location = new Point(3, 11);
      label10.Name = "label10";
      label10.Size = new Size(39, 15);
      label10.TabIndex = 21;
      label10.Text = "BAND";
      label10.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel3
      // 
      panel3.BackColor = Theme.QsoCard;
      panel3.Controls.Add(ModeFrame);
      panel3.Controls.Add(label11);
      panel3.Location = new Point(369, 3);
      panel3.Name = "panel3";
      panel3.Size = new Size(177, 36);
      panel3.TabIndex = 2;
      // 
      // ModeFrame
      // 
      ModeFrame.BackColor = Theme.QsoFieldEdited;
      ModeFrame.Controls.Add(ModeComboBox);
      ModeFrame.Location = new Point(43, 4);
      ModeFrame.Name = "ModeFrame";
      ModeFrame.Size = new Size(132, 30);
      ModeFrame.TabIndex = 24;
      // 
      // ModeComboBox
      // 
      ModeComboBox.Font = new Font("Courier New", 12F);
      ModeComboBox.FormattingEnabled = true;
      ModeComboBox.Location = new Point(2, 2);
      ModeComboBox.Name = "ModeComboBox";
      ModeComboBox.Size = new Size(128, 26);
      ModeComboBox.TabIndex = 23;
      ModeComboBox.TextChanged += Field_Changed;
      // 
      // label11
      // 
      label11.AutoSize = true;
      label11.Location = new Point(3, 11);
      label11.Name = "label11";
      label11.Size = new Size(41, 15);
      label11.TabIndex = 21;
      label11.Text = "MODE";
      label11.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel4
      // 
      panel4.BackColor = Theme.QsoCard;
      panel4.Controls.Add(SatFrame);
      panel4.Controls.Add(label12);
      panel4.Location = new Point(552, 3);
      panel4.Name = "panel4";
      panel4.Size = new Size(177, 36);
      panel4.TabIndex = 3;
      // 
      // SatFrame
      // 
      SatFrame.BackColor = Theme.QsoFieldEdited;
      SatFrame.Controls.Add(SatComboBox);
      SatFrame.Location = new Point(43, 4);
      SatFrame.Name = "SatFrame";
      SatFrame.Size = new Size(132, 30);
      SatFrame.TabIndex = 24;
      // 
      // SatComboBox
      // 
      SatComboBox.Font = new Font("Courier New", 12F);
      SatComboBox.FormattingEnabled = true;
      SatComboBox.Location = new Point(2, 2);
      SatComboBox.Name = "SatComboBox";
      SatComboBox.Size = new Size(128, 26);
      SatComboBox.TabIndex = 23;
      SatComboBox.TextChanged += Field_Changed;
      // 
      // label12
      // 
      label12.AutoSize = true;
      label12.Location = new Point(3, 11);
      label12.Name = "label12";
      label12.Size = new Size(26, 15);
      label12.TabIndex = 21;
      label12.Text = "SAT";
      label12.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel5
      // 
      panel5.BackColor = Theme.QsoCard;
      panel5.Controls.Add(CallFrame);
      panel5.Controls.Add(label13);
      panel5.Location = new Point(3, 45);
      panel5.Name = "panel5";
      panel5.Size = new Size(177, 36);
      panel5.TabIndex = 4;
      // 
      // CallFrame
      // 
      CallFrame.BackColor = Theme.QsoFieldEdited;
      CallFrame.Controls.Add(CallEdit);
      CallFrame.Location = new Point(43, 4);
      CallFrame.Name = "CallFrame";
      CallFrame.Size = new Size(132, 30);
      CallFrame.TabIndex = 25;
      // 
      // CallEdit
      // 
      CallEdit.CharacterCasing = CharacterCasing.Upper;
      CallEdit.Font = new Font("Courier New", 12F);
      CallEdit.Location = new Point(2, 2);
      CallEdit.Margin = new Padding(13);
      CallEdit.Name = "CallEdit";
      CallEdit.Size = new Size(128, 26);
      CallEdit.TabIndex = 23;
      CallEdit.TextChanged += Field_Changed;
      // 
      // label13
      // 
      label13.AutoSize = true;
      label13.Location = new Point(3, 11);
      label13.Margin = new Padding(6);
      label13.Name = "label13";
      label13.Size = new Size(35, 15);
      label13.TabIndex = 21;
      label13.Text = "CALL";
      label13.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel6
      // 
      panel6.BackColor = Theme.QsoCard;
      panel6.Controls.Add(GridFrame);
      panel6.Controls.Add(label14);
      panel6.Location = new Point(186, 45);
      panel6.Name = "panel6";
      panel6.Size = new Size(177, 36);
      panel6.TabIndex = 5;
      // 
      // GridFrame
      // 
      GridFrame.BackColor = Theme.QsoFieldEdited;
      GridFrame.Controls.Add(GridEdit);
      GridFrame.Location = new Point(43, 4);
      GridFrame.Name = "GridFrame";
      GridFrame.Size = new Size(132, 30);
      GridFrame.TabIndex = 25;
      // 
      // GridEdit
      // 
      GridEdit.CharacterCasing = CharacterCasing.Upper;
      GridEdit.Font = new Font("Courier New", 12F);
      GridEdit.Location = new Point(2, 2);
      GridEdit.Margin = new Padding(13);
      GridEdit.Name = "GridEdit";
      GridEdit.Size = new Size(128, 26);
      GridEdit.TabIndex = 23;
      GridEdit.TextChanged += Field_Changed;
      // 
      // label14
      // 
      label14.AutoSize = true;
      label14.Location = new Point(3, 11);
      label14.Name = "label14";
      label14.Size = new Size(33, 15);
      label14.TabIndex = 21;
      label14.Text = "GRID";
      label14.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel11
      // 
      panel11.BackColor = Theme.QsoCard;
      panel11.Controls.Add(StateFrame);
      panel11.Controls.Add(label1);
      panel11.Location = new Point(369, 45);
      panel11.Name = "panel11";
      panel11.Size = new Size(177, 36);
      panel11.TabIndex = 6;
      // 
      // StateFrame
      // 
      StateFrame.BackColor = Theme.QsoFieldEdited;
      StateFrame.Controls.Add(StateComboBox);
      StateFrame.Location = new Point(43, 4);
      StateFrame.Name = "StateFrame";
      StateFrame.Size = new Size(132, 30);
      StateFrame.TabIndex = 24;
      // 
      // StateComboBox
      // 
      StateComboBox.Font = new Font("Courier New", 12F);
      StateComboBox.FormattingEnabled = true;
      StateComboBox.Location = new Point(2, 2);
      StateComboBox.Name = "StateComboBox";
      StateComboBox.Size = new Size(128, 26);
      StateComboBox.TabIndex = 23;
      StateComboBox.TextChanged += Field_Changed;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(3, 11);
      label1.Name = "label1";
      label1.Size = new Size(37, 15);
      label1.TabIndex = 21;
      label1.Text = "STATE";
      label1.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel7
      // 
      panel7.BackColor = Theme.QsoCard;
      panel7.Controls.Add(SentFrame);
      panel7.Controls.Add(label15);
      panel7.Location = new Point(552, 45);
      panel7.Name = "panel7";
      panel7.Size = new Size(177, 36);
      panel7.TabIndex = 7;
      // 
      // SentFrame
      // 
      SentFrame.BackColor = Theme.QsoFieldEdited;
      SentFrame.Controls.Add(SentEdit);
      SentFrame.Location = new Point(43, 4);
      SentFrame.Name = "SentFrame";
      SentFrame.Size = new Size(132, 30);
      SentFrame.TabIndex = 24;
      // 
      // SentEdit
      // 
      SentEdit.CharacterCasing = CharacterCasing.Upper;
      SentEdit.Font = new Font("Courier New", 12F);
      SentEdit.Location = new Point(2, 2);
      SentEdit.Margin = new Padding(13);
      SentEdit.Name = "SentEdit";
      SentEdit.Size = new Size(128, 26);
      SentEdit.TabIndex = 23;
      SentEdit.TextChanged += Field_Changed;
      // 
      // label15
      // 
      label15.AutoSize = true;
      label15.Location = new Point(3, 11);
      label15.Name = "label15";
      label15.Size = new Size(34, 15);
      label15.TabIndex = 21;
      label15.Text = "SENT";
      label15.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel8
      // 
      panel8.BackColor = Theme.QsoCard;
      panel8.Controls.Add(RecvFrame);
      panel8.Controls.Add(label16);
      panel8.Location = new Point(3, 87);
      panel8.Name = "panel8";
      panel8.Size = new Size(177, 36);
      panel8.TabIndex = 8;
      // 
      // RecvFrame
      // 
      RecvFrame.BackColor = Theme.QsoFieldEdited;
      RecvFrame.Controls.Add(RecvEdit);
      RecvFrame.Location = new Point(43, 4);
      RecvFrame.Name = "RecvFrame";
      RecvFrame.Size = new Size(132, 30);
      RecvFrame.TabIndex = 25;
      // 
      // RecvEdit
      // 
      RecvEdit.CharacterCasing = CharacterCasing.Upper;
      RecvEdit.Font = new Font("Courier New", 12F);
      RecvEdit.Location = new Point(2, 2);
      RecvEdit.Margin = new Padding(13);
      RecvEdit.Name = "RecvEdit";
      RecvEdit.Size = new Size(128, 26);
      RecvEdit.TabIndex = 23;
      RecvEdit.TextChanged += Field_Changed;
      // 
      // label16
      // 
      label16.AutoSize = true;
      label16.Location = new Point(3, 11);
      label16.Name = "label16";
      label16.Size = new Size(35, 15);
      label16.TabIndex = 21;
      label16.Text = "RECV";
      label16.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel9
      // 
      panel9.BackColor = Theme.QsoCard;
      panel9.Controls.Add(NameFrame);
      panel9.Controls.Add(label17);
      panel9.Location = new Point(186, 87);
      panel9.Name = "panel9";
      panel9.Size = new Size(177, 36);
      panel9.TabIndex = 9;
      // 
      // NameFrame
      // 
      NameFrame.BackColor = Theme.QsoFieldEdited;
      NameFrame.Controls.Add(NameEdit);
      NameFrame.Location = new Point(43, 4);
      NameFrame.Name = "NameFrame";
      NameFrame.Size = new Size(132, 30);
      NameFrame.TabIndex = 25;
      // 
      // NameEdit
      // 
      NameEdit.Font = new Font("Courier New", 12F);
      NameEdit.Location = new Point(2, 2);
      NameEdit.Margin = new Padding(13);
      NameEdit.Name = "NameEdit";
      NameEdit.Size = new Size(128, 26);
      NameEdit.TabIndex = 23;
      NameEdit.TextChanged += Field_Changed;
      // 
      // label17
      // 
      label17.AutoSize = true;
      label17.Location = new Point(3, 11);
      label17.Name = "label17";
      label17.Size = new Size(41, 15);
      label17.TabIndex = 21;
      label17.Text = "NAME";
      label17.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // panel12
      // 
      panel12.BackColor = Theme.QsoCard;
      panel12.Controls.Add(NotesFrame);
      panel12.Controls.Add(label2);
      panel12.Location = new Point(369, 87);
      panel12.Name = "panel12";
      panel12.Size = new Size(177, 36);
      panel12.TabIndex = 10;
      // 
      // NotesFrame
      // 
      NotesFrame.BackColor = Theme.QsoFieldEdited;
      NotesFrame.Controls.Add(NotesEdit);
      NotesFrame.Location = new Point(43, 4);
      NotesFrame.Name = "NotesFrame";
      NotesFrame.Size = new Size(132, 30);
      NotesFrame.TabIndex = 25;
      // 
      // NotesEdit
      // 
      NotesEdit.Font = new Font("Courier New", 12F);
      NotesEdit.Location = new Point(2, 2);
      NotesEdit.Margin = new Padding(13);
      NotesEdit.Name = "NotesEdit";
      NotesEdit.Size = new Size(128, 26);
      NotesEdit.TabIndex = 23;
      NotesEdit.TextChanged += Field_Changed;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(3, 11);
      label2.Name = "label2";
      label2.Size = new Size(42, 15);
      label2.TabIndex = 21;
      label2.Text = "NOTES";
      label2.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // ButtonsPanel
      // 
      ButtonsPanel.BackColor = Theme.QsoCard;
      ButtonsPanel.Controls.Add(ClearBtn);
      ButtonsPanel.Controls.Add(SaveBtn);
      ButtonsPanel.Location = new Point(552, 87);
      ButtonsPanel.Name = "ButtonsPanel";
      ButtonsPanel.Size = new Size(177, 36);
      ButtonsPanel.TabIndex = 11;
      // 
      // ClearBtn
      // 
      ClearBtn.Location = new Point(94, 6);
      ClearBtn.Margin = new Padding(13);
      ClearBtn.Name = "ClearBtn";
      ClearBtn.Size = new Size(72, 23);
      ClearBtn.TabIndex = 24;
      ClearBtn.Text = "Clear";
      ClearBtn.UseVisualStyleBackColor = true;
      ClearBtn.Click += ClearBtn_Click;
      // 
      // SaveBtn
      // 
      SaveBtn.Location = new Point(11, 7);
      SaveBtn.Name = "SaveBtn";
      SaveBtn.Size = new Size(72, 23);
      SaveBtn.TabIndex = 23;
      SaveBtn.Text = "Save";
      SaveBtn.UseVisualStyleBackColor = true;
      SaveBtn.Click += LogBtn_Click;
      // 
      // QsoEntryPanel
      // 
      AcceptButton = SaveBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(734, 128);
      Controls.Add(flowLayoutPanel1);
      Name = "QsoEntryPanel";
      Text = "QSO Entry";
      FormClosing += QsoEntryPanel_FormClosing;
      flowLayoutPanel1.ResumeLayout(false);
      panel1.ResumeLayout(false);
      panel1.PerformLayout();
      UtcFrame.ResumeLayout(false);
      panel2.ResumeLayout(false);
      panel2.PerformLayout();
      BandFrame.ResumeLayout(false);
      panel3.ResumeLayout(false);
      panel3.PerformLayout();
      ModeFrame.ResumeLayout(false);
      panel4.ResumeLayout(false);
      panel4.PerformLayout();
      SatFrame.ResumeLayout(false);
      panel5.ResumeLayout(false);
      panel5.PerformLayout();
      CallFrame.ResumeLayout(false);
      CallFrame.PerformLayout();
      panel6.ResumeLayout(false);
      panel6.PerformLayout();
      GridFrame.ResumeLayout(false);
      GridFrame.PerformLayout();
      panel11.ResumeLayout(false);
      panel11.PerformLayout();
      StateFrame.ResumeLayout(false);
      panel7.ResumeLayout(false);
      panel7.PerformLayout();
      SentFrame.ResumeLayout(false);
      SentFrame.PerformLayout();
      panel8.ResumeLayout(false);
      panel8.PerformLayout();
      RecvFrame.ResumeLayout(false);
      RecvFrame.PerformLayout();
      panel9.ResumeLayout(false);
      panel9.PerformLayout();
      NameFrame.ResumeLayout(false);
      NameFrame.PerformLayout();
      panel12.ResumeLayout(false);
      panel12.PerformLayout();
      NotesFrame.ResumeLayout(false);
      NotesFrame.PerformLayout();
      ButtonsPanel.ResumeLayout(false);
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private FlowLayoutPanel flowLayoutPanel1;
    private Panel panel1;
    private Label UtcLabel;
    private TextBox SentEdit;
    private Panel panel2;
    private Label label10;
    private Panel panel3;
    private Label label11;
    private Panel panel4;
    private Label label12;
    private Panel panel5;
    private Label label13;
    private Panel panel6;
    private Label label14;
    private Panel panel7;
    private Label label15;
    private Panel panel8;
    private Label label16;
    private Panel panel9;
    private Label label17;
    private Panel ButtonsPanel;
    private Button SaveBtn;
    private Button ClearBtn;
    private Panel panel11;
    private Label label1;
    private Panel panel10;
    private Panel BandFrame;
    private ComboBox BandComboBox;
    private Panel ModeFrame;
    private ComboBox ModeComboBox;
    private Panel SatFrame;
    private ComboBox SatComboBox;
    private Panel CallFrame;
    private TextBox RecvEdit;
    private Panel GridFrame;
    private TextBox CallEdit;
    private Panel StateFrame;
    private ComboBox StateComboBox;
    private Panel SentFrame;
    private TextBox GridEdit;
    private Panel RecvFrame;
    private TextBox textBox4;
    private Panel NameFrame;
    private TextBox NameEdit;
    private Panel UtcFrame;
    private DateTimePicker UtcPicker;
    private DateTimePicker dateTimePicker1;
    private ComboBox comboBox1;
    private ToolTip toolTip1;
    private Panel panel12;
    private Panel NotesFrame;
    private TextBox NotesEdit;
    private Label label2;
  }
}
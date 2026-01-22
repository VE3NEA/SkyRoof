namespace SkyRoof
{
  partial class FrequencyWidget
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      components = new System.ComponentModel.Container();
      label7 = new Label();
      RitCheckbox = new CheckBox();
      RitSpinner = new NumericUpDown();
      label5 = new Label();
      label6 = new Label();
      label4 = new Label();
      label3 = new Label();
      UplinkModeCombobox = new ComboBox();
      DownlinkModeCombobox = new ComboBox();
      UplinkDopplerLabel = new Label();
      DownlinkDopplerLabel = new Label();
      label1 = new Label();
      UplinkManualSpinner = new NumericUpDown();
      UplinkManualCheckbox = new CheckBox();
      UplinkDopplerCheckbox = new CheckBox();
      UplinkFrequencyLabel = new Label();
      contextMenuStrip1 = new ContextMenuStrip(components);
      ShowNominalFrequencyMNU = new ToolStripMenuItem();
      ShowCorrectedFrequencyMNU = new ToolStripMenuItem();
      UplinkLabel = new Label();
      label2 = new Label();
      DownlinkManualSpinner = new NumericUpDown();
      DownlinkManualCheckbox = new CheckBox();
      DownlinkDopplerCheckbox = new CheckBox();
      DownlinkFrequencyLabel = new Label();
      DownlinkLabel = new Label();
      TxBtn = new Button();
      toolTip1 = new ToolTip(components);
      ((System.ComponentModel.ISupportInitialize)RitSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)UplinkManualSpinner).BeginInit();
      contextMenuStrip1.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)DownlinkManualSpinner).BeginInit();
      SuspendLayout();
      // 
      // label7
      // 
      label7.AutoSize = true;
      label7.Location = new Point(210, 56);
      label7.Name = "label7";
      label7.Size = new Size(24, 15);
      label7.TabIndex = 48;
      label7.Text = "RIT";
      // 
      // RitCheckbox
      // 
      RitCheckbox.AutoSize = true;
      RitCheckbox.Location = new Point(194, 55);
      RitCheckbox.Name = "RitCheckbox";
      RitCheckbox.Size = new Size(15, 14);
      RitCheckbox.TabIndex = 47;
      RitCheckbox.UseVisualStyleBackColor = true;
      RitCheckbox.CheckedChanged += UiControl_Changed;
      // 
      // RitSpinner
      // 
      RitSpinner.BorderStyle = BorderStyle.None;
      RitSpinner.DecimalPlaces = 3;
      RitSpinner.Font = new Font("Segoe UI", 10F);
      RitSpinner.Increment = new decimal(new int[] { 2, 0, 0, 131072 });
      RitSpinner.Location = new Point(262, 55);
      RitSpinner.Maximum = new decimal(new int[] { 25, 0, 0, 0 });
      RitSpinner.Minimum = new decimal(new int[] { 25, 0, 0, int.MinValue });
      RitSpinner.Name = "RitSpinner";
      RitSpinner.Size = new Size(72, 21);
      RitSpinner.TabIndex = 46;
      RitSpinner.ValueChanged += UiControl_Changed;
      // 
      // label5
      // 
      label5.AutoSize = true;
      label5.Location = new Point(545, 32);
      label5.Name = "label5";
      label5.Size = new Size(47, 15);
      label5.TabIndex = 45;
      label5.Text = "Manual";
      // 
      // label6
      // 
      label6.AutoSize = true;
      label6.Location = new Point(545, 9);
      label6.Name = "label6";
      label6.Size = new Size(49, 15);
      label6.TabIndex = 44;
      label6.Text = "Doppler";
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Location = new Point(210, 32);
      label4.Name = "label4";
      label4.Size = new Size(47, 15);
      label4.TabIndex = 43;
      label4.Text = "Manual";
      // 
      // label3
      // 
      label3.AutoSize = true;
      label3.Location = new Point(210, 9);
      label3.Name = "label3";
      label3.Size = new Size(49, 15);
      label3.TabIndex = 42;
      label3.Text = "Doppler";
      // 
      // UplinkModeCombobox
      // 
      UplinkModeCombobox.AutoCompleteMode = AutoCompleteMode.Suggest;
      UplinkModeCombobox.AutoCompleteSource = AutoCompleteSource.ListItems;
      UplinkModeCombobox.DropDownStyle = ComboBoxStyle.DropDownList;
      UplinkModeCombobox.FormattingEnabled = true;
      UplinkModeCombobox.Location = new Point(444, 9);
      UplinkModeCombobox.Name = "UplinkModeCombobox";
      UplinkModeCombobox.Size = new Size(72, 23);
      UplinkModeCombobox.TabIndex = 41;
      UplinkModeCombobox.SelectedValueChanged += UiControl_Changed;
      // 
      // DownlinkModeCombobox
      // 
      DownlinkModeCombobox.DropDownStyle = ComboBoxStyle.DropDownList;
      DownlinkModeCombobox.FormattingEnabled = true;
      DownlinkModeCombobox.Location = new Point(104, 9);
      DownlinkModeCombobox.Name = "DownlinkModeCombobox";
      DownlinkModeCombobox.Size = new Size(72, 23);
      DownlinkModeCombobox.TabIndex = 40;
      DownlinkModeCombobox.SelectedValueChanged += UiControl_Changed;
      // 
      // UplinkDopplerLabel
      // 
      UplinkDopplerLabel.BackColor = Color.White;
      UplinkDopplerLabel.Font = new Font("Segoe UI", 9.5F);
      UplinkDopplerLabel.Location = new Point(599, 6);
      UplinkDopplerLabel.Name = "UplinkDopplerLabel";
      UplinkDopplerLabel.Size = new Size(56, 20);
      UplinkDopplerLabel.TabIndex = 39;
      UplinkDopplerLabel.Text = "0,000";
      UplinkDopplerLabel.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // DownlinkDopplerLabel
      // 
      DownlinkDopplerLabel.BackColor = Color.White;
      DownlinkDopplerLabel.Font = new Font("Segoe UI", 9.5F);
      DownlinkDopplerLabel.Location = new Point(262, 6);
      DownlinkDopplerLabel.Name = "DownlinkDopplerLabel";
      DownlinkDopplerLabel.Size = new Size(56, 20);
      DownlinkDopplerLabel.TabIndex = 38;
      DownlinkDopplerLabel.Text = "0,000";
      DownlinkDopplerLabel.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold);
      label1.Location = new Point(342, 1);
      label1.Name = "label1";
      label1.Size = new Size(21, 24);
      label1.TabIndex = 37;
      label1.Text = "↑";
      // 
      // UplinkManualSpinner
      // 
      UplinkManualSpinner.BorderStyle = BorderStyle.None;
      UplinkManualSpinner.DecimalPlaces = 3;
      UplinkManualSpinner.Font = new Font("Segoe UI", 9.5F);
      UplinkManualSpinner.Increment = new decimal(new int[] { 2, 0, 0, 131072 });
      UplinkManualSpinner.Location = new Point(599, 30);
      UplinkManualSpinner.Maximum = new decimal(new int[] { 25, 0, 0, 0 });
      UplinkManualSpinner.Minimum = new decimal(new int[] { 25, 0, 0, int.MinValue });
      UplinkManualSpinner.Name = "UplinkManualSpinner";
      UplinkManualSpinner.Size = new Size(72, 20);
      UplinkManualSpinner.TabIndex = 36;
      UplinkManualSpinner.ValueChanged += UiControl_Changed;
      // 
      // UplinkManualCheckbox
      // 
      UplinkManualCheckbox.AutoSize = true;
      UplinkManualCheckbox.Location = new Point(529, 32);
      UplinkManualCheckbox.Name = "UplinkManualCheckbox";
      UplinkManualCheckbox.Size = new Size(15, 14);
      UplinkManualCheckbox.TabIndex = 35;
      UplinkManualCheckbox.UseVisualStyleBackColor = true;
      UplinkManualCheckbox.CheckedChanged += UiControl_Changed;
      // 
      // UplinkDopplerCheckbox
      // 
      UplinkDopplerCheckbox.AutoSize = true;
      UplinkDopplerCheckbox.Location = new Point(529, 9);
      UplinkDopplerCheckbox.Name = "UplinkDopplerCheckbox";
      UplinkDopplerCheckbox.Size = new Size(15, 14);
      UplinkDopplerCheckbox.TabIndex = 34;
      UplinkDopplerCheckbox.UseVisualStyleBackColor = true;
      UplinkDopplerCheckbox.CheckedChanged += UiControl_Changed;
      // 
      // UplinkFrequencyLabel
      // 
      UplinkFrequencyLabel.BackColor = Color.Black;
      UplinkFrequencyLabel.ContextMenuStrip = contextMenuStrip1;
      UplinkFrequencyLabel.Font = new Font("Microsoft Sans Serif", 16F);
      UplinkFrequencyLabel.ForeColor = Color.Aqua;
      UplinkFrequencyLabel.Location = new Point(346, 39);
      UplinkFrequencyLabel.Name = "UplinkFrequencyLabel";
      UplinkFrequencyLabel.Size = new Size(170, 34);
      UplinkFrequencyLabel.TabIndex = 33;
      UplinkFrequencyLabel.Text = "000,000,000";
      UplinkFrequencyLabel.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // contextMenuStrip1
      // 
      contextMenuStrip1.Items.AddRange(new ToolStripItem[] { ShowNominalFrequencyMNU, ShowCorrectedFrequencyMNU });
      contextMenuStrip1.Name = "contextMenuStrip1";
      contextMenuStrip1.Size = new Size(217, 48);
      contextMenuStrip1.Opening += contextMenuStrip1_Opening;
      // 
      // ShowNominalFrequencyMNU
      // 
      ShowNominalFrequencyMNU.Name = "ShowNominalFrequencyMNU";
      ShowNominalFrequencyMNU.Size = new Size(216, 22);
      ShowNominalFrequencyMNU.Text = "Show Nominal Frequency";
      ShowNominalFrequencyMNU.Click += ShowNominalFrequencyMNU_Click;
      // 
      // ShowCorrectedFrequencyMNU
      // 
      ShowCorrectedFrequencyMNU.Name = "ShowCorrectedFrequencyMNU";
      ShowCorrectedFrequencyMNU.Size = new Size(216, 22);
      ShowCorrectedFrequencyMNU.Text = "Show Corrected Frequency";
      ShowCorrectedFrequencyMNU.Click += ShowCorrectedFrequencyMNU_Click;
      // 
      // UplinkLabel
      // 
      UplinkLabel.AutoEllipsis = true;
      UplinkLabel.Location = new Point(360, 10);
      UplinkLabel.Name = "UplinkLabel";
      UplinkLabel.Size = new Size(62, 15);
      UplinkLabel.TabIndex = 32;
      UplinkLabel.Text = "No Uplink";
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold);
      label2.Location = new Point(7, 5);
      label2.Name = "label2";
      label2.Size = new Size(21, 24);
      label2.TabIndex = 31;
      label2.Text = "↓";
      // 
      // DownlinkManualSpinner
      // 
      DownlinkManualSpinner.BorderStyle = BorderStyle.None;
      DownlinkManualSpinner.DecimalPlaces = 3;
      DownlinkManualSpinner.Font = new Font("Segoe UI", 10F);
      DownlinkManualSpinner.Increment = new decimal(new int[] { 2, 0, 0, 131072 });
      DownlinkManualSpinner.Location = new Point(262, 30);
      DownlinkManualSpinner.Maximum = new decimal(new int[] { 25, 0, 0, 0 });
      DownlinkManualSpinner.Minimum = new decimal(new int[] { 25, 0, 0, int.MinValue });
      DownlinkManualSpinner.Name = "DownlinkManualSpinner";
      DownlinkManualSpinner.Size = new Size(72, 21);
      DownlinkManualSpinner.TabIndex = 30;
      DownlinkManualSpinner.ValueChanged += UiControl_Changed;
      // 
      // DownlinkManualCheckbox
      // 
      DownlinkManualCheckbox.AutoSize = true;
      DownlinkManualCheckbox.Location = new Point(194, 32);
      DownlinkManualCheckbox.Name = "DownlinkManualCheckbox";
      DownlinkManualCheckbox.Size = new Size(15, 14);
      DownlinkManualCheckbox.TabIndex = 29;
      DownlinkManualCheckbox.UseVisualStyleBackColor = true;
      DownlinkManualCheckbox.CheckedChanged += UiControl_Changed;
      // 
      // DownlinkDopplerCheckbox
      // 
      DownlinkDopplerCheckbox.AutoSize = true;
      DownlinkDopplerCheckbox.Location = new Point(194, 9);
      DownlinkDopplerCheckbox.Name = "DownlinkDopplerCheckbox";
      DownlinkDopplerCheckbox.Size = new Size(15, 14);
      DownlinkDopplerCheckbox.TabIndex = 28;
      DownlinkDopplerCheckbox.UseVisualStyleBackColor = true;
      DownlinkDopplerCheckbox.CheckedChanged += UiControl_Changed;
      // 
      // DownlinkFrequencyLabel
      // 
      DownlinkFrequencyLabel.BackColor = Color.Black;
      DownlinkFrequencyLabel.ContextMenuStrip = contextMenuStrip1;
      DownlinkFrequencyLabel.Cursor = Cursors.Hand;
      DownlinkFrequencyLabel.Font = new Font("Microsoft Sans Serif", 16F);
      DownlinkFrequencyLabel.ForeColor = Color.Aqua;
      DownlinkFrequencyLabel.Location = new Point(7, 39);
      DownlinkFrequencyLabel.Name = "DownlinkFrequencyLabel";
      DownlinkFrequencyLabel.Size = new Size(170, 34);
      DownlinkFrequencyLabel.TabIndex = 27;
      DownlinkFrequencyLabel.Text = "000,000,000";
      DownlinkFrequencyLabel.TextAlign = ContentAlignment.MiddleCenter;
      DownlinkFrequencyLabel.Click += DownlinkFrequencyLabel_Click;
      // 
      // DownlinkLabel
      // 
      DownlinkLabel.Location = new Point(25, 12);
      DownlinkLabel.Name = "DownlinkLabel";
      DownlinkLabel.Size = new Size(73, 15);
      DownlinkLabel.TabIndex = 26;
      DownlinkLabel.Text = "Terrestrial";
      // 
      // TxBtn
      // 
      TxBtn.Location = new Point(524, 51);
      TxBtn.Name = "TxBtn";
      TxBtn.Size = new Size(131, 21);
      TxBtn.TabIndex = 49;
      TxBtn.Text = "Transmit";
      TxBtn.UseVisualStyleBackColor = true;
      TxBtn.Click += TxBtn_Click;
      // 
      // FrequencyWidget
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      BorderStyle = BorderStyle.FixedSingle;
      Controls.Add(TxBtn);
      Controls.Add(label7);
      Controls.Add(RitCheckbox);
      Controls.Add(RitSpinner);
      Controls.Add(label5);
      Controls.Add(label6);
      Controls.Add(label4);
      Controls.Add(label3);
      Controls.Add(UplinkModeCombobox);
      Controls.Add(DownlinkModeCombobox);
      Controls.Add(UplinkDopplerLabel);
      Controls.Add(DownlinkDopplerLabel);
      Controls.Add(label1);
      Controls.Add(UplinkManualSpinner);
      Controls.Add(UplinkManualCheckbox);
      Controls.Add(UplinkDopplerCheckbox);
      Controls.Add(UplinkFrequencyLabel);
      Controls.Add(UplinkLabel);
      Controls.Add(label2);
      Controls.Add(DownlinkManualSpinner);
      Controls.Add(DownlinkManualCheckbox);
      Controls.Add(DownlinkDopplerCheckbox);
      Controls.Add(DownlinkFrequencyLabel);
      Controls.Add(DownlinkLabel);
      Name = "FrequencyWidget";
      Size = new Size(677, 75);
      ((System.ComponentModel.ISupportInitialize)RitSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)UplinkManualSpinner).EndInit();
      contextMenuStrip1.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)DownlinkManualSpinner).EndInit();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Label label7;
    private CheckBox RitCheckbox;
    private NumericUpDown RitSpinner;
    private Label label5;
    private Label label6;
    private Label label4;
    private Label label3;
    private ComboBox UplinkModeCombobox;
    private ComboBox DownlinkModeCombobox;
    private Label UplinkDopplerLabel;
    private Label DownlinkDopplerLabel;
    private Label label1;
    private NumericUpDown UplinkManualSpinner;
    private CheckBox UplinkManualCheckbox;
    private CheckBox UplinkDopplerCheckbox;
    private Label UplinkFrequencyLabel;
    private Label UplinkLabel;
    private Label label2;
    private NumericUpDown DownlinkManualSpinner;
    private CheckBox DownlinkManualCheckbox;
    private CheckBox DownlinkDopplerCheckbox;
    private Label DownlinkFrequencyLabel;
    private Label DownlinkLabel;
    private ContextMenuStrip contextMenuStrip1;
    private ToolStripMenuItem ShowNominalFrequencyMNU;
    private ToolStripMenuItem ShowCorrectedFrequencyMNU;
    private Button TxBtn;
    private ToolTip toolTip1;
    private Label label8;
  }
}

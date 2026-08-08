namespace SkyRoof
{
  partial class SstvDenoiseDialog
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
      PreviewPanel = new Panel();
      PreviewBox = new PictureBox();
      SidePanel = new Panel();
      ApplyBtn = new Button();
      SkipNoiseBandsCheckBox = new CheckBox();
      NlmGroupBox = new GroupBox();
      NlmTwoPassCheckBox = new CheckBox();
      NlmPatchSpinner = new NumericUpDown();
      NlmPatchLabel = new Label();
      NlmStrengthSpinner = new NumericUpDown();
      NlmStrengthLabel = new Label();
      WienerGroupBox = new GroupBox();
      WienerHeightSpinner = new NumericUpDown();
      WienerHeightLabel = new Label();
      WienerWidthSpinner = new NumericUpDown();
      WienerWidthLabel = new Label();
      AlgorithmGroupBox = new GroupBox();
      NlmRadio = new RadioButton();
      WienerRadio = new RadioButton();
      NoneRadio = new RadioButton();
      BottomPanel = new Panel();
      CancelBtn = new Button();
      OkBtn = new Button();
      StatusLabel = new Label();
      PreviewPanel.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)PreviewBox).BeginInit();
      SidePanel.SuspendLayout();
      NlmGroupBox.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)NlmPatchSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)NlmStrengthSpinner).BeginInit();
      WienerGroupBox.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)WienerHeightSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)WienerWidthSpinner).BeginInit();
      AlgorithmGroupBox.SuspendLayout();
      BottomPanel.SuspendLayout();
      SuspendLayout();
      // 
      // PreviewPanel
      // 
      PreviewPanel.AutoScroll = true;
      PreviewPanel.BackColor = SystemColors.ControlDark;
      PreviewPanel.Controls.Add(PreviewBox);
      PreviewPanel.Dock = DockStyle.Fill;
      PreviewPanel.Location = new Point(0, 0);
      PreviewPanel.Name = "PreviewPanel";
      PreviewPanel.Size = new Size(654, 596);
      PreviewPanel.TabIndex = 0;
      PreviewPanel.ClientSizeChanged += PreviewPanel_ClientSizeChanged;
      // 
      // PreviewBox
      // 
      PreviewBox.Location = new Point(0, 0);
      PreviewBox.Name = "PreviewBox";
      PreviewBox.Size = new Size(640, 480);
      PreviewBox.TabIndex = 0;
      PreviewBox.TabStop = false;
      // 
      // SidePanel
      // 
      SidePanel.Controls.Add(ApplyBtn);
      SidePanel.Controls.Add(SkipNoiseBandsCheckBox);
      SidePanel.Controls.Add(NlmGroupBox);
      SidePanel.Controls.Add(WienerGroupBox);
      SidePanel.Controls.Add(AlgorithmGroupBox);
      SidePanel.Dock = DockStyle.Right;
      SidePanel.Location = new Point(654, 0);
      SidePanel.Name = "SidePanel";
      SidePanel.Size = new Size(250, 596);
      SidePanel.TabIndex = 1;
      // 
      // ApplyBtn
      // 
      ApplyBtn.Location = new Point(24, 406);
      ApplyBtn.Name = "ApplyBtn";
      ApplyBtn.Size = new Size(90, 27);
      ApplyBtn.TabIndex = 4;
      ApplyBtn.Text = "Apply";
      ApplyBtn.UseVisualStyleBackColor = true;
      ApplyBtn.Click += ApplyBtn_Click;
      // 
      // SkipNoiseBandsCheckBox
      // 
      SkipNoiseBandsCheckBox.AutoSize = true;
      SkipNoiseBandsCheckBox.Location = new Point(24, 372);
      SkipNoiseBandsCheckBox.Name = "SkipNoiseBandsCheckBox";
      SkipNoiseBandsCheckBox.Size = new Size(146, 19);
      SkipNoiseBandsCheckBox.TabIndex = 3;
      SkipNoiseBandsCheckBox.Text = "Skip Noise-Only Bands";
      SkipNoiseBandsCheckBox.UseVisualStyleBackColor = true;
      // 
      // NlmGroupBox
      // 
      NlmGroupBox.Controls.Add(NlmTwoPassCheckBox);
      NlmGroupBox.Controls.Add(NlmPatchSpinner);
      NlmGroupBox.Controls.Add(NlmPatchLabel);
      NlmGroupBox.Controls.Add(NlmStrengthSpinner);
      NlmGroupBox.Controls.Add(NlmStrengthLabel);
      NlmGroupBox.Location = new Point(10, 226);
      NlmGroupBox.Name = "NlmGroupBox";
      NlmGroupBox.Size = new Size(230, 128);
      NlmGroupBox.TabIndex = 4;
      NlmGroupBox.TabStop = false;
      NlmGroupBox.Text = "Non-Local Means";
      // 
      // NlmTwoPassCheckBox
      // 
      NlmTwoPassCheckBox.AutoSize = true;
      NlmTwoPassCheckBox.Location = new Point(14, 92);
      NlmTwoPassCheckBox.Name = "NlmTwoPassCheckBox";
      NlmTwoPassCheckBox.Size = new Size(143, 19);
      NlmTwoPassCheckBox.TabIndex = 8;
      NlmTwoPassCheckBox.Text = "Remove Residual Dots";
      NlmTwoPassCheckBox.UseVisualStyleBackColor = true;
      // 
      // NlmPatchSpinner
      // 
      NlmPatchSpinner.Location = new Point(146, 57);
      NlmPatchSpinner.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
      NlmPatchSpinner.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
      NlmPatchSpinner.Name = "NlmPatchSpinner";
      NlmPatchSpinner.Size = new Size(70, 23);
      NlmPatchSpinner.TabIndex = 3;
      NlmPatchSpinner.Value = new decimal(new int[] { 3, 0, 0, 0 });
      // 
      // NlmPatchLabel
      // 
      NlmPatchLabel.AutoSize = true;
      NlmPatchLabel.Location = new Point(12, 60);
      NlmPatchLabel.Name = "NlmPatchLabel";
      NlmPatchLabel.Size = new Size(60, 15);
      NlmPatchLabel.TabIndex = 2;
      NlmPatchLabel.Text = "Patch Size";
      // 
      // NlmStrengthSpinner
      // 
      NlmStrengthSpinner.DecimalPlaces = 2;
      NlmStrengthSpinner.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
      NlmStrengthSpinner.Location = new Point(146, 27);
      NlmStrengthSpinner.Maximum = new decimal(new int[] { 32, 0, 0, 65536 });
      NlmStrengthSpinner.Minimum = new decimal(new int[] { 5, 0, 0, 131072 });
      NlmStrengthSpinner.Name = "NlmStrengthSpinner";
      NlmStrengthSpinner.Size = new Size(70, 23);
      NlmStrengthSpinner.TabIndex = 1;
      NlmStrengthSpinner.Value = new decimal(new int[] { 60, 0, 0, 131072 });
      // 
      // NlmStrengthLabel
      // 
      NlmStrengthLabel.AutoSize = true;
      NlmStrengthLabel.Location = new Point(12, 30);
      NlmStrengthLabel.Name = "NlmStrengthLabel";
      NlmStrengthLabel.Size = new Size(52, 15);
      NlmStrengthLabel.TabIndex = 0;
      NlmStrengthLabel.Text = "Strength";
      // 
      // WienerGroupBox
      // 
      WienerGroupBox.Controls.Add(WienerHeightSpinner);
      WienerGroupBox.Controls.Add(WienerHeightLabel);
      WienerGroupBox.Controls.Add(WienerWidthSpinner);
      WienerGroupBox.Controls.Add(WienerWidthLabel);
      WienerGroupBox.Location = new Point(10, 122);
      WienerGroupBox.Name = "WienerGroupBox";
      WienerGroupBox.Size = new Size(230, 92);
      WienerGroupBox.TabIndex = 3;
      WienerGroupBox.TabStop = false;
      WienerGroupBox.Text = "Wiener";
      // 
      // WienerHeightSpinner
      // 
      WienerHeightSpinner.Increment = new decimal(new int[] { 2, 0, 0, 0 });
      WienerHeightSpinner.Location = new Point(146, 57);
      WienerHeightSpinner.Maximum = new decimal(new int[] { 15, 0, 0, 0 });
      WienerHeightSpinner.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
      WienerHeightSpinner.Name = "WienerHeightSpinner";
      WienerHeightSpinner.Size = new Size(70, 23);
      WienerHeightSpinner.TabIndex = 3;
      WienerHeightSpinner.Value = new decimal(new int[] { 5, 0, 0, 0 });
      // 
      // WienerHeightLabel
      // 
      WienerHeightLabel.AutoSize = true;
      WienerHeightLabel.Location = new Point(12, 60);
      WienerHeightLabel.Name = "WienerHeightLabel";
      WienerHeightLabel.Size = new Size(90, 15);
      WienerHeightLabel.TabIndex = 2;
      WienerHeightLabel.Text = "Window Height";
      // 
      // WienerWidthSpinner
      // 
      WienerWidthSpinner.Increment = new decimal(new int[] { 2, 0, 0, 0 });
      WienerWidthSpinner.Location = new Point(146, 27);
      WienerWidthSpinner.Maximum = new decimal(new int[] { 21, 0, 0, 0 });
      WienerWidthSpinner.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
      WienerWidthSpinner.Name = "WienerWidthSpinner";
      WienerWidthSpinner.Size = new Size(70, 23);
      WienerWidthSpinner.TabIndex = 1;
      WienerWidthSpinner.Value = new decimal(new int[] { 9, 0, 0, 0 });
      // 
      // WienerWidthLabel
      // 
      WienerWidthLabel.AutoSize = true;
      WienerWidthLabel.Location = new Point(12, 30);
      WienerWidthLabel.Name = "WienerWidthLabel";
      WienerWidthLabel.Size = new Size(86, 15);
      WienerWidthLabel.TabIndex = 0;
      WienerWidthLabel.Text = "Window Width";
      // 
      // AlgorithmGroupBox
      // 
      AlgorithmGroupBox.Controls.Add(NlmRadio);
      AlgorithmGroupBox.Controls.Add(WienerRadio);
      AlgorithmGroupBox.Controls.Add(NoneRadio);
      AlgorithmGroupBox.Location = new Point(10, 10);
      AlgorithmGroupBox.Name = "AlgorithmGroupBox";
      AlgorithmGroupBox.Size = new Size(230, 104);
      AlgorithmGroupBox.TabIndex = 0;
      AlgorithmGroupBox.TabStop = false;
      AlgorithmGroupBox.Text = "Algorithm";
      // 
      // NlmRadio
      // 
      NlmRadio.AutoSize = true;
      NlmRadio.Location = new Point(14, 75);
      NlmRadio.Name = "NlmRadio";
      NlmRadio.Size = new Size(119, 19);
      NlmRadio.TabIndex = 2;
      NlmRadio.Text = "Non-Local Means";
      NlmRadio.UseVisualStyleBackColor = true;
      NlmRadio.CheckedChanged += MethodRadio_CheckedChanged;
      // 
      // WienerRadio
      // 
      WienerRadio.AutoSize = true;
      WienerRadio.Location = new Point(14, 50);
      WienerRadio.Name = "WienerRadio";
      WienerRadio.Size = new Size(62, 19);
      WienerRadio.TabIndex = 1;
      WienerRadio.Text = "Wiener";
      WienerRadio.UseVisualStyleBackColor = true;
      WienerRadio.CheckedChanged += MethodRadio_CheckedChanged;
      // 
      // NoneRadio
      // 
      NoneRadio.AutoSize = true;
      NoneRadio.Checked = true;
      NoneRadio.Location = new Point(14, 25);
      NoneRadio.Name = "NoneRadio";
      NoneRadio.Size = new Size(54, 19);
      NoneRadio.TabIndex = 0;
      NoneRadio.TabStop = true;
      NoneRadio.Text = "None";
      NoneRadio.UseVisualStyleBackColor = true;
      NoneRadio.CheckedChanged += MethodRadio_CheckedChanged;
      // 
      // BottomPanel
      // 
      BottomPanel.Controls.Add(CancelBtn);
      BottomPanel.Controls.Add(OkBtn);
      BottomPanel.Controls.Add(StatusLabel);
      BottomPanel.Dock = DockStyle.Bottom;
      BottomPanel.Location = new Point(0, 596);
      BottomPanel.Name = "BottomPanel";
      BottomPanel.Size = new Size(904, 44);
      BottomPanel.TabIndex = 2;
      // 
      // CancelBtn
      // 
      CancelBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      CancelBtn.DialogResult = DialogResult.Cancel;
      CancelBtn.Location = new Point(805, 10);
      CancelBtn.Name = "CancelBtn";
      CancelBtn.Size = new Size(75, 25);
      CancelBtn.TabIndex = 3;
      CancelBtn.Text = "Cancel";
      CancelBtn.UseVisualStyleBackColor = true;
      // 
      // OkBtn
      // 
      OkBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      OkBtn.Location = new Point(724, 10);
      OkBtn.Name = "OkBtn";
      OkBtn.Size = new Size(75, 25);
      OkBtn.TabIndex = 2;
      OkBtn.Text = "OK";
      OkBtn.UseVisualStyleBackColor = true;
      OkBtn.Click += OkBtn_Click;
      // 
      // StatusLabel
      // 
      StatusLabel.AutoSize = true;
      StatusLabel.Location = new Point(14, 15);
      StatusLabel.Name = "StatusLabel";
      StatusLabel.Size = new Size(0, 15);
      StatusLabel.TabIndex = 0;
      // 
      // SstvDenoiseDialog
      // 
      AcceptButton = OkBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = CancelBtn;
      ClientSize = new Size(904, 640);
      Controls.Add(PreviewPanel);
      Controls.Add(SidePanel);
      Controls.Add(BottomPanel);
      MinimizeBox = false;
      Name = "SstvDenoiseDialog";
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterParent;
      Text = "Denoise Image";
      PreviewPanel.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)PreviewBox).EndInit();
      SidePanel.ResumeLayout(false);
      SidePanel.PerformLayout();
      NlmGroupBox.ResumeLayout(false);
      NlmGroupBox.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)NlmPatchSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)NlmStrengthSpinner).EndInit();
      WienerGroupBox.ResumeLayout(false);
      WienerGroupBox.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)WienerHeightSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)WienerWidthSpinner).EndInit();
      AlgorithmGroupBox.ResumeLayout(false);
      AlgorithmGroupBox.PerformLayout();
      BottomPanel.ResumeLayout(false);
      BottomPanel.PerformLayout();
      ResumeLayout(false);
    }

    #endregion

    private Panel PreviewPanel;
    private PictureBox PreviewBox;
    private Panel SidePanel;
    private GroupBox AlgorithmGroupBox;
    private RadioButton NoneRadio;
    private RadioButton WienerRadio;
    private RadioButton NlmRadio;
    private GroupBox WienerGroupBox;
    private Label WienerWidthLabel;
    private NumericUpDown WienerWidthSpinner;
    private Label WienerHeightLabel;
    private NumericUpDown WienerHeightSpinner;
    private GroupBox NlmGroupBox;
    private Label NlmStrengthLabel;
    private NumericUpDown NlmStrengthSpinner;
    private Label NlmPatchLabel;
    private NumericUpDown NlmPatchSpinner;
    private CheckBox NlmTwoPassCheckBox;
    private CheckBox SkipNoiseBandsCheckBox;
    private Panel BottomPanel;
    private Label StatusLabel;
    private Button ApplyBtn;
    private Button OkBtn;
    private Button CancelBtn;
  }
}

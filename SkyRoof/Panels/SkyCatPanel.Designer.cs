namespace SkyRoof
{
  partial class SkyCatPanel
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
      OutputBox = new TextBox();
      TopPanel = new Panel();
      StateLabel = new Label();
      AutoScrollCheck = new CheckBox();
      ClearBtn = new Button();
      TopPanel.SuspendLayout();
      SuspendLayout();
      //
      // OutputBox
      //
      OutputBox.BackColor = SystemColors.Window;
      OutputBox.BorderStyle = BorderStyle.None;
      OutputBox.Dock = DockStyle.Fill;
      OutputBox.Font = new Font("Consolas", 8.5F);
      OutputBox.ForeColor = SystemColors.WindowText;
      OutputBox.Location = new Point(0, 28);
      OutputBox.Multiline = true;
      OutputBox.Name = "OutputBox";
      OutputBox.ReadOnly = true;
      OutputBox.ScrollBars = ScrollBars.Both;
      OutputBox.Size = new Size(700, 372);
      OutputBox.TabIndex = 1;
      OutputBox.WordWrap = false;
      //
      // TopPanel
      //
      TopPanel.Controls.Add(StateLabel);
      TopPanel.Controls.Add(AutoScrollCheck);
      TopPanel.Controls.Add(ClearBtn);
      TopPanel.Dock = DockStyle.Top;
      TopPanel.Location = new Point(0, 0);
      TopPanel.Name = "TopPanel";
      TopPanel.Size = new Size(700, 28);
      TopPanel.TabIndex = 0;
      //
      // StateLabel
      //
      StateLabel.AutoSize = true;
      StateLabel.Location = new Point(6, 7);
      StateLabel.Name = "StateLabel";
      StateLabel.Size = new Size(70, 15);
      StateLabel.TabIndex = 0;
      StateLabel.Text = "Not running";
      //
      // AutoScrollCheck
      //
      AutoScrollCheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      AutoScrollCheck.AutoSize = true;
      AutoScrollCheck.Checked = true;
      AutoScrollCheck.CheckState = CheckState.Checked;
      AutoScrollCheck.Location = new Point(535, 6);
      AutoScrollCheck.Name = "AutoScrollCheck";
      AutoScrollCheck.Size = new Size(80, 19);
      AutoScrollCheck.TabIndex = 1;
      AutoScrollCheck.Text = "Auto scroll";
      AutoScrollCheck.UseVisualStyleBackColor = true;
      AutoScrollCheck.CheckedChanged += AutoScrollCheck_CheckedChanged;
      //
      // ClearBtn
      //
      ClearBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      ClearBtn.Location = new Point(621, 3);
      ClearBtn.Name = "ClearBtn";
      ClearBtn.Size = new Size(66, 23);
      ClearBtn.TabIndex = 2;
      ClearBtn.Text = "Clear";
      ClearBtn.UseVisualStyleBackColor = true;
      ClearBtn.Click += ClearBtn_Click;
      //
      // SkyCatPanel
      //
      ClientSize = new Size(700, 400);
      Controls.Add(OutputBox);
      Controls.Add(TopPanel);
      Name = "SkyCatPanel";
      Text = "SkyCAT";
      FormClosed += SkyCatPanel_FormClosed;
      TopPanel.ResumeLayout(false);
      TopPanel.PerformLayout();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private TextBox OutputBox;
    private Panel TopPanel;
    private Label StateLabel;
    private CheckBox AutoScrollCheck;
    private Button ClearBtn;
  }
}

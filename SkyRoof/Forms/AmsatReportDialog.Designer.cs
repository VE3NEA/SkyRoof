namespace SkyRoof
{
  partial class AmsatReportDialog
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
      comboBox1 = new ComboBox();
      comboBox2 = new ComboBox();
      label1 = new Label();
      label2 = new Label();
      cancelBtn = new Button();
      okBtn = new Button();
      SuspendLayout();
      // 
      // comboBox1
      // 
      comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
      comboBox1.FormattingEnabled = true;
      comboBox1.Location = new Point(12, 27);
      comboBox1.Name = "comboBox1";
      comboBox1.Size = new Size(185, 23);
      comboBox1.TabIndex = 0;
      // 
      // comboBox2
      // 
      comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
      comboBox2.FormattingEnabled = true;
      comboBox2.Location = new Point(12, 80);
      comboBox2.Name = "comboBox2";
      comboBox2.Size = new Size(185, 23);
      comboBox2.TabIndex = 1;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(12, 9);
      label1.Name = "label1";
      label1.Size = new Size(48, 15);
      label1.TabIndex = 2;
      label1.Text = "Satellite";
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(12, 62);
      label2.Name = "label2";
      label2.Size = new Size(77, 15);
      label2.TabIndex = 3;
      label2.Text = "Status Report";
      // 
      // cancelBtn
      // 
      cancelBtn.DialogResult = DialogResult.Cancel;
      cancelBtn.Location = new Point(109, 127);
      cancelBtn.Margin = new Padding(4, 3, 4, 3);
      cancelBtn.Name = "cancelBtn";
      cancelBtn.Size = new Size(88, 27);
      cancelBtn.TabIndex = 9;
      cancelBtn.Text = "Cancel";
      cancelBtn.UseVisualStyleBackColor = true;
      // 
      // okBtn
      // 
      okBtn.DialogResult = DialogResult.OK;
      okBtn.Location = new Point(13, 127);
      okBtn.Margin = new Padding(4, 3, 4, 3);
      okBtn.Name = "okBtn";
      okBtn.Size = new Size(88, 27);
      okBtn.TabIndex = 8;
      okBtn.Text = "OK";
      okBtn.UseVisualStyleBackColor = true;
      okBtn.Click += okBtn_Click;
      // 
      // AmsatReportDialog
      // 
      AcceptButton = okBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = cancelBtn;
      ClientSize = new Size(210, 172);
      Controls.Add(cancelBtn);
      Controls.Add(okBtn);
      Controls.Add(comboBox2);
      Controls.Add(label2);
      Controls.Add(label1);
      Controls.Add(comboBox1);
      FormBorderStyle = FormBorderStyle.FixedToolWindow;
      Name = "AmsatReportDialog";
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterScreen;
      Text = "Report to AMSAT";
      Load += AmsatReportDialog_Load;
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private ComboBox comboBox1;
    private ComboBox comboBox2;
    private Label label1;
    private Label label2;
    private Button cancelBtn;
    private Button okBtn;
  }
}
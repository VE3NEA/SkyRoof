namespace SkyRoof
{
  partial class TextInputForm
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
      promptLabel = new Label();
      textBox = new VE3NEA.TextBoxEx();
      okButton = new Button();
      cancelButton = new Button();
      SuspendLayout();
      //
      // promptLabel
      //
      promptLabel.AutoEllipsis = true;
      promptLabel.Location = new Point(12, 12);
      promptLabel.Name = "promptLabel";
      promptLabel.Size = new Size(308, 18);
      promptLabel.TabIndex = 3;
      promptLabel.Text = "Name:";
      //
      // textBox
      //
      textBox.Location = new Point(12, 33);
      textBox.Name = "textBox";
      textBox.Size = new Size(308, 23);
      textBox.TabIndex = 0;
      //
      // okButton
      //
      okButton.DialogResult = DialogResult.OK;
      okButton.Location = new Point(164, 67);
      okButton.Name = "okButton";
      okButton.Size = new Size(75, 25);
      okButton.TabIndex = 1;
      okButton.Text = "OK";
      okButton.UseVisualStyleBackColor = true;
      //
      // cancelButton
      //
      cancelButton.DialogResult = DialogResult.Cancel;
      cancelButton.Location = new Point(245, 67);
      cancelButton.Name = "cancelButton";
      cancelButton.Size = new Size(75, 25);
      cancelButton.TabIndex = 2;
      cancelButton.Text = "Cancel";
      cancelButton.UseVisualStyleBackColor = true;
      //
      // TextInputForm
      //
      AcceptButton = okButton;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = cancelButton;
      ClientSize = new Size(332, 104);
      Controls.Add(cancelButton);
      Controls.Add(okButton);
      Controls.Add(textBox);
      Controls.Add(promptLabel);
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      Name = "TextInputForm";
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterParent;
      Text = "Rename";
      Shown += TextInputForm_Shown;
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Label promptLabel;
    private VE3NEA.TextBoxEx textBox;
    private Button okButton;
    private Button cancelButton;
  }
}

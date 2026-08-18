namespace SkyRoof
{
    partial class UserDetailsDialog
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
      panel1 = new Panel();
      cancelBtn = new Button();
      okBtn = new Button();
      flowLayoutPanel1 = new FlowLayoutPanel();
      label1 = new Label();
      label2 = new Label();
      textBox1 = new VE3NEA.TextBoxEx();
      label3 = new Label();
      textBox2 = new VE3NEA.TextBoxEx();
      label4 = new Label();
      numericUpDown1 = new NumericUpDown();
      panel1.SuspendLayout();
      flowLayoutPanel1.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
      SuspendLayout();
      // 
      // panel1
      // 
      panel1.Controls.Add(cancelBtn);
      panel1.Controls.Add(okBtn);
      panel1.Dock = DockStyle.Bottom;
      panel1.Location = new Point(0, 188);
      panel1.Margin = new Padding(4, 3, 4, 3);
      panel1.Name = "panel1";
      panel1.Size = new Size(231, 35);
      panel1.TabIndex = 2;
      // 
      // cancelBtn
      // 
      cancelBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      cancelBtn.DialogResult = DialogResult.Cancel;
      cancelBtn.Location = new Point(139, 6);
      cancelBtn.Margin = new Padding(4, 3, 4, 3);
      cancelBtn.Name = "cancelBtn";
      cancelBtn.Size = new Size(88, 27);
      cancelBtn.TabIndex = 1;
      cancelBtn.Text = "Cancel";
      cancelBtn.UseVisualStyleBackColor = true;
      // 
      // okBtn
      // 
      okBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      okBtn.DialogResult = DialogResult.OK;
      okBtn.Location = new Point(40, 5);
      okBtn.Margin = new Padding(4, 3, 4, 3);
      okBtn.Name = "okBtn";
      okBtn.Size = new Size(88, 27);
      okBtn.TabIndex = 0;
      okBtn.Text = "OK";
      okBtn.UseVisualStyleBackColor = true;
      okBtn.Click += okBtn_Click;
      // 
      // flowLayoutPanel1
      // 
      flowLayoutPanel1.Controls.Add(label1);
      flowLayoutPanel1.Controls.Add(label2);
      flowLayoutPanel1.Controls.Add(textBox1);
      flowLayoutPanel1.Controls.Add(label3);
      flowLayoutPanel1.Controls.Add(textBox2);
      flowLayoutPanel1.Controls.Add(label4);
      flowLayoutPanel1.Controls.Add(numericUpDown1);
      flowLayoutPanel1.Dock = DockStyle.Fill;
      flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
      flowLayoutPanel1.Location = new Point(0, 0);
      flowLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
      flowLayoutPanel1.Name = "flowLayoutPanel1";
      flowLayoutPanel1.Padding = new Padding(12);
      flowLayoutPanel1.Size = new Size(231, 188);
      flowLayoutPanel1.TabIndex = 1;
      // 
      // label1
      // 
      label1.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
      label1.Location = new Point(16, 12);
      label1.Margin = new Padding(4, 0, 4, 0);
      label1.Name = "label1";
      label1.Size = new Size(202, 27);
      label1.TabIndex = 99;
      label1.Text = "User Details";
      label1.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(16, 39);
      label2.Margin = new Padding(4, 0, 4, 0);
      label2.Name = "label2";
      label2.Size = new Size(49, 15);
      label2.TabIndex = 98;
      label2.Text = "Callsign";
      // 
      // textBox1
      // 
      textBox1.CharacterCasing = CharacterCasing.Upper;
      textBox1.Location = new Point(16, 57);
      textBox1.Margin = new Padding(4, 3, 4, 3);
      textBox1.Name = "textBox1";
      textBox1.Size = new Size(200, 23);
      textBox1.TabIndex = 0;
      textBox1.TextChanged += textBoxes_TextChanged;
      // 
      // label3
      // 
      label3.AutoSize = true;
      label3.ForeColor = Color.Red;
      label3.Location = new Point(16, 83);
      label3.Margin = new Padding(4, 0, 4, 0);
      label3.Name = "label3";
      label3.Size = new Size(131, 15);
      label3.TabIndex = 3;
      label3.Text = "6-character Grid Square";
      // 
      // textBox2
      // 
      textBox2.CharacterCasing = CharacterCasing.Upper;
      textBox2.Location = new Point(16, 101);
      textBox2.Margin = new Padding(4, 3, 4, 3);
      textBox2.Name = "textBox2";
      textBox2.Size = new Size(200, 23);
      textBox2.TabIndex = 1;
      textBox2.TextChanged += textBoxes_TextChanged;
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Location = new Point(16, 127);
      label4.Margin = new Padding(4, 0, 4, 0);
      label4.Name = "label4";
      label4.Size = new Size(91, 15);
      label4.TabIndex = 101;
      label4.Text = "Altitude, meters";
      // 
      // numericUpDown1
      // 
      numericUpDown1.Location = new Point(15, 145);
      numericUpDown1.Maximum = new decimal(new int[] { 8849, 0, 0, 0 });
      numericUpDown1.Name = "numericUpDown1";
      numericUpDown1.Size = new Size(200, 23);
      numericUpDown1.TabIndex = 100;
      // 
      // UserDetailsDialog
      // 
      AcceptButton = okBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = cancelBtn;
      ClientSize = new Size(231, 223);
      Controls.Add(flowLayoutPanel1);
      Controls.Add(panel1);
      FormBorderStyle = FormBorderStyle.FixedToolWindow;
      Margin = new Padding(4, 3, 4, 3);
      Name = "UserDetailsDialog";
      StartPosition = FormStartPosition.CenterParent;
      Text = "User Details";
      panel1.ResumeLayout(false);
      flowLayoutPanel1.ResumeLayout(false);
      flowLayoutPanel1.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
      ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.Button okBtn;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox2;
    private Label label4;
    private NumericUpDown numericUpDown1;
  }
}
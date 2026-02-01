
namespace SkyRoof
{
  partial class LoqFt4QsoDialog
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
      imageList1 = new ImageList(components);
      pictureBox1 = new PictureBox();
      label1 = new Label();
      SaveBtn = new Button();
      EditBtn = new Button();
      CancelBtn = new Button();
      ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
      SuspendLayout();
      // 
      // imageList1
      // 
      imageList1.ColorDepth = ColorDepth.Depth32Bit;
      imageList1.ImageSize = new Size(16, 16);
      imageList1.TransparentColor = Color.Transparent;
      // 
      // pictureBox1
      // 
      pictureBox1.Image = Properties.Resources.database_plus_48;
      pictureBox1.Location = new Point(12, 12);
      pictureBox1.Name = "pictureBox1";
      pictureBox1.Size = new Size(48, 48);
      pictureBox1.TabIndex = 0;
      pictureBox1.TabStop = false;
      // 
      // label1
      // 
      label1.Location = new Point(79, 12);
      label1.Name = "label1";
      label1.Size = new Size(259, 23);
      label1.TabIndex = 1;
      label1.Text = "Save FT4 QSO with ZZ0ZZ?";
      label1.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // SaveBtn
      // 
      SaveBtn.Location = new Point(79, 44);
      SaveBtn.Name = "SaveBtn";
      SaveBtn.Size = new Size(75, 23);
      SaveBtn.TabIndex = 2;
      SaveBtn.Text = "Save";
      SaveBtn.UseVisualStyleBackColor = true;
      SaveBtn.Click += SaveBtn_Click;
      // 
      // EditBtn
      // 
      EditBtn.Location = new Point(171, 44);
      EditBtn.Name = "EditBtn";
      EditBtn.Size = new Size(75, 23);
      EditBtn.TabIndex = 3;
      EditBtn.Text = "Edit";
      EditBtn.UseVisualStyleBackColor = true;
      EditBtn.Click += EditBtn_Click;
      // 
      // CancelBtn
      // 
      CancelBtn.Location = new Point(263, 44);
      CancelBtn.Name = "CancelBtn";
      CancelBtn.Size = new Size(75, 23);
      CancelBtn.TabIndex = 4;
      CancelBtn.Text = "Cancel";
      CancelBtn.UseVisualStyleBackColor = true;
      CancelBtn.Click += CancelBtn_Click;
      // 
      // LoqFt4QsoDialog
      // 
      AcceptButton = SaveBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = CancelBtn;
      ClientSize = new Size(352, 75);
      Controls.Add(CancelBtn);
      Controls.Add(EditBtn);
      Controls.Add(SaveBtn);
      Controls.Add(label1);
      Controls.Add(pictureBox1);
      FormBorderStyle = FormBorderStyle.FixedToolWindow;
      MaximizeBox = false;
      MdiChildrenMinimizedAnchorBottom = false;
      MinimizeBox = false;
      Name = "LoqFt4QsoDialog";
      ShowInTaskbar = false;
      SizeGripStyle = SizeGripStyle.Hide;
      StartPosition = FormStartPosition.Manual;
      Text = "Save QSO";
      TopMost = true;
      FormClosing += LoqFt4QsoDialog_FormClosing;
      Move += LoqFt4QsoDialog_Move;
      ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
      ResumeLayout(false);
    }

    #endregion

    private ImageList imageList1;
    private PictureBox pictureBox1;
    private Label label1;
    private Button SaveBtn;
    private Button EditBtn;
    private Button CancelBtn;
  }
}
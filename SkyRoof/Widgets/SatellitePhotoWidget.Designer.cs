namespace SkyRoof
{
  partial class SatellitePhotoWidget
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
      if (disposing)
      {
        Cts?.Cancel();
        Cts?.Dispose();
        components?.Dispose();
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
      Picture = new PictureBox();
      Tooltip = new ToolTip(components);
      ((System.ComponentModel.ISupportInitialize)Picture).BeginInit();
      SuspendLayout();
      // 
      // Picture
      // 
      Picture.BackColor = Color.Transparent;
      Picture.Cursor = Cursors.Hand;
      Picture.Dock = DockStyle.Fill;
      Picture.Location = new Point(0, 0);
      Picture.Name = "Picture";
      Picture.Size = new Size(90, 78);
      Picture.SizeMode = PictureBoxSizeMode.Zoom;
      Picture.TabIndex = 0;
      Picture.TabStop = false;
      Picture.Click += Picture_Click;
      // 
      // SatellitePhotoWidget
      // 
      BorderStyle = BorderStyle.FixedSingle;
      Controls.Add(Picture);
      Name = "SatellitePhotoWidget";
      Size = new Size(90, 78);
      ((System.ComponentModel.ISupportInitialize)Picture).EndInit();
      ResumeLayout(false);
    }

    #endregion

    private PictureBox Picture;
    private ToolTip Tooltip;
  }
}

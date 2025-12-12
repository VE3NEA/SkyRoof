namespace SkyRoof
{
  partial class AudioWaterfallWidget
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
      toolTip1 = new ToolTip(components);
      SuspendLayout();
      // 
      // AudioWaterfallWidget
      // 
      AutoScaleDimensions = new SizeF(9F, 17F);
      AutoScaleMode = AutoScaleMode.Font;
      Cursor = Cursors.Cross;
      DoubleBuffered = true;
      Font = new Font("Courier New", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
      Margin = new Padding(4, 3, 4, 3);
      Name = "AudioWaterfallWidget";
      Size = new Size(606, 182);
      Resize += AudioWaterfallWidget_Resize;
      ResumeLayout(false);
    }

    #endregion

    private ToolTip toolTip1;
  }
}

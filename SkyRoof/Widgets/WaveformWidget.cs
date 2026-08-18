using MathNet.Numerics;
using System.ComponentModel;
using VE3NEA;

namespace SkyRoof
{
  public class WaveformWidget : UserControl
  {
    private static readonly Color waveformForegroundColor = Color.Aqua;
    private static readonly Color waveformAxisColor = Color.Teal;

    private readonly List<MarkerInfo> waveformMarkers = [];
    private readonly VE3NEA.ToolTipEx toolTip = new();
    private string? hoveredMarkerTooltip;
    private RecordingManager? recordingManager;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RecordingManager? RecordingManager
    {
      get => recordingManager;
      set
      {
        if (recordingManager != null) recordingManager.Changed -= RecordingManager_Changed;
        recordingManager = value;
        if (recordingManager != null) recordingManager.Changed += RecordingManager_Changed;
        Invalidate();
      }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int GainDb { get; set; }

    public event EventHandler<SeekRequestedEventArgs>? SeekRequested;

    private enum MarkerShape { Circle, Triangle, Square }

    private sealed class MarkerInfo(RecordingEvents.RecordingEvent recordingEvent, Rectangle bounds)
    {
      public RecordingEvents.RecordingEvent RecordingEvent { get; } = recordingEvent;
      public Rectangle Bounds { get; } = bounds;
    }

    public sealed class SeekRequestedEventArgs(int position) : EventArgs
    {
      public int Position { get; } = position;
    }

    public WaveformWidget()
    {
      DoubleBuffered = true;
      BackColor = Color.Black; // fixed: instrument display
      toolTip.ShowAlways = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      if (RecordingManager == null) return;

      var waveformRect = DrawWaveformBackground(e.Graphics, ClientRectangle);
      DrawWaveformSamples(e.Graphics, waveformRect);
      DrawRecordingMarkers(e.Graphics, waveformRect);
      DrawTimeScale(e.Graphics, waveformRect, ClientRectangle);
      DrawPlaybackPosition(e.Graphics, waveformRect, ClientRectangle);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      if (RecordingManager == null || !RecordingManager.IsPlayingBack || RecordingManager.SamplesInBuffer <= 0 || ClientRectangle.Width <= 0) return;

      int position = (int)Math.Round(e.X * (double)RecordingManager.SamplesInBuffer / ClientRectangle.Width);
      SeekRequested?.Invoke(this, new(position));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
      base.OnMouseMove(e);

      string? tooltip = GetMarkerInfo(e.Location)?.RecordingEvent.GetTooltipText();
      if (tooltip == hoveredMarkerTooltip) return;

      hoveredMarkerTooltip = tooltip;
      toolTip.SetToolTip(this, tooltip ?? string.Empty);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
      base.OnMouseLeave(e);

      if (hoveredMarkerTooltip == null) return;

      hoveredMarkerTooltip = null;
      toolTip.SetToolTip(this, string.Empty);
    }

    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);
      Invalidate();
    }

    private void DrawPlaybackPosition(Graphics g, Rectangle waveformRect, Rectangle bounds)
    {
      if (RecordingManager == null || RecordingManager.SamplesInBuffer <= 0 || waveformRect.Width <= 0) return;
      if (!RecordingManager.IsPlayingBack && RecordingManager.PlaybackPosition <= 0) return;

      int x = waveformRect.Left + (int)Math.Round(((double)waveformRect.Width - 1) * RecordingManager.PlaybackPosition / RecordingManager.SamplesInBuffer);
      x = Math.Clamp(x, waveformRect.Left, waveformRect.Right - 1);

      using var pen = new Pen(Color.Red);
      if (!RecordingManager.IsPlayingBack) pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
      int lineTop = Math.Max(bounds.Top, waveformRect.Top - GetMaxMarkerSize());
      int lineBottom = Math.Min(bounds.Bottom - 1, waveformRect.Bottom + 2 + 8);
      g.DrawLine(pen, x, lineTop, x, lineBottom);
    }

    private Rectangle DrawWaveformBackground(Graphics g, Rectangle bounds)
    {
      int textHeight = TextRenderer.MeasureText("0", Font, Size, TextFormatFlags.NoPadding).Height;
      int scaleHeight = textHeight * 2;
      int markerHeight = GetMaxMarkerSize();
      Rectangle waveformRect = new(bounds.Left, bounds.Top + markerHeight, bounds.Width, Math.Max(1, bounds.Height - scaleHeight - markerHeight));

      int midY = waveformRect.Top + waveformRect.Height / 2;
      using var axisPen = new Pen(waveformAxisColor);
      g.DrawLine(axisPen, waveformRect.Left, midY, waveformRect.Right - 1, midY);
      g.DrawLine(axisPen, waveformRect.Left, waveformRect.Top, waveformRect.Left, waveformRect.Bottom - 1);

      return waveformRect;
    }

    private void DrawTimeScale(Graphics g, Rectangle waveformRect, Rectangle bounds)
    {
      double totalSeconds = RecordingManager?.GetVisibleDurationSeconds() ?? 0;

      if (totalSeconds <= 0 || waveformRect.Width <= 10) return;

      double[] stepsSec = { 1, 2, 5, 10, 20, 30, 60, 120, 300, 600, 1200, 1800, 3600 };
      double[] smallStepsSec = { 1.0 / 6.0, 0.5, 1, 2, 5, 10, 30, 60, 150, 300, 600, 900, 1800 };

      double pixelsPerSecond = waveformRect.Width / Math.Max(1.0, totalSeconds);

      double step = 0;
      double smallStep = 0;
      for (int i = 0; i < stepsSec.Length; i++)
        if (stepsSec[i] * pixelsPerSecond > 59)
        {
          step = stepsSec[i];
          smallStep = smallStepsSec[i];
          break;
        }
      if (step == 0) return;

      g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
      int scaleTopY = Math.Min(bounds.Bottom - 1, waveformRect.Bottom + 2);

      using var scalePen = new Pen(waveformAxisColor);
      g.DrawLine(scalePen, waveformRect.Left, scaleTopY, waveformRect.Right - 1, scaleTopY);

      double t = 0;
      int smallLen = 4;
      while (t <= totalSeconds)
      {
        int x = waveformRect.Left + (int)Math.Round(t * pixelsPerSecond);
        g.DrawLine(scalePen, x, scaleTopY, x, scaleTopY + smallLen);
        t += smallStep;
      }

      t = 0;
      int majorLen = 8;
      using var labelBrush = new SolidBrush(waveformAxisColor);
      while (t <= totalSeconds)
      {
        int x = waveformRect.Left + (int)Math.Round(t * pixelsPerSecond);
        g.DrawLine(scalePen, x, scaleTopY, x, scaleTopY + majorLen);

        TimeSpan ts = TimeSpan.FromSeconds(t);
        string label = ts.TotalHours >= 1 ? $"{(int)ts.TotalHours:D2}:{ts:mm\\:ss}" : $"{ts:mm\\:ss}";
        var size = TextRenderer.MeasureText(label, Font, Size, TextFormatFlags.NoPadding);
        g.DrawString(label, Font, labelBrush, x - size.Width / 2, scaleTopY + majorLen + 2);

        t += step;
      }
    }

    private void DrawWaveformSamples(Graphics g, Rectangle waveformRect)
    {
      if (RecordingManager == null) return;

      int recordedSamples = Math.Max(RecordingManager.SamplesInBuffer, 0);
      if (recordedSamples == 0) return;

      int visibleSamples = RecordingManager.IsRecording ? RecordingManager.AudioBuffer?.Length ?? RecordingManager.IqBuffer?.Length ?? 0 : recordedSamples;
      if (visibleSamples <= 0) return;

      float halfHeight = Math.Max(1, (waveformRect.Height - 2) / 2f);
      double gain = 0.1 * Dsp.FromDb2((float)GainDb);

      using var waveformPen = new Pen(waveformForegroundColor);
      int pixelWidth = Math.Max(1, waveformRect.Width);

      for (int x = 0; x < pixelWidth; x++)
      {
        int start = (int)((long)x * visibleSamples / pixelWidth);
        int end = (int)((long)(x + 1) * visibleSamples / pixelWidth);
        if (end <= start) end = Math.Min(visibleSamples, start + 1);
        if (start >= recordedSamples) break;
        if (end > recordedSamples) end = recordedSamples;

        float maxMagnitude = 0;
        if (RecordingManager.AudioBuffer != null)
          for (int i = start; i < end; i++)
            maxMagnitude = Math.Max(maxMagnitude, Math.Abs(RecordingManager.AudioBuffer[i]));
        else if (RecordingManager.IqBuffer != null)
          for (int i = start; i < end; i++)
            maxMagnitude = Math.Max(maxMagnitude, RecordingManager.IqBuffer[i].Magnitude);

        int amplitude = Math.Min((int)Math.Round(maxMagnitude * gain * halfHeight), (int)halfHeight);
        if (amplitude <= 0) continue;

        int pixelX = waveformRect.Left + x;
        g.DrawLine(waveformPen, pixelX, waveformRect.Top + waveformRect.Height / 2 - amplitude, pixelX, waveformRect.Top + waveformRect.Height / 2 + amplitude);
      }
    }

    private void DrawRecordingMarkers(Graphics g, Rectangle waveformRect)
    {
      waveformMarkers.Clear();

      if (RecordingManager == null) return;

      double totalSeconds = RecordingManager.GetVisibleDurationSeconds();
      if (totalSeconds <= 0 || waveformRect.Width <= 0 || RecordingManager.RecordingEvents.Events.Count == 0) return;

      int markerTop = waveformRect.Top;

      foreach (var recordingEvent in RecordingManager.RecordingEvents.Events.OrderBy(e => e.Utc))
      {
        double eventSeconds = RecordingManager.RecordingEvents.GetRelativeSeconds(recordingEvent);
        if (eventSeconds < 0 || eventSeconds > totalSeconds) continue;

        int x = waveformRect.Left + (int)Math.Round((waveformRect.Width - 1) * eventSeconds / totalSeconds);
        int markerSize = GetMarkerSize(recordingEvent.EventType);

        Rectangle bounds = new(
          x - markerSize / 2,
          markerTop - markerSize,
          markerSize,
          markerSize);

        DrawMarker(g, bounds, recordingEvent.EventType);
        waveformMarkers.Add(new(recordingEvent, bounds));
      }
    }

    private void DrawMarker(Graphics g, Rectangle bounds, string eventType)
    {
      var (color, shape) = GetMarkerStyle(eventType);

      using var brush = new SolidBrush(color);
      using var pen = new Pen(Color.Black); // fixed: on the black display

      switch (shape)
      {
        case MarkerShape.Circle:
          g.FillEllipse(brush, bounds);
          g.DrawEllipse(pen, bounds);
          break;

        case MarkerShape.Triangle:
          g.FillPolygon(brush,
          [
            new Point(bounds.Left, bounds.Top),
            new Point(bounds.Right - 1, bounds.Top + bounds.Height / 2),
            new Point(bounds.Left, bounds.Bottom - 1),
          ]);
          g.DrawPolygon(pen,
          [
            new Point(bounds.Left, bounds.Top),
            new Point(bounds.Right - 1, bounds.Top + bounds.Height / 2),
            new Point(bounds.Left, bounds.Bottom - 1),
          ]);
          break;

        case MarkerShape.Square:
          g.FillRectangle(brush, bounds);
          g.DrawRectangle(pen, bounds);
          break;
      }
    }

    private (Color color, MarkerShape shape) GetMarkerStyle(string eventType)
    {
      return eventType switch
      {
        "satellite" => (Color.Lime, MarkerShape.Circle),
        "transmitter" => (Color.Fuchsia, MarkerShape.Triangle),
        "mode" => (Color.Yellow, MarkerShape.Triangle),
        "qso" => (Color.Aqua, MarkerShape.Square),
        _ => (Color.White, MarkerShape.Circle), // fixed: on the black display
      };
    }

    private int GetMarkerSize(string? eventType = null)
    {
      int textHeight = TextRenderer.MeasureText("0", Font, Size, TextFormatFlags.NoPadding).Height;
      int markerSize = Math.Max(10, (int)Math.Round(textHeight * 0.8));
      if (eventType is "transmitter" or "mode")
        markerSize = Math.Max(markerSize, (int)Math.Round(markerSize * 1.15));

      return markerSize;
    }

    private int GetMaxMarkerSize()
    {
      return Math.Max(GetMarkerSize(), GetMarkerSize("transmitter"));
    }

    private MarkerInfo? GetMarkerInfo(Point location)
    {
      for (int i = waveformMarkers.Count - 1; i >= 0; i--)
        if (waveformMarkers[i].Bounds.Contains(location))
          return waveformMarkers[i];

      return null;
    }

    private void RecordingManager_Changed(object? sender, EventArgs e)
    {
      if (IsDisposed) return;
      if (InvokeRequired) BeginInvoke(Invalidate);
      else Invalidate();
    }
  }
}

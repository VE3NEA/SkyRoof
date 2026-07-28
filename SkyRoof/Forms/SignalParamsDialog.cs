using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using SkyRoof.Satellites;
using VE3NEA.SkyTlm.Core;

namespace SkyRoof
{
  // modal editor for the SignalParams used to decode the selected transmitter's telemetry, plus the telemetry
  // format (the field-decoder definition) its frames are parsed with. Shows the values actually in use (resolved
  // from the DB or found by the pipeline) and lets the user override any of them, or reset one back to its
  // DB-derived value from the right-click menu. A status dot to the right of each field shows its provenance:
  // transparent = the DB value, yellow = edited by the user (pending), green = an override (manual or pipeline)
  // that has produced a frame. The dot colors are computed by the caller and passed in via SignalParamsView; the
  // dialog re-derives a field's dot live as it is edited or reset.
  public partial class SignalParamsDialog : Form
  {
    // the "Auto" tri-state combo items, in index order (null / true / false)
    private static readonly string[] TriStateItems = { "Auto", "On", "Off" };

    // dot colors, shared with the caller so the gear button uses the same yellow/green
    public static readonly Color EditedColor = Color.Orange;
    public static readonly Color ConfirmedColor = Color.LimeGreen;

    // provenance of a field's current value, mapped to a dot color
    public enum FieldDot { None, Edited, Confirmed }

    // one field's dot: its panel and control, the initial (caller-computed) state, a predicate that reports
    // whether the control currently holds the DB-derived value, a reader for its current value, the two
    // baselines that value is compared against, and the reset action.
    //
    // The baselines are separate because they answer different questions. Opened is what the field held when
    // the dialog opened and never moves: it is what OK reports the changes against, so a discovery result
    // still leaves the dialog with parameters to apply. Populated is what the dot's Initial state describes,
    // and a discovery result re-baselines it, so its dot reads as the found value rather than as an edit.
    private sealed class DotBinding
    {
      internal string Name = "";
      internal Panel Dot = null!;
      internal Control Control = null!;
      internal FieldDot Initial;
      internal Func<bool> AtDb = () => false;
      internal Func<object?> Value = () => null;
      internal object? Opened;
      internal object? Populated;
      internal Action Reset = () => { };

      internal bool AtOpened() => Equals(Value(), Opened);
      internal bool AtPopulated() => Equals(Value(), Populated);
    }

    private readonly List<DotBinding> Bindings = new();

    // current fill color per dot panel; null = transparent (nothing drawn, blends into the form)
    private readonly Dictionary<Panel, Color?> DotColors = new();

    // shared right-click menu with a single "reset to database value" command, wired to every value control
    private ContextMenuStrip ResetMenu = null!;

    // control the dialog is centered over (the Telemetry panel); null falls back to the designer's CenterParent
    private Control? Anchor;

    private SignalParams Original;

    // the edited parameters, valid after the dialog returns DialogResult.OK
    public SignalParams Result { get; private set; }

    // the telemetry-format definition id the user chose (null = none selected), valid after DialogResult.OK
    public string? ResultFormatId { get; private set; }

    // names of the fields the user moved to a new manual value, and the names of those reset to the DB value
    public HashSet<string> ChangedFields { get; } = new();
    public HashSet<string> ResetFields { get; } = new();

    public SignalParamsDialog()
    {
      InitializeComponent();
      WireDots();
      SetupResetMenu();
    }

    public DialogResult Open(SignalParamsView view, Control? anchor = null)
    {
      Anchor = anchor;
      if (anchor != null) StartPosition = FormStartPosition.Manual;

      Original = view.Params;
      Result = view.Params;
      ResultFormatId = view.FormatId;

      ModulationCombo.DataSource = Enum.GetValues(typeof(Modulation));
      FramingCombo.DataSource = Enum.GetValues(typeof(Framing));
      ManchesterCombo.DataSource = (string[])TriStateItems.Clone();
      DifferentialCombo.DataSource = (string[])TriStateItems.Clone();
      TelemetryFormatCombo.DataSource = new List<string>(view.FormatIds);

      PopulateControls(view);
      BuildBindings(view);
      // Save to overrides persists what a search found, so it stays disabled until a search has found
      // something: with no result there is nothing to write beyond what the database already says (§6.1).
      SaveOverrideBtn.Enabled = false;
      return ShowDialog();
    }

    // center over the anchor (the Telemetry panel) once the size is final, clamped to the screen work area
    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (Anchor == null || !Anchor.IsHandleCreated) return;

      var r = Anchor.RectangleToScreen(Anchor.ClientRectangle);
      var wa = Screen.FromRectangle(r).WorkingArea;
      int x = Math.Max(wa.Left, Math.Min(r.Left + (r.Width - Width) / 2, wa.Right - Width));
      int y = Math.Max(wa.Top, Math.Min(r.Top + (r.Height - Height) / 2, wa.Bottom - Height));
      Location = new Point(x, y);
    }


    //----------------------------------------------------------------------------------------------
    //                                         controls
    //----------------------------------------------------------------------------------------------
    private void PopulateControls(SignalParamsView view)
    {
      // pre-fill the editable fields with the values actually used for decoding: the run-time finding when the
      // pipeline resolved one, otherwise the curated value.
      ModulationCombo.SelectedItem = Original.Modulation;
      FramingCombo.SelectedItem = Original.Framing;
      BaudTextBox.Text = FormatRate(Original.ResolvedBaud ?? Original.Baud);
      DeviationTextBox.Text = FormatRate(Original.ResolvedDeviation ?? Original.Deviation);
      AfCarrierTextBox.Text = FormatNullable(Original.AfCarrier);
      ManchesterCombo.SelectedIndex = BoolToIndex(Original.Manchester);
      DifferentialCombo.SelectedIndex = BoolToIndex(Original.Differential);

      // the NORAD-resolved format is pre-selected, or the combo is left empty when no format matches
      if (view.FormatId != null && view.FormatIds.Contains(view.FormatId))
        TelemetryFormatCombo.SelectedItem = view.FormatId;
      else
        TelemetryFormatCombo.SelectedIndex = -1;
    }

    // capture each field's populated (in-use) value and its DB-derived value, then wire a change handler so the
    // dot is re-derived live: transparent at the DB value, its opening color at the populated value, else yellow.
    private void BuildBindings(SignalParamsView view)
    {
      var db = view.DbParams;

      object dbMod = db.Modulation;
      object dbFraming = db.Framing;
      string dbBaud = FormatRate(db.ResolvedBaud ?? db.Baud);
      string dbDev = FormatRate(db.ResolvedDeviation ?? db.Deviation);
      string dbAf = FormatNullable(db.AfCarrier);
      int dbMan = BoolToIndex(db.Manchester);
      int dbDiff = BoolToIndex(db.Differential);
      int dbFmt = FormatIndex(view.FormatIds, view.DbFormatId);

      Bind("Modulation", ModulationDot, view.ModulationDot, ModulationCombo,
        () => Equals(ModulationCombo.SelectedItem, dbMod),
        () => ModulationCombo.SelectedItem,
        () => ModulationCombo.SelectedItem = dbMod);

      Bind("Framing", FramingDot, view.FramingDot, FramingCombo,
        () => Equals(FramingCombo.SelectedItem, dbFraming),
        () => FramingCombo.SelectedItem,
        () => FramingCombo.SelectedItem = dbFraming);

      Bind("Baud", BaudDot, view.BaudDot, BaudTextBox,
        () => BaudTextBox.Text == dbBaud,
        () => BaudTextBox.Text,
        () => BaudTextBox.Text = dbBaud);

      Bind("Deviation", DeviationDot, view.DeviationDot, DeviationTextBox,
        () => DeviationTextBox.Text == dbDev,
        () => DeviationTextBox.Text,
        () => DeviationTextBox.Text = dbDev);

      Bind("AfCarrier", AfCarrierDot, view.AfCarrierDot, AfCarrierTextBox,
        () => AfCarrierTextBox.Text == dbAf,
        () => AfCarrierTextBox.Text,
        () => AfCarrierTextBox.Text = dbAf);

      Bind("Manchester", ManchesterDot, view.ManchesterDot, ManchesterCombo,
        () => ManchesterCombo.SelectedIndex == dbMan,
        () => ManchesterCombo.SelectedIndex,
        () => ManchesterCombo.SelectedIndex = dbMan);

      Bind("Differential", DifferentialDot, view.DifferentialDot, DifferentialCombo,
        () => DifferentialCombo.SelectedIndex == dbDiff,
        () => DifferentialCombo.SelectedIndex,
        () => DifferentialCombo.SelectedIndex = dbDiff);

      Bind("TelemetryFormat", TelemetryFormatDot, view.FormatDot, TelemetryFormatCombo,
        () => TelemetryFormatCombo.SelectedIndex == dbFmt,
        () => TelemetryFormatCombo.SelectedIndex,
        () => TelemetryFormatCombo.SelectedIndex = dbFmt);
    }

    private void Bind(string name, Panel dot, FieldDot initial, Control control,
      Func<bool> atDb, Func<object?> value, Action reset)
    {
      var b = new DotBinding
      {
        Name = name, Dot = dot, Control = control, Initial = initial,
        AtDb = atDb, Value = value, Opened = value(), Populated = value(), Reset = reset
      };
      Bindings.Add(b);
      DotColors[dot] = ColorFor(initial);

      void Handler(object? sender, EventArgs e) => RefreshDot(b);
      if (control is ComboBox cb) cb.SelectedIndexChanged += Handler;
      else if (control is TextBox tb) tb.TextChanged += Handler;
    }

    // the DB value is transparent; the populated value keeps the caller's state; any other value is yellow
    private void RefreshDot(DotBinding b)
    {
      DotColors[b.Dot] = b.AtDb() ? null : b.AtPopulated() ? ColorFor(b.Initial) : EditedColor;
      b.Dot.Invalidate();
    }

    private static Color? ColorFor(FieldDot state) => state switch
    {
      FieldDot.Edited => EditedColor,
      FieldDot.Confirmed => ConfirmedColor,
      _ => null
    };

    private void WireDots()
    {
      foreach (var dot in new[] { ModulationDot, FramingDot, BaudDot, DeviationDot, AfCarrierDot,
        ManchesterDot, DifferentialDot, TelemetryFormatDot })
        dot.Paint += Dot_Paint;
    }

    private void Dot_Paint(object? sender, PaintEventArgs e)
    {
      var dot = (Panel)sender!;
      if (!DotColors.TryGetValue(dot, out var c) || c is not Color color) return;
      e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
      using var brush = new SolidBrush(color);
      e.Graphics.FillEllipse(brush, 1, 1, dot.Width - 3, dot.Height - 3);
    }

    // one shared right-click menu resets the field it was opened on back to its DB-derived value
    private void SetupResetMenu()
    {
      components ??= new Container();
      ResetMenu = new ContextMenuStrip(components);
      var reset = new ToolStripMenuItem("Reset to database value");
      reset.Click += ResetItem_Click;
      ResetMenu.Items.Add(reset);

      foreach (var ctl in new Control[] { ModulationCombo, FramingCombo, BaudTextBox, DeviationTextBox,
        AfCarrierTextBox, ManchesterCombo, DifferentialCombo, TelemetryFormatCombo })
        ctl.ContextMenuStrip = ResetMenu;
    }

    private void ResetItem_Click(object? sender, EventArgs e)
    {
      var b = Bindings.FirstOrDefault(x => x.Control == ResetMenu.SourceControl);
      b?.Reset();
    }


    //----------------------------------------------------------------------------------------------
    //                                    parameter discovery
    //----------------------------------------------------------------------------------------------
    // Discover searches for parameters that decode the transmitter, over the bursts arriving from here on
    // (discover_params_plan.md §4.1). The dialog owns none of that: the caller owns the session, because it
    // owns the live pipeline the bursts come from and the apply path the answer goes into. The dialog is the
    // button, the progress line and the save click.

    /// <summary>Raised when the operator presses Discover. The argument is the requested state — the button
    /// is a toggle, and a second press cancels the session (§4.6a).</summary>
    public event Action<bool>? DiscoverToggled;

    /// <summary>Raised when the operator presses Save to overrides: write the parameters currently shown
    /// into <c>transmitters-override.json</c> so the correction survives a restart (§6.1).</summary>
    public event EventHandler? SaveOverrideRequested;

    /// <summary>True while a discovery session is running, so the caller can end it if the dialog closes.</summary>
    public bool Discovering { get; private set; }

    /// <summary>True when a search produced a result and the fields it set still hold what it found. Those
    /// values decoded a frame — that is how the search found them — so the caller applies them as confirmed
    /// rather than as an edit waiting for proof (§4.5).</summary>
    public bool DiscoveredApplied => HasDiscovered
      && Bindings.Where(b => DiscoveredFields.Contains(b.Name)).All(b => b.AtPopulated());

    // a search has produced a result while this dialog was open
    private bool HasDiscovered;

    // set when the search is stopped from outside with a message of its own — the operator's second press,
    // or LOS — so the session's Ended notification does not overwrite that with "no parameters found":
    // cancelling is not the same as failing (§4.6a).
    private bool StoppedExplicitly;

    // the fields a discovery result replaces (§6.5): the ones whose dots it re-baselines to green
    private static readonly string[] DiscoveredFields = { "Modulation", "Framing", "Baud", "Deviation" };

    private void DiscoverBtn_Click(object sender, EventArgs e)
    {
      Discovering = !Discovering;
      DiscoverBtn.Text = Discovering ? "Stop" : "Discover";
      if (Discovering) { StoppedExplicitly = false; ShowDiscoveryProgress(0, 0, false); }
      else ShowDiscoveryStopped("search stopped");
      DiscoverToggled?.Invoke(Discovering);
    }

    /// <summary>The search was ended from outside rather than by finding an answer — the operator's second
    /// press, or the end of the pass. The button returns to its idle state and the line says why, instead of
    /// sitting on "waiting" over a search that is over (§4.6a).</summary>
    public void ShowDiscoveryStopped(string reason)
    {
      if (InvokeRequired) { BeginInvoke(() => ShowDiscoveryStopped(reason)); return; }

      Discovering = false;
      DiscoverBtn.Text = "Discover";
      StoppedExplicitly = true;
      SetStatus(reason, SystemColors.ControlText);
    }

    /// <summary>Progress line (§7 P2/P3). The search spends most of a pass waiting for the next burst, so
    /// the line says which of the two states it is in: waiting, with the bursts analyzed and skipped so
    /// far, or analyzing the burst that just arrived. The skipped count is shown because it is the signal
    /// that retaining bursts would pay off.</summary>
    public void ShowDiscoveryProgress(int analyzed, int skipped, bool analyzing)
    {
      SetStatus(analyzing ? "analyzing" : $"waiting: {analyzed} analyzed, {skipped} skipped",
        SystemColors.ControlText);
    }

    /// <summary>
    /// A hypothesis decoded: put the parameters found into the fields and say so. The caller has already
    /// applied them to the live pass — this is the report, not the decision (§4.5). The search is over, so
    /// the button returns to its idle state. The fields themselves show what was found, so the line says
    /// only that the search succeeded; the frames that follow are what the save decision rests on.
    /// </summary>
    public void ShowDiscovered(SignalParams found)
    {
      if (InvokeRequired) { BeginInvoke(() => ShowDiscovered(found)); return; }

      Discovering = false;
      DiscoverBtn.Text = "Discover";
      Original = found;
      ModulationCombo.SelectedItem = found.Modulation;
      FramingCombo.SelectedItem = found.Framing;
      BaudTextBox.Text = FormatRate(found.ResolvedBaud ?? found.Baud);
      DeviationTextBox.Text = FormatRate(found.ResolvedDeviation ?? found.Deviation);
      AfCarrierTextBox.Text = FormatNullable(found.AfCarrier);
      MarkDiscovered();
      SaveOverrideBtn.Enabled = true;
      SetStatus("signal parameters found", ConfirmedColor);
    }

    // The discovered values are the new baseline for the fields the search sets, and they are confirmed
    // rather than pending: a hypothesis only becomes a result by decoding a frame, so the fields it moved
    // away from the DB get a green dot, not the yellow one an untested edit gets. Fields the search landed
    // back on the DB value keep no dot at all — there is nothing there to override.
    private void MarkDiscovered()
    {
      HasDiscovered = true;
      foreach (var b in Bindings.Where(b => DiscoveredFields.Contains(b.Name)))
      {
        b.Populated = b.Value();
        b.Initial = FieldDot.Confirmed;
        RefreshDot(b);
      }
    }

    /// <summary>The evidence the operator needs for the save decision: frames decoded <b>since</b> the
    /// parameters were applied (§6.1). No further frames means the answer was probably a coincidence — do
    /// nothing, and the pass takes it away.</summary>
    public void ShowFramesSinceApply(int frames)
      => SetStatus($"{frames} frame(s) decoded with these parameters", frames > 0 ? ConfirmedColor : EditedColor);

    /// <summary>The session ended with no answer. A legitimate outcome: it says the problem is not the
    /// parameters (§4.5).</summary>
    public void ShowDiscoveryEnded(bool found)
    {
      Discovering = false;
      DiscoverBtn.Text = "Discover";
      if (StoppedExplicitly) { StoppedExplicitly = false; return; }
      if (!found) SetStatus("no parameters found", EditedColor);
    }

    // Progress arrives on the discovery worker thread. The session is always stopped before the dialog is
    // released (TelemetryPanel disposes it in a finally), so this cannot normally race a close — but a
    // status line is not worth a crash if that ordering is ever changed, so it fails silently instead.
    private void SetStatus(string text, Color color)
    {
      if (IsDisposed || DiscoverStatusLabel.IsDisposed) return;
      if (DiscoverStatusLabel.IsHandleCreated && DiscoverStatusLabel.InvokeRequired)
      {
        try { DiscoverStatusLabel.BeginInvoke(() => SetStatus(text, color)); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }   // handle destroyed between the check and the call
        return;
      }
      DiscoverStatusLabel.ForeColor = color;
      DiscoverStatusLabel.Text = text;
    }

    private void SaveOverrideBtn_Click(object sender, EventArgs e) => SaveOverrideRequested?.Invoke(this, e);


    /// <summary>The parameters the controls currently hold, as an applicable <see cref="SignalParams"/>.
    /// Public because Save to overrides writes what is on screen without closing the dialog (§6.1).</summary>
    public SignalParams CurrentParams()
    {
      var edited = Original with
      {
        Baud = ParseNullable(BaudTextBox.Text) ?? Original.Baud,
        Modulation = (Modulation)ModulationCombo.SelectedItem,
        Framing = (Framing)FramingCombo.SelectedItem,
        Deviation = ParseNullable(DeviationTextBox.Text),
        Manchester = IndexToBool(ManchesterCombo.SelectedIndex),
        Differential = IndexToBool(DifferentialCombo.SelectedIndex),
        AfCarrier = ParseNullable(AfCarrierTextBox.Text)
      };
      // drop the previous run-time findings so the rebuilt pipeline re-discovers them against the new manual
      // params instead of displaying stale values on the next open.
      edited.ResolvedDeviation = null;
      edited.ResolvedBaud = null;
      return edited;
    }

    private void OkBtn_Click(object sender, EventArgs e)
    {
      Result = CurrentParams();

      ResultFormatId = TelemetryFormatCombo.SelectedIndex >= 0 ? (string)TelemetryFormatCombo.SelectedItem : null;

      // classify every field that moved since the dialog opened: back to the DB value (reset) or to a new
      // value (changed), whether the operator typed it or the search found it. Fields left as they opened
      // are reported as neither.
      ChangedFields.Clear();
      ResetFields.Clear();
      foreach (var b in Bindings)
      {
        if (b.AtOpened()) continue;
        if (b.AtDb()) ResetFields.Add(b.Name);
        else ChangedFields.Add(b.Name);
      }

      DialogResult = DialogResult.OK;
      Close();
    }


    //----------------------------------------------------------------------------------------------
    //                                         helpers
    //----------------------------------------------------------------------------------------------
    private static int BoolToIndex(bool? value) => value switch { true => 1, false => 2, null => 0 };

    private static bool? IndexToBool(int index) => index switch { 1 => true, 2 => false, _ => (bool?)null };

    private static int FormatIndex(IReadOnlyList<string> ids, string? id)
    {
      if (id == null) return -1;
      for (int i = 0; i < ids.Count; i++) if (ids[i] == id) return i;
      return -1;
    }

    private static string FormatNumber(double value) => value.ToString("0.###", CultureInfo.CurrentCulture);

    private static string FormatNullable(double? value) => value is double v ? FormatNumber(v) : "";

    // baud and deviation are shown as the round value they approximate: the pipeline measures 9600.832 and
    // the operator reads 9600, which is also what Save to overrides then writes to the file.
    private static string FormatRate(double value) => FormatNumber(SignalParamsResolver.RoundToStandard(value));

    private static string FormatRate(double? value) => FormatNullable(SignalParamsResolver.RoundToStandard(value));

    private static double? ParseNullable(string text)
    {
      if (string.IsNullOrWhiteSpace(text)) return null;
      return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double v) ? v : null;
    }
  }


  // the data the caller hands the dialog: the params to display and their DB-derived counterparts (for reset),
  // the available telemetry-format ids with the current and DB-derived selection, and the per-field dot state.
  public sealed class SignalParamsView
  {
    public SignalParams Params = null!;
    public SignalParams DbParams = null!;
    public IReadOnlyList<string> FormatIds = Array.Empty<string>();
    public string? FormatId;
    public string? DbFormatId;
    public SignalParamsDialog.FieldDot ModulationDot;
    public SignalParamsDialog.FieldDot FramingDot;
    public SignalParamsDialog.FieldDot BaudDot;
    public SignalParamsDialog.FieldDot DeviationDot;
    public SignalParamsDialog.FieldDot AfCarrierDot;
    public SignalParamsDialog.FieldDot ManchesterDot;
    public SignalParamsDialog.FieldDot DifferentialDot;
    public SignalParamsDialog.FieldDot FormatDot;
  }
}

using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
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

    // one field's dot: its panel and control, the initial (caller-computed) state, predicates that report whether
    // the control currently holds the DB-derived value or the value it was populated with, and the reset action
    private sealed class DotBinding
    {
      internal string Name = "";
      internal Panel Dot = null!;
      internal Control Control = null!;
      internal FieldDot Initial;
      internal Func<bool> AtDb = () => false;
      internal Func<bool> AtPopulated = () => true;
      internal Action Reset = () => { };
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
      BaudTextBox.Text = FormatNumber(Original.ResolvedBaud ?? Original.Baud);
      DeviationTextBox.Text = FormatNullable(Original.ResolvedDeviation ?? Original.Deviation);
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

      var mod0 = ModulationCombo.SelectedItem;
      var framing0 = FramingCombo.SelectedItem;
      var baud0 = BaudTextBox.Text;
      var dev0 = DeviationTextBox.Text;
      var af0 = AfCarrierTextBox.Text;
      var man0 = ManchesterCombo.SelectedIndex;
      var diff0 = DifferentialCombo.SelectedIndex;
      var fmt0 = TelemetryFormatCombo.SelectedIndex;

      object dbMod = db.Modulation;
      object dbFraming = db.Framing;
      string dbBaud = FormatNumber(db.ResolvedBaud ?? db.Baud);
      string dbDev = FormatNullable(db.ResolvedDeviation ?? db.Deviation);
      string dbAf = FormatNullable(db.AfCarrier);
      int dbMan = BoolToIndex(db.Manchester);
      int dbDiff = BoolToIndex(db.Differential);
      int dbFmt = FormatIndex(view.FormatIds, view.DbFormatId);

      Bind("Modulation", ModulationDot, view.ModulationDot, ModulationCombo,
        () => Equals(ModulationCombo.SelectedItem, dbMod),
        () => Equals(ModulationCombo.SelectedItem, mod0),
        () => ModulationCombo.SelectedItem = dbMod);

      Bind("Framing", FramingDot, view.FramingDot, FramingCombo,
        () => Equals(FramingCombo.SelectedItem, dbFraming),
        () => Equals(FramingCombo.SelectedItem, framing0),
        () => FramingCombo.SelectedItem = dbFraming);

      Bind("Baud", BaudDot, view.BaudDot, BaudTextBox,
        () => BaudTextBox.Text == dbBaud,
        () => BaudTextBox.Text == baud0,
        () => BaudTextBox.Text = dbBaud);

      Bind("Deviation", DeviationDot, view.DeviationDot, DeviationTextBox,
        () => DeviationTextBox.Text == dbDev,
        () => DeviationTextBox.Text == dev0,
        () => DeviationTextBox.Text = dbDev);

      Bind("AfCarrier", AfCarrierDot, view.AfCarrierDot, AfCarrierTextBox,
        () => AfCarrierTextBox.Text == dbAf,
        () => AfCarrierTextBox.Text == af0,
        () => AfCarrierTextBox.Text = dbAf);

      Bind("Manchester", ManchesterDot, view.ManchesterDot, ManchesterCombo,
        () => ManchesterCombo.SelectedIndex == dbMan,
        () => ManchesterCombo.SelectedIndex == man0,
        () => ManchesterCombo.SelectedIndex = dbMan);

      Bind("Differential", DifferentialDot, view.DifferentialDot, DifferentialCombo,
        () => DifferentialCombo.SelectedIndex == dbDiff,
        () => DifferentialCombo.SelectedIndex == diff0,
        () => DifferentialCombo.SelectedIndex = dbDiff);

      Bind("TelemetryFormat", TelemetryFormatDot, view.FormatDot, TelemetryFormatCombo,
        () => TelemetryFormatCombo.SelectedIndex == dbFmt,
        () => TelemetryFormatCombo.SelectedIndex == fmt0,
        () => TelemetryFormatCombo.SelectedIndex = dbFmt);
    }

    private void Bind(string name, Panel dot, FieldDot initial, Control control,
      Func<bool> atDb, Func<bool> atPopulated, Action reset)
    {
      var b = new DotBinding
      {
        Name = name, Dot = dot, Control = control, Initial = initial,
        AtDb = atDb, AtPopulated = atPopulated, Reset = reset
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

    private void OkBtn_Click(object sender, EventArgs e)
    {
      var mod = (Modulation)ModulationCombo.SelectedItem;
      var framing = (Framing)FramingCombo.SelectedItem;
      double baud = ParseNullable(BaudTextBox.Text) ?? Original.Baud;
      double? deviation = ParseNullable(DeviationTextBox.Text);
      double? afCarrier = ParseNullable(AfCarrierTextBox.Text);

      Result = Original with
      {
        Baud = baud,
        Modulation = mod,
        Framing = framing,
        Deviation = deviation,
        Manchester = IndexToBool(ManchesterCombo.SelectedIndex),
        Differential = IndexToBool(DifferentialCombo.SelectedIndex),
        AfCarrier = afCarrier
      };
      // drop the previous run-time findings so the rebuilt pipeline re-discovers them against the new manual
      // params instead of displaying stale values on the next open.
      Result.ResolvedDeviation = null;
      Result.ResolvedBaud = null;

      ResultFormatId = TelemetryFormatCombo.SelectedIndex >= 0 ? (string)TelemetryFormatCombo.SelectedItem : null;

      // classify every field the user moved relative to the value in use: back to the DB value (reset) or to a
      // new manual value (changed). Fields left at the populated value are reported as neither.
      ChangedFields.Clear();
      ResetFields.Clear();
      foreach (var b in Bindings)
      {
        if (b.AtPopulated()) continue;
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

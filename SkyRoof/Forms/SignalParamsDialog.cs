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

    // dot colors, shared with the caller so the gear button uses the same yellow/green. The green is
    // themed, because the status line paints text with it and LimeGreen is unreadable on the light surface.
    public static readonly Color EditedColor = Color.Orange;
    public static Color ConfirmedColor => Theme.ParamsConfirmed;

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
      // what was last handed to the caller and applied to the live pipeline. Every edit is applied as it is
      // committed, so this is what a commit is tested against: tabbing out of a field nobody touched, or a
      // second commit of the same value, must not tear the pipeline down and restart the countdown.
      internal object? Applied;
      internal Action Reset = () => { };
      // undo the control's change subscription: a Repopulate rebuilds the bindings, and a handler left
      // behind would go on re-deriving the dot of a binding that no longer describes anything (§4.5).
      internal Action Detach = () => { };

      internal bool AtOpened() => Equals(Value(), Opened);
      internal bool AtPopulated() => Equals(Value(), Populated);
      internal bool AtApplied() => Equals(Value(), Applied);
    }

    private readonly List<DotBinding> Bindings = new();

    // current fill color per dot panel; null = transparent (nothing drawn, blends into the form)
    private readonly Dictionary<Panel, Color?> DotColors = new();

    // shared right-click menu with a single "reset to database value" command, wired to every value control
    private ContextMenuStrip ResetMenu = null!;

    // control the dialog is centered over (the Telemetry panel); null falls back to the designer's CenterParent
    private Control? Anchor;

    private SignalParams Original;

    // the format ids the combo was filled from, so the controls can be restored without the view
    private IReadOnlyList<string> FormatIds = Array.Empty<string>();

    // The state Cancel goes back to: what the dialog opened with, moved forward to the values on screen
    // every time Save writes them down. Editing is applied to the live pipeline as it happens, so Cancel
    // has no unsent result to discard - it re-applies this one instead. A Cancel that follows a Save with
    // no further edit therefore does nothing: saved parameters are the recorded truth about the
    // transmitter, and undoing them is what Reset to database value is for.
    private SignalParams RevertParams = null!;
    private string? RevertFormatId;

    // Set while the dialog writes its own controls - populating them, restoring them, clearing them for a
    // search, filling in its answer. The commit handlers below must not mistake any of that for an operator
    // edit and apply it to the live pipeline.
    private bool Suppressed;

    // what the fields held when a search started, so a search that ends without an answer gives them back
    private SignalParams? SearchRestore;
    private string? SearchRestoreFormatId;

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
      // wired here rather than in the designer: the button keeps its DialogResult.Cancel (which closes the
      // form), and this handler runs first to put the parameters back before it does
      CancelBtn.Click += CancelBtn_Click;
    }

    public DialogResult Open(SignalParamsView view, Control? anchor = null)
    {
      Anchor = anchor;
      if (anchor != null) StartPosition = FormStartPosition.Manual;

      Original = view.Params;
      Result = view.Params;
      ResultFormatId = view.FormatId;
      FormatIds = view.FormatIds;
      RevertParams = view.Params;
      RevertFormatId = view.FormatId;

      ModulationCombo.DataSource = Enum.GetValues(typeof(Modulation));
      FramingCombo.DataSource = Enum.GetValues(typeof(Framing));
      ManchesterCombo.DataSource = (string[])TriStateItems.Clone();
      DifferentialCombo.DataSource = (string[])TriStateItems.Clone();
      TelemetryFormatCombo.DataSource = new List<string>(view.FormatIds);

      Quiet(() => PopulateControls(view));
      BuildBindings(view);
      // Save writes a parameter set down as the truth about this transmitter, so it stays disabled until the
      // panel reports the set proven — the frames are counted there, which is why a reopened dialog comes
      // back with the Save, or the countdown, the pass left it at rather than greyed out again (§3, §4.3).
      Proven = view.CanSave;
      Savable = view.Savable;
      Saved = view.Saved;
      UpdateSaveEnabled();
      if (view.Status != null) { CountdownText = view.Status; RefreshStatusLine(); }
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
      FormatIds = view.FormatIds;
      RestoreControls(Original, view.FormatId);
    }

    // Write a parameter set into the controls. Every caller is the dialog writing its own controls - the
    // initial populate, a Cancel putting the parameters back, a search restoring what it cleared - so all
    // of them run it under Quiet: none of it is an operator edit and none of it may be applied as one.
    private void RestoreControls(SignalParams p, string? formatId)
    {
      ModulationCombo.SelectedItem = p.Modulation;
      FramingCombo.SelectedItem = p.Framing;
      BaudTextBox.Text = FormatRate(p.ResolvedBaud ?? p.Baud);
      DeviationTextBox.Text = FormatRate(p.ResolvedDeviation ?? p.Deviation);
      AfCarrierTextBox.Text = FormatNullable(p.AfCarrier);
      ManchesterCombo.SelectedIndex = BoolToIndex(p.Manchester);
      DifferentialCombo.SelectedIndex = BoolToIndex(p.Differential);
      // the NORAD-resolved format is pre-selected, or the combo is left empty when no format matches
      TelemetryFormatCombo.SelectedIndex = FormatIndex(FormatIds, formatId);
    }

    // run a block of the dialog's own writes to the controls without any of it reaching the live pipeline
    private void Quiet(Action write)
    {
      bool was = Suppressed;
      Suppressed = true;
      try { write(); }
      finally { Suppressed = was; }
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
      b.Applied = b.Value();
      DotColors[dot] = ColorFor(initial);

      // the dot follows every keystroke; the pipeline follows a committed edit. For a combo the two are the
      // same moment, for a text box the commit is Enter or the focus leaving it (§4.3).
      void Handler(object? sender, EventArgs e) { RefreshDot(b); UpdateSaveEnabled(); RefreshStatusLine(); }
      void Commit(object? sender, EventArgs e) => ApplyIfEdited();
      void CommitOnEnter(object? sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) ApplyIfEdited(); }
      if (control is ComboBox cb)
      {
        cb.SelectedIndexChanged += Handler;
        cb.SelectedIndexChanged += Commit;
        b.Detach = () => { cb.SelectedIndexChanged -= Handler; cb.SelectedIndexChanged -= Commit; };
      }
      else if (control is TextBox tb)
      {
        tb.TextChanged += Handler;
        tb.Leave += Commit;
        tb.KeyDown += CommitOnEnter;
        b.Detach = () => { tb.TextChanged -= Handler; tb.Leave -= Commit; tb.KeyDown -= CommitOnEnter; };
      }
    }


    //----------------------------------------------------------------------------------------------
    //                                        applying edits
    //----------------------------------------------------------------------------------------------
    /// <summary>Raised when an edit is committed - a combo selection, Enter in a text box, or the focus
    /// leaving one - and when Cancel puts the parameters back. The caller applies what the dialog holds
    /// immediately: this is a live control panel over a running decoder, not a form that decides on OK.
    /// <see cref="Result"/>, <see cref="ResultFormatId"/>, <see cref="ChangedFields"/> and
    /// <see cref="ResetFields"/> are up to date by the time it fires.</summary>
    public event EventHandler? ParamsEdited;

    // Apply what is on screen, if it has moved since the last time it was applied. The test matters: a
    // Leave fires whenever the focus passes through a text box, and applying an unchanged set would tear
    // the decoder down and restart the countdown for nothing.
    private void ApplyIfEdited()
    {
      if (Suppressed || IsDisposed) return;
      if (Bindings.All(b => b.AtApplied())) return;

      CaptureResult();
      foreach (var b in Bindings) b.Applied = b.Value();
      ParamsEdited?.Invoke(this, EventArgs.Empty);
    }

    // Classify every field that moved since the dialog opened: back to the DB value (reset) or to a new
    // value (changed), whether the operator typed it or the search found it. Fields left as they opened are
    // reported as neither. Measured against the opening values rather than the last applied ones, because
    // the caller accumulates the set of overridden fields and a field is overridden until it goes back.
    private void CaptureResult()
    {
      Result = CurrentParams();
      ResultFormatId = CurrentFormatId();
      ChangedFields.Clear();
      ResetFields.Clear();
      foreach (var b in Bindings)
      {
        if (b.AtOpened()) continue;
        if (b.AtDb()) ResetFields.Add(b.Name);
        else ChangedFields.Add(b.Name);
      }
    }

    /// <summary>Re-baseline the dots, the save gate and the countdown from the caller, after it has applied
    /// what the dialog sent it or after a frame has changed its verdict. All of that state lives in the
    /// panel (§3), so the panel states it rather than the dialog inferring it. The bindings are not rebuilt:
    /// the operator may be typing in one of these controls.</summary>
    public void Refresh(SignalParamsView view)
    {
      if (IsDisposed) return;
      Original = view.Params;
      foreach (var b in Bindings)
      {
        b.Initial = DotIn(view, b.Name);
        b.Populated = b.Value();
        b.Applied = b.Value();
        RefreshDot(b);
      }
      Proven = view.CanSave;
      Savable = view.Savable;
      Saved = view.Saved;
      UpdateSaveEnabled();
      CountdownText = view.Status;
      RefreshStatusLine();
    }

    private static FieldDot DotIn(SignalParamsView v, string name) => name switch
    {
      "Modulation" => v.ModulationDot,
      "Framing" => v.FramingDot,
      "Baud" => v.BaudDot,
      "Deviation" => v.DeviationDot,
      "AfCarrier" => v.AfCarrierDot,
      "Manchester" => v.ManchesterDot,
      "Differential" => v.DifferentialDot,
      "TelemetryFormat" => v.FormatDot,
      _ => FieldDot.None
    };

    /// <summary>The parameters on screen have been written to the override file, so they become what Cancel
    /// goes back to. A saved set is the recorded truth about this transmitter; Cancel does not undo it.</summary>
    public void MarkSaved()
    {
      RevertParams = CurrentParams();
      RevertFormatId = CurrentFormatId();
      Saved = true;
    }

    private void CancelBtn_Click(object? sender, EventArgs e)
    {
      // every edit went to the pipeline as it was committed, so there is nothing here to discard - Cancel
      // puts back the last set that was written down, which is what the dialog opened with unless a Save
      // has moved that baseline forward since. Unchanged since then, this applies nothing at all.
      Quiet(() => RestoreControls(RevertParams, RevertFormatId));
      ApplyIfEdited();
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

    // the save gate's first half (§4.3): the panel's verdict that enough frames have decoded with the
    // parameters in use. The second half is AtPopulated below — the fields must still hold the values those
    // frames were decoded with — and it is re-tested on every field change, so editing a proven field greys
    // Save out again: the proof was for the other values.
    private bool Proven;

    // Whether there is anywhere to save the parameters at all: false for a terrestrial signal, which has no
    // transmitter row for transmitters-override.json to be keyed by (§4.9). It gates the button ahead of
    // both halves of the gate — Ctrl+Shift included, since a forced save would otherwise reach a handler
    // that can only return — and it replaces the countdown, which would otherwise count toward a button
    // that never enables however many frames decode.
    private bool Savable = true;

    // Whether the set on screen is the one currently written in the override file. The countdown in front of
    // Save is a countdown to a decision; once the decision is taken the line has to stop asking for it. Any
    // further edit unendorses the set and clears this again, which the caller's own OverrideSaved does too.
    private bool Saved;

    // the save gate's second half (§4.3), shared with the status line so the two cannot disagree. The
    // telemetry format is excluded: it is not part of what Save writes to transmitters-override.json,
    // so choosing a different one cannot invalidate the demod parameters' proof.
    private bool FieldsHoldProvenValues => Bindings.All(b => b.Name == "TelemetryFormat" || b.AtPopulated());

    // ... and not while a search is running: the fields are cleared for it, so Save would write down a set
    // the operator cannot see, assembled half from blanks and half from what the search has yet to replace.
    private void UpdateSaveEnabled()
      => SaveOverrideBtn.Enabled = Savable && !Discovering && (ForceSave || (Proven && FieldsHoldProvenValues));

    // The countdown line while it is the thing on the status line, else null. Kept so that an edit can take
    // the line away from a countdown that has stopped describing what is on screen, and give it back
    // unchanged when the field returns to the value it was counting for.
    private string? CountdownText;

    // The countdown counts frames decoded with the parameters IN USE, and a field edited since is not among
    // them: the line would otherwise go on offering to save values the operator can no longer see, next to
    // an orange dot and a greyed-out Save button that both say the opposite (§4.3).
    private void RefreshStatusLine()
    {
      if (CountdownText == null) return;
      if (!FieldsHoldProvenValues) SetStatus("edited value not tested — click OK to apply it", EditedColor);
      else SetStatus(CountdownText, ConfirmedColor);
    }

    // Ctrl+Shift held down enables Save whatever the gate says, for as long as the keys are down (§4.11).
    // The gate weighs the evidence this program can see; an operator reading the parameters off the
    // satellite's own documentation has evidence it cannot, and a two-handed gesture is a deliberate enough
    // way to say so — it cannot be arrived at by clicking, and it is gone the moment the keys are released.
    private bool ForceSave;

    protected override void OnKeyDown(KeyEventArgs e)
    {
      base.OnKeyDown(e);
      UpdateForceSave();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
      base.OnKeyUp(e);
      UpdateForceSave();
    }

    // the keys can be pressed or released while another window has the focus, so their state is re-read
    // rather than remembered whenever this one gains or loses it
    protected override void OnActivated(EventArgs e)
    {
      base.OnActivated(e);
      UpdateForceSave();
    }

    protected override void OnDeactivate(EventArgs e)
    {
      base.OnDeactivate(e);
      UpdateForceSave();
    }

    private void UpdateForceSave()
    {
      bool forced = ModifierKeys == (Keys.Control | Keys.Shift);
      if (forced == ForceSave) return;
      ForceSave = forced;
      UpdateSaveEnabled();
    }

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
      if (Discovering) { StoppedExplicitly = false; BeginSearchDisplay(); ShowDiscoveryProgress(0, 0, false); }
      else ShowDiscoveryStopped("search stopped");
      DiscoverToggled?.Invoke(Discovering);
    }

    // The search answers for the parameters, so while it runs the dialog stops showing values it is about to
    // replace: the four fields the search determines are cleared and every control is locked, and the answer
    // fills them in. Nothing here is applied - the pipeline goes on decoding with the parameters it has,
    // which is exactly what the search is listening to. AF carrier, Manchester and Precoding keep their
    // values because the search neither reads nor sets them, and blanking a value still in use would
    // misreport what is decoding; they are locked all the same, since an edit mid-search would be applied
    // under a display that is not describing the pipeline.
    private void BeginSearchDisplay()
    {
      SearchRestore = CurrentParams();
      SearchRestoreFormatId = CurrentFormatId();

      Quiet(() =>
      {
        ModulationCombo.SelectedItem = Modulation.Unknown;
        FramingCombo.SelectedItem = Framing.Unknown;
        BaudTextBox.Text = "";
        DeviationTextBox.Text = "";
      });

      foreach (var b in Bindings)
      {
        b.Control.Enabled = false;
        // no dot while the search runs: an empty field is not an override, it is a question
        if (DiscoveredFields.Contains(b.Name)) { DotColors[b.Dot] = null; b.Dot.Invalidate(); }
      }
      UpdateSaveEnabled();
    }

    // The search is over. An answer leaves the fields it filled in; anything else gives back what was on
    // screen when it started, because a search that found nothing must not cost the operator the parameters
    // it was searching from.
    private void EndSearchDisplay(bool restore)
    {
      if (SearchRestore != null && restore)
        Quiet(() => RestoreControls(SearchRestore, SearchRestoreFormatId));
      SearchRestore = null;

      foreach (var b in Bindings)
      {
        b.Control.Enabled = true;
        RefreshDot(b);
      }
      UpdateSaveEnabled();
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
      EndSearchDisplay(true);
      CountdownText = null;
      SetStatus(reason, SystemColors.ControlText);
    }

    /// <summary>Progress line (§7 P2/P3). The search spends most of a pass waiting for the next burst, so
    /// the line says which of the two states it is in: waiting, with the bursts analyzed and skipped so
    /// far, or analyzing the burst that just arrived. The skipped count is shown because it is the signal
    /// that retaining bursts would pay off.</summary>
    public void ShowDiscoveryProgress(int analyzed, int skipped, bool analyzing)
    {
      CountdownText = null;
      SetStatus(analyzing ? "analyzing" : $"waiting: {analyzed} analyzed, {skipped} skipped",
        SystemColors.ControlText);
    }

    /// <summary>
    /// A hypothesis decoded: put the parameters found into the fields and say so. The caller has already
    /// applied them to the live pass — this is the report, not the decision (§4.5). The search is over, so
    /// the button returns to its idle state. The fields themselves show what was found, so the line says
    /// only how many more frames the save decision still needs.
    /// </summary>
    public void ShowDiscovered(SignalParams found, int need)
    {
      if (InvokeRequired) { BeginInvoke(() => ShowDiscovered(found, need)); return; }

      Discovering = false;
      DiscoverBtn.Text = "Discover";
      Original = found;
      Quiet(() =>
      {
        ModulationCombo.SelectedItem = found.Modulation;
        FramingCombo.SelectedItem = found.Framing;
        BaudTextBox.Text = FormatRate(found.ResolvedBaud ?? found.Baud);
        DeviationTextBox.Text = FormatRate(found.ResolvedDeviation ?? found.Deviation);
        AfCarrierTextBox.Text = FormatNullable(found.AfCarrier);
      });
      // the answer stays on screen: this is the populate the search was cleared for
      EndSearchDisplay(false);
      MarkDiscovered();
      // the caller has already applied these, so they are the applied baseline too - a later Leave on one of
      // these controls must not re-apply what is already running
      foreach (var b in Bindings) b.Applied = b.Value();
      // green dots at once, Save held back: one frame answers "are these the parameters in use, and did they
      // work?", which is what the dot means, but not "should this be written down as the truth about this
      // transmitter?" (§2). The countdown makes the difference visible instead of mysterious.
      ShowConfirmingFrames(0, need);
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

    /// <summary>The evidence the operator needs for the save decision: the frames decoded <b>after</b> the
    /// one that turned the dots green (§2). Green throughout — the parameters are working, the button is
    /// simply not unlocked yet.</summary>
    public void ShowConfirmingFrames(int have, int need)
    {
      if (InvokeRequired) { BeginInvoke(() => ShowConfirmingFrames(have, need)); return; }

      Proven = have >= need;
      UpdateSaveEnabled();
      CountdownText = ConfirmingFramesText(have, need, Savable, Saved);
      RefreshStatusLine();
    }

    /// <summary>The countdown's wording, shared with the panel so that the line a reopened dialog comes back
    /// with is the one the open dialog was showing.</summary>
    public static string ConfirmingFramesText(int have, int need, bool savable, bool saved)
    {
      // nothing to count toward: the frames are decoding and the parameters are confirmed, but a signal
      // with no transmitter behind it has no override row to be written to (§4.9)
      if (!savable) return "parameters confirmed — terrestrial, nothing to save";

      // already written down: the count is still the evidence, but it is no longer a countdown to anything.
      // A forced save (§4.11) can land before the evidence is in, and then there is no count to report.
      if (saved)
        return have >= need ? $"{have + 1} frames decoded — parameters have been saved"
                            : "parameters have been saved";

      int left = need - have;
      if (left <= 0) return $"{have + 1} frames decoded — parameters can be saved";
      return left == 1 ? "1 more frame to save" : $"{left} more frames to save";
    }

    /// <summary>The session ended with no answer. A legitimate outcome: it says the problem is not the
    /// parameters (§4.5).</summary>
    public void ShowDiscoveryEnded(bool found)
    {
      Discovering = false;
      DiscoverBtn.Text = "Discover";
      if (StoppedExplicitly) { StoppedExplicitly = false; return; }
      if (!found)
      {
        EndSearchDisplay(true);
        CountdownText = null;
        SetStatus("no parameters found", EditedColor);
      }
    }

    /// <summary>The answer was another transmitter's own parameters and the caller has selected that
    /// transmitter instead, so this dialog is describing one that is no longer the selection. Rebuild it
    /// around the new transmitter's parameters and say what happened (§4.5). Nothing here is discovered and
    /// nothing is proven: the database was already right about the transmitter now on screen.</summary>
    public void Repopulate(SignalParamsView view, string status)
    {
      if (InvokeRequired) { BeginInvoke(() => Repopulate(view, status)); return; }

      // detach first: the controls are about to be assigned the new transmitter's values, and a handler
      // still on a binding of the old one would re-derive its dot from them.
      foreach (var b in Bindings) b.Detach();
      Bindings.Clear();
      DotColors.Clear();

      Original = view.Params;
      Result = view.Params;
      ResultFormatId = view.FormatId;
      // the result belonged to the other transmitter: there is nothing here for OK to apply, and no
      // discovered field to re-baseline
      HasDiscovered = false;
      Discovering = false;
      StoppedExplicitly = false;
      DiscoverBtn.Text = "Discover";

      TelemetryFormatCombo.DataSource = new List<string>(view.FormatIds);
      // the co-channel switch is the caller writing the dialog, not an operator edit, and it ends whatever
      // the search had done to the fields - they now describe a different transmitter entirely
      SearchRestore = null;
      Quiet(() => PopulateControls(view));
      BuildBindings(view);
      foreach (var b in Bindings) b.Control.Enabled = true;
      // unlike the initial Open, the dots are already on screen with the previous transmitter's colors
      foreach (var b in Bindings) b.Dot.Invalidate();

      Proven = view.CanSave;
      Savable = view.Savable;
      Saved = view.Saved;
      RevertParams = view.Params;
      RevertFormatId = view.FormatId;
      UpdateSaveEnabled();
      CountdownText = null;
      SetStatus(status, ConfirmedColor);
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

    private string? CurrentFormatId()
      => TelemetryFormatCombo.SelectedIndex >= 0 ? (string)TelemetryFormatCombo.SelectedItem : null;


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
      // the caret may still be sitting in a text box whose edit has not been committed by a Leave yet
      ApplyIfEdited();
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
    // whether the panel has counted enough confirming frames for Save to be offered, and the countdown line
    // to reopen with (null = nothing to say; the dialog keeps its own status)
    public bool CanSave;
    // false when there is no transmitter to save against at all (a terrestrial signal), which disables Save
    // outright and replaces the countdown with the reason (§4.9)
    public bool Savable = true;
    // whether the set on screen is the one currently in the override file, so the line in front of Save
    // reports the decision as taken instead of going on asking for it
    public bool Saved;
    public string? Status;
  }
}

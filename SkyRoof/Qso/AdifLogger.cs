using VE3NEA;

namespace SkyRoof
{
  public class AdifLogger
  {
    private readonly NewFileEvery NewFileEvery;

    public HashSet<string?> UsStates = new()
    {
      "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
      "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
      "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
      "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
      "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
    };

    private readonly HashSet<string> WorkedCalls = new();
    private readonly HashSet<string> WorkedGrids = new();
    private readonly HashSet<string> WorkedStates = new();
    private readonly Dictionary<string, string> GridLookup = new();
    private readonly Dictionary<string, string> StateLookup = new();
    private readonly Dictionary<string, string> NameLookup = new();

    public AdifLogger(NewFileEvery every)
    {
      NewFileEvery = every;
      BuildWkdLists();
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                                save
    //--------------------------------------------------------------------------------------------------------------
    internal void SaveQso(QsoInfo qso)
    {
      string path = BuildAdifPath(qso.Utc);

      AdifFile adifFile = new();
      adifFile.Header = new() { ["PROGRAMID"] = Application.ProductName!, ["PROGRAMVERSION"] = Utils.GetVersionNumber() };
      adifFile.AppendToFile(path, qso.ToAdifEntry());
    }

    private string BuildAdifPath(DateTime utc)
    {
      string path = Path.Combine(Utils.GetUserDataFolder(), "Adif");
      Directory.CreateDirectory(path);

      switch (NewFileEvery)
      {
        case NewFileEvery.Day: return Path.Combine(path, $"{utc:yyyy-MM-dd}.adi");
        case NewFileEvery.Month: return Path.Combine(path, $"{utc:yyyy-MM}.adi");
        default: return Path.Combine(path, $"{utc:yyyy}.adi");
      }
    }





    //--------------------------------------------------------------------------------------------------------------
    //                                              augment
    //--------------------------------------------------------------------------------------------------------------
    internal QsoInfo Augment(QsoInfo qso)
    {
      // look up grid and state
      if (qso.Grid == "") qso.Grid = GridLookup.GetValueOrDefault(qso.Call) ?? "";
      if (qso.State == "") qso.State = StateLookup.GetValueOrDefault(qso.Call) ?? "";
      if (qso.Name == "") qso.Name = NameLookup.GetValueOrDefault(qso.Call) ?? "";
      return qso;
    }

    internal QsoInfo GetStatus(QsoInfo qso)
    {
      bool dupe = WorkedCalls.Contains(qso.Call);
      bool newState = IsValidState(qso.State) && !WorkedStates.Contains(qso.State);
      bool newGrid = IsValidGrid(qso.Grid) && !WorkedGrids.Contains(qso.Grid);

      if (newGrid) qso.BackColor = "#00FF00";
      else if (newState) qso.BackColor = "#88FF88";
      else if (dupe) qso.BackColor = "#CCCCCC";

      if (newState && newGrid) qso.StatusString = "New state and grid";
      else if (newState) qso.StatusString = "New state";
      else if (newGrid) qso.StatusString = "New grid";
      else if (dupe) qso.StatusString = "Duplicate";
      else if (!newGrid && !IsValidState(qso.State)) qso.StatusString = "Status unknown";
      else if (!newState && !IsValidGrid(qso.Grid)) qso.StatusString = "Status unknown";
      else qso.StatusString = "Not needed";

      return qso;
    }

    private bool IsValidGrid(string grid)
    {
      return Utils.GridSquare4Regex.IsMatch(grid);
    }

    private bool IsValidState(string state)
    {
      return UsStates.Contains(state);
    }



    //--------------------------------------------------------------------------------------------------------------
    //                                          build WKD lists
    //--------------------------------------------------------------------------------------------------------------

    private void BuildWkdLists()
    {
      AdifFile adifFile = new();
      adifFile.FieldsFilter = "CALL|GRIDSQUARE|NAME|STATE|PROP_MODE";

      string path = Path.Combine(Utils.GetUserDataFolder(), "Adif");
      Directory.CreateDirectory(path);
      foreach (var filePath in Directory.GetFiles(path, "*.adi"))
      {
        adifFile.LoadFromFile(filePath);
        foreach(var qso in adifFile.Qsos) ImportQso(qso);
      }
    }

    private void ImportQso(AdifEntry qso)
    {
      string call = qso["CALL"];
      bool isSat = qso.GetValueOrDefault("PROP_MODE") == "SAT";
      string? name = qso.GetValueOrDefault("NAME");

      string? grid = qso.GetValueOrDefault("GRIDSQUARE");
      grid = grid != null && qso["GRIDSQUARE"].Length >= 4 ? qso["GRIDSQUARE"].Substring(0, 4) : null;
      
      string? state = qso.GetValueOrDefault("STATE");
      if (!UsStates.Contains(state)) state = null;


      if (isSat)
        WorkedCalls.Add(qso["CALL"]);

      if (!string.IsNullOrEmpty(grid))
      {
        GridLookup[call] = grid;
        if (isSat) WorkedGrids.Add(grid);
      }

      if (!string.IsNullOrEmpty(state))
      {
        StateLookup[call] = state;
        if (isSat) WorkedStates.Add(qso["STATE"]);
      }

      if (!string.IsNullOrEmpty(name))
        NameLookup[call] = name;
    }
  }
}

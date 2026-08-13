using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SkyRoof.Properties;
using System.IO;
using VE3NEA;

namespace SkyRoof
{
  public enum CatAction
  {
    setup,
    read_rx_frequency,
    read_tx_frequency,
    write_rx_frequency,
    write_tx_frequency,
    read_rx_mode,
    read_tx_mode,
    write_rx_mode,
    write_tx_mode,
    read_ptt,
    write_ptt_on,
    write_ptt_off,
    write_ctcss_tone,
    enable_ctcss,
    disable_ctcss
  }

  public class RadioCapabilities
  {
    public int version { get; set; } = 0;
    public string model { get; set; }
    public bool cross_band_split { get; set; }
    public AvailableCommands? simplex { get; set; }
    public AvailableCommands? split { get; set; }
    public AvailableCommands? duplex { get; set; }

    public bool CanSplitTune(bool crossband)
    {
      if (split == null) return false;
      if (crossband && !cross_band_split) return false;
      if (!split.when_receiving.Contains(CatAction.write_rx_frequency)) return false;
      if (!split.when_transmitting.Contains(CatAction.write_tx_frequency)) return false;

      return true;
    }

    public static RadioCapabilities? LoadFromFile(string path)
    {
      try
      {
        if (!File.Exists(path)) return null;
        string json = File.ReadAllText(path);
        return LoadFromJson(json);
      }
      catch (Exception) { return null; }             
    }

    public static RadioCapabilities? LoadFromJson(string json)
    {
      return JsonConvert.DeserializeObject<RadioCapabilities>(json);
    }

    public static RadioCapabilities LoadDefaultCapabilities()
    {
        string path = Path.Combine(Utils.GetUserDataFolder(), "cat_info.json");
        
        // Get embedded resource version by directly deserializing
        string resourceJson = System.Text.Encoding.UTF8.GetString(Resources.cat_info);
        RadioCapabilities embeddedCaps = LoadFromJson(resourceJson)!;
        
        // Try to load from file
        RadioCapabilities? caps = null;
        try 
        { 
            caps = LoadFromFile(path);
        } 
        catch { }

        // If file doesn't exist, is corrupt, or has older/equal version than resource
        if (caps == null || caps.version <= embeddedCaps.version)
        {
            // Write resource to file
            File.WriteAllBytes(path, Resources.cat_info);
            
            // Use the already deserialized embedded caps
            return embeddedCaps;
        }

        return caps;
    }
  }

  public class AvailableCommands
  {
    [JsonConverter(typeof(TolerantCatActionArrayConverter))]
    public CatAction[] when_receiving { get; set; } = Array.Empty<CatAction>();

    [JsonConverter(typeof(TolerantCatActionArrayConverter))]
    public CatAction[] when_transmitting { get; set; } = Array.Empty<CatAction>();

    [JsonConverter(typeof(TolerantCatActionArrayConverter))]
    public CatAction[] when_setting_up { get; set; } = Array.Empty<CatAction>();

    internal bool Can(CatAction action, bool ptt)
    {
      return ptt ? when_transmitting.Contains(action) : when_receiving.Contains(action);
    }

    internal bool CanSetup(CatAction action)
    {
      return when_setting_up.Contains(action);
    }
  }




  // parses a JSON array of action names into CatAction[], silently dropping names this version does
  // not know. this keeps a newer SkyCAT (which may advertise commands added later) from breaking an
  // older SkyRoof, which the default StringEnumConverter would do by throwing on an unknown name.
  public class TolerantCatActionArrayConverter : JsonConverter<CatAction[]>
  {
    public override CatAction[] ReadJson(JsonReader reader, Type objectType, CatAction[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
      if (reader.TokenType == JsonToken.Null) return Array.Empty<CatAction>();
      if (reader.TokenType != JsonToken.StartArray)
        throw new JsonException($"Expected an array of CatAction names but found {reader.TokenType}.");

      var actions = new List<CatAction>();
      while (reader.Read())
      {
        if (reader.TokenType == JsonToken.EndArray) break;
        if (reader.TokenType == JsonToken.String
          && Enum.TryParse<CatAction>((string)reader.Value!, ignoreCase: true, out var action))
          actions.Add(action);
      }
      return actions.ToArray();
    }

    public override void WriteJson(JsonWriter writer, CatAction[]? value, JsonSerializer serializer)
    {
      writer.WriteStartArray();
      if (value != null)
        foreach (var action in value) writer.WriteValue(action.ToString());
      writer.WriteEndArray();
    }
  }
}

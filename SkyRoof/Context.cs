using MathNet.Numerics;
using VE3NEA;

namespace SkyRoof
{
  public class Context
  {
    // resources
    internal readonly Font AwesomeFont8 = FontAwesomeFactory.Create(8);
    internal readonly Font AwesomeFont14 = FontAwesomeFactory.Create(14);
    internal readonly PaletteManager PaletteManager = new PaletteManager();

    // data
    public Settings Settings = new();
    public SatnogsDb SatnogsDb;
    public GroupSatellitePasses GroupPasses;
    public HamSatellitePasses HamPasses;
    public SdrSatellitePasses SdrPasses;
    public AmsatStatusLoader AmsatStatusLoader = new();
    public LoggerInterface LoggerInterface;


    // main form
    public MainForm MainForm;
    public SatelliteSelectorWidget SatelliteSelector;
    public FrequencyWidget FrequencyControl;
    public RotatorWidget RotatorControl;

    // panels
    public GroupViewPanel? GroupViewPanel;
    public SatelliteDetailsPanel? SatelliteDetailsPanel;
    public PassesPanel? PassesPanel;
    public TimelinePanel? TimelinePanel;
    public SkyViewPanel? SkyViewPanel;
    public EarthViewPanel? EarthViewPanel;
    public TransmittersPanel? TransmittersPanel;
    public WaterfallPanel? WaterfallPanel;
    public QsoEntryPanel? QsoEntryPanel;
    public Ft4ConsolePanel? Ft4ConsolePanel;

    // devices
    public SoapySdrDevice? Sdr;
    public Slicer? Slicer;
    public CatControl CatControl = new();

    // soundcards
    public readonly OutputSoundcard<float> SpeakerSoundcard = new();
    public readonly OutputSoundcard<float> AudioVacSoundcard = new();
    public readonly OutputSoundcard<Complex32> IqVacSoundcard = new();
    public readonly Announcer Announcer = new();
    public UdpStreamSender UdpStreamSender = new();
    
    public void ClosePanels()
    {
      GroupViewPanel?.Close();
      SatelliteDetailsPanel?.Close();
      PassesPanel?.Close();
      TimelinePanel?.Close();
      SkyViewPanel?.Close();
      EarthViewPanel?.Close();
      TransmittersPanel?.Close();
      WaterfallPanel?.Close();
      QsoEntryPanel?.Close();
      Ft4ConsolePanel?.Close();
    }
  }
}

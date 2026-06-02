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
    public MonitoredSatellitePasses MonitoredPasses;
    public AmsatStatusLoader AmsatStatusLoader = new();
    public LoggerInterface LoggerInterface;

    // main form
    public MainForm MainForm;
    public SatelliteSelectorWidget SatelliteSelector;
    public FrequencyWidget FrequencyControl;
    public RotatorWidget RotatorControl;
    public SatellitePhotoWidget SatellitePhotoWidget;
    public AutoMonitorBannerWidget AutoMonitorBannerWidget;

    // panels
    public GroupViewPanel? GroupViewPanel;
    public SatelliteDetailsPanel? SatelliteDetailsPanel;
    public PassesPanel? PassesPanel;
    public TimelinePanel? TimelinePanel;
    public SkyViewPanel? SkyViewPanel;
    public EarthViewPanel? EarthViewPanel;
    public TransmittersPanel? TransmittersPanel;
    public MonitoredSatellitesPanel? MonitoredSatellitesPanel;
    public WaterfallPanel? WaterfallPanel;
    public QsoEntryPanel? QsoEntryPanel;
    public Ft4ConsolePanel? Ft4ConsolePanel;
    public RecorderPanel? RecorderPanel;
    public QsoSchedulerPanel? QsoSchedulerPanel;

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
    public AutoRecorder AutoRecorder;
    
    public void ClosePanels()
    {
      Ft4ConsolePanel?.Close();
      GroupViewPanel?.Close();
      SatelliteDetailsPanel?.Close();
      PassesPanel?.Close();
      TimelinePanel?.Close();
      SkyViewPanel?.Close();
      EarthViewPanel?.Close();
      MonitoredSatellitesPanel?.Close();
      TransmittersPanel?.Close();
      WaterfallPanel?.Close();
      QsoEntryPanel?.Close();
      RecorderPanel?.Close();
      QsoSchedulerPanel?.Close();
    }
  }
}

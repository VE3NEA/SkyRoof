using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;
using SharpGL;

namespace VE3NEA
{
  public class ExceptionLogger
  {
    private static NativeSoapySdr.SoapySDRLogHandlerDelegate LogHandlerDelegate = new(SoapySdrLogHandler);
    private static int MainThreadId;

    public static void Initialize()
    {
      string appName = AppDomain.CurrentDomain.FriendlyName ?? "log";
      string fileName = Path.Combine(Utils.GetUserDataFolder(), "Logs", $"{appName}_.txt");
      Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
          .WriteTo.File(fileName, 
            rollingInterval: RollingInterval.Day, 
            fileSizeLimitBytes: 3_000_000,
            rollOnFileSizeLimit: true,
            retainedFileCountLimit: 20,
            shared: true
           ).CreateLogger();


      // handle exceptions in UI thread
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
      Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

      // handle exceptions in non-UI threads
      MainThreadId = Environment.CurrentManagedThreadId;
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
      
      // Handle exceptions in async/await code
      TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

      // handle exceptions in SoapySDR
      NativeSoapySdr.SoapySDR_registerLogHandler(LogHandlerDelegate);

      Log.Information($"Starting {typeof(Utils).Assembly.GetName().FullName}");
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      var exception = e.ExceptionObject as Exception;
      Log.Error(exception, "Worker thread exception occurred:");

      // show dialog if not in the message loop but still on the main UI thread,
      // e.g., in the main form's constructor
      if (Environment.CurrentManagedThreadId == MainThreadId) ShowDialog(exception, false);
    }

    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
      Log.Error(e.Exception, "UI thread exception occurred:");
      if (ShowDialog(e.Exception) == DialogResult.Abort) Application.Exit();
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Async task exception occurred:");
        e.SetObserved(); // Prevents the process from terminating
    }

    private static DialogResult ShowDialog(Exception? e, bool allowContinue = true)
    {
      string errorMessage = $"Exception: {e?.Message ?? e?.ToString()}\nModule: {e.Source} \nFunction: {e.TargetSite}";

      if (allowContinue)
        return MessageBox.Show(errorMessage, AppDomain.CurrentDomain.FriendlyName, MessageBoxButtons.AbortRetryIgnore,
          MessageBoxIcon.Stop);

      MessageBox.Show(errorMessage, AppDomain.CurrentDomain.FriendlyName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
      return DialogResult.OK;
    }

    internal static void CheckOpenglError(OpenGL gl, bool log = true)
    {
      uint err;

      while ((err = gl.GetError()) != OpenGL.GL_NO_ERROR)
        if (log)
        {
          string errorName = gl.ErrorString(err);
          if (string.IsNullOrEmpty(errorName)) errorName = $"error {err}";
          string message = $"OpenGL: {errorName}\n{new StackTrace(true)}";
          Log.Error(message);
          Debug.WriteLine(message);
        }
    }

    public static void SoapySdrLogHandler(NativeSoapySdr.SoapySDRLogLevel logLevel, IntPtr messagePtr)
    {
      try
      {
        if (messagePtr == IntPtr.Zero) return;
        string message = Marshal.PtrToStringAnsi(messagePtr)!;
        Log.Information($"[{logLevel}]: {message}");
      }
      catch { }
    }
  }
}

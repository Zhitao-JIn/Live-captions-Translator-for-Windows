/*
be used to interact with the Live Captions window on Windows
1.keep available
2.retrieve current caption text
*/
using System.Diagnostics;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using Serilog;

public static class WinCaption
{
    private static readonly Serilog.ILogger _logger =
        new LoggerConfiguration()
            .WriteTo.File("log.txt")
            .WriteTo.Console() 
            .CreateLogger();
    public const string PROCESS_NAME = "LiveCaptions";
    public const string TARGET_CLASS_NAME = "LiveCaptionsDesktopWindow";
    public const string TARGET_TEXTBLOCK_AUTOMATIONID = "CaptionsTextBlock";
    private static Process? Process = null;
    private static AutomationElement? Window = null;
    private static AutomationElement? CaptionsTextBlock = null;
    private static String? CurrentCaption = null;
    private static object KeepAvailableLock = new object();
    private static System.Threading.Timer? timer;



    private static bool GetProcess()
    {
        _logger.Information("Trying to get process...");
        if (Process == null || Process.HasExited)//try to get process,if no process or exited
        {
            _logger.Information("Searching for process...");
            Process = Process.GetProcessesByName(PROCESS_NAME).FirstOrDefault();
        }
        if (Process == null || Process.HasExited)//start new process, if not found or exited
        {
            _logger.Information("Process not found. Starting new process...");
            Process = Process.Start(PROCESS_NAME);
        }
        Thread.Sleep(500);
        if (Process != null && !Process.HasExited)
        {
            _logger.Information("Process found: " + Process.Id);
            
        }
        else
        {
            _logger.Information("no process found after starting.");
        }
        return Process != null && !Process.HasExited;
    }

    private static bool GetWindows()
    {
        using var automation = new UIA3Automation();
        if (Window == null || !Window.IsAvailable)//try to get window, if no window or closed
        {
            _logger.Information("Trying to get window...");
            GetProcess();
            if (Process != null && !Process.HasExited){
                var app = FlaUI.Core.Application.Attach(Process.Id);
                Window =  app.GetMainWindow(automation);
            }
        }
        Thread.Sleep(500);
        if (Window != null && Window.Patterns.Window != null)
        {
            _logger.Information("Window found: " + Window.Properties.Name);
        }
        else
        {
            _logger.Information("no window found.");
        }
        return Window != null && Window.Patterns.Window != null;
    }
    private static bool GetCaption()
    {
        if (CaptionsTextBlock == null || !CaptionsTextBlock.IsAvailable)
        {
            _logger.Information("Trying to get CaptionsTextBlock...");
            GetWindows();
            if (Window != null)
            {
                CaptionsTextBlock = Window.FindFirstDescendant(cf => cf.ByAutomationId(TARGET_TEXTBLOCK_AUTOMATIONID));
            }
        }
        Thread.Sleep(500);
        if (CaptionsTextBlock != null && CaptionsTextBlock.Properties.Name.TryGetValue(out CurrentCaption) && !string.IsNullOrEmpty(CurrentCaption))
        {
            CurrentCaption = CurrentCaption.Replace("\r", "").Replace("\n", "");;
            //"[" + DateTime.Now.ToString("HH:mm:ss") + "]  " +
            _logger.Information(
            "Current caption: " +
            (CurrentCaption != null ? CurrentCaption[Math.Max(0, CurrentCaption.Length - 30)..CurrentCaption.Length] : "null"));
            return true;
        }
        else
        {
            CurrentCaption = null;
            //"[" + DateTime.Now.ToString("HH:mm:ss") + "]  " +
             _logger.Warning(
            "Caption is null or unavailable.");
            return false;
        }

    }

    private static void KeepAvailable()
    {   
        GetCaption();
    }

    public static void Start()
    {
        while (!GetProcess());
        while (!GetWindows());
        while (!GetCaption());
        timer = new System.Threading.Timer(_ =>
        {   
            if (!Monitor.TryEnter(KeepAvailableLock)) return; 
            try
            {
                KeepAvailable();
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error in KeepAvailable: " + ex.Message);
                _logger.Error("Error in KeepAvailable: " + ex.Message);
            }
            finally
            {
                Monitor.Exit(KeepAvailableLock);
            }
        }, null, 0, 500);
        //Console.WriteLine("Started monitoring caption.");
        _logger.Information("initialization finished. Started monitoring caption.");
        //Console.ReadLine();
    }

    public static string? GetCurrentCaption()
    {
        return CurrentCaption;
    }
}
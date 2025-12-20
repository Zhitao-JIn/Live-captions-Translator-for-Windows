/*
be used to interact with the Live Captions window on Windows
1.keep available
2.retrieve current caption text
*/
using System.Diagnostics;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;

public static class WinCaption
{
    public const string PROCESS_NAME = "LiveCaptions";
    public const string TARGET_CLASS_NAME = "LiveCaptionsDesktopWindow";
    public const string TARGET_TEXTBLOCK_AUTOMATIONID = "CaptionsTextBlock";
    private static Process? Process = null;
    private static AutomationElement? Window = null;
    private static AutomationElement? CaptionsTextBlock = null;
    private static String? CurrentCaption = null;
    private static object KeepAvailableLock = new object();



    private static bool GetProcess()
    {
        if (Process == null || Process.HasExited)//try to get process,if no process or exited
        {
            Process = Process.GetProcessesByName(PROCESS_NAME).FirstOrDefault();
        }
        if (Process == null || Process.HasExited)//start new process, if not found or exited
        {
            Process = Process.Start(PROCESS_NAME);
        }
        Thread.Sleep(500);
        return Process != null && !Process.HasExited;
    }

    private static bool GetWindows()
    {
        using var automation = new UIA3Automation();
        if (Window == null || !Window.IsAvailable)//try to get window, if no window or closed
        {
            GetProcess();
            if (Process != null){
                var app = FlaUI.Core.Application.Attach(Process.Id);
                Window =  app.GetMainWindow(automation);
            }
        }
        Thread.Sleep(500);
        return Window != null && Window.Patterns.Window != null;
    }
    private static bool GetCaption()
    {
        if (CaptionsTextBlock == null || !CaptionsTextBlock.IsAvailable)
        {
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
            return true;
        }
        else
        {
            CurrentCaption = null;
            return false;
        }
    }

    private static void KeepAvailable()
    {   
        GetCaption();
        if (CurrentCaption != null)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "]  " +
            "Current caption: " +
            (CurrentCaption != null ? CurrentCaption[Math.Max(0, CurrentCaption.Length - 30)..CurrentCaption.Length] : "null"));
        }
        else
        {
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "]  " +
            "Caption is null or unavailable.");
        }
    }

    public static void Start()
    {
        while (!GetProcess())
        {
            Console.WriteLine("Waiting for process...");
        }
        Console.WriteLine("Process found.");
        while (!GetWindows())
        {
            Console.WriteLine("Waiting for window...");
        }
        Console.WriteLine("Window found.");
        while (!GetCaption())
        {
            Console.WriteLine("Waiting for caption...");
        }
        Console.WriteLine("Caption found: " + CurrentCaption);
        var timer = new System.Threading.Timer(_ =>
        {   
            if (!Monitor.TryEnter(KeepAvailableLock)) return; 
            try
            {
                KeepAvailable();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in KeepAvailable: " + ex.Message);
            }
            finally
            {
                Monitor.Exit(KeepAvailableLock);
            }
        }, null, 0, 500);
        Console.WriteLine("Started monitoring caption.");
        Console.ReadLine();
    }

    public static string? GetCurrentCaption()
    {
        return CurrentCaption;
    }
}
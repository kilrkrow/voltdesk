using System;
using System.IO;
using System.Threading;
using Microsoft.UI.Dispatching;

namespace PowerDesktopApp
{
    internal static class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        static void Main(string[] args)
        {
            var log = Path.Combine(Path.GetTempPath(), "voltdesk-boot.log");
            void L(string m) { File.AppendAllText(log, DateTime.Now.ToString("HH:mm:ss.fff") + " " + m + "\r\n"); }
            try { File.WriteAllText(log, ""); } catch {}
            L("main");
            const string mutexName = "Global\\VoltDeskSingleInstanceMutex";
            _mutex = new Mutex(true, mutexName, out bool createdNew);
            L("mutex createdNew=" + createdNew);
            if (!createdNew)
            {
                try { createdNew = _mutex.WaitOne(0); L("waitOne=" + createdNew); }
                catch (AbandonedMutexException) { createdNew = true; L("abandoned"); }
                if (!createdNew)
                {
                    bool wantExit = false;
                    foreach (var a in args)
                    {
                        if (a == "--exit")
                            wantExit = true;
                    }
                    string evName = wantExit ? App.ExitEventName : App.OpenSettingsEventName;
                    try
                    {
                        using var ev = EventWaitHandle.OpenExisting(evName);
                        ev.Set();
                        L("signaled " + evName);
                    }
                    catch (Exception ex)
                    {
                        L("signal fail " + ex.Message);
                    }
                    L("exit other instance");
                    return;
                }
            }

            try
            {
                L("comwrappers");
                WinRT.ComWrappersSupport.InitializeComWrappers();
                L("app.start");
                Microsoft.UI.Xaml.Application.Start(p =>
                {
                    L("start callback");
                    var context = new DispatcherQueueSynchronizationContext(
                        DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    L("new App");
                    new App();
                    L("app constructed");
                });
                L("app.start returned");
            }
            catch (Exception ex)
            {
                L("EX " + ex);
            }
            finally
            {
                L("finally");
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
    }
}

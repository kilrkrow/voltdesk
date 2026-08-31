using System;
using System.IO;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace PowerDesktopApp
{
    public class App : Microsoft.UI.Xaml.Application
    {
        public const string OpenSettingsEventName = @"Local\VoltDeskOpenSettings";
        public const string ExitEventName = @"Local\VoltDeskExit";

        private TrayApplicationContext? _tray;
        private LifetimeWindow? _lifetime;
        private EventWaitHandle? _openSettingsEvent;
        private EventWaitHandle? _exitEvent;
        private Thread? _signalThread;
        private volatile bool _stopSignals;

        public App()
        {
            UnhandledException += (_, e) =>
            {
                try
                {
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "voltdesk-boot.log"),
                        DateTime.Now.ToString("HH:mm:ss.fff") + " UNHANDLED " + e.Exception + "\r\n");
                    e.Handled = true;
                }
                catch { }
            };
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var log = Path.Combine(Path.GetTempPath(), "voltdesk-boot.log");
            void L(string m) { File.AppendAllText(log, DateTime.Now.ToString("HH:mm:ss.fff") + " " + m + "\r\n"); }
            try
            {
                L("OnLaunched");
                _lifetime = new LifetimeWindow();
                L("lifetime");
                _tray = new TrayApplicationContext();
                L("tray");
                StartSignalWatchers();
                foreach (var a in Environment.GetCommandLineArgs())
                {
                    if (a == "--settings")
                    {
                        L("open settings");
                        _tray.OpenSettings();
                        L("settings opened");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                L("OnLaunched EX " + ex);
            }
        }

        internal void ShutdownFromTray()
        {
            _stopSignals = true;
            try { _openSettingsEvent?.Set(); } catch { }
            try { _exitEvent?.Set(); } catch { }
            try
            {
                _lifetime?.AllowClose();
                _lifetime?.Close();
            }
            catch { }
            _lifetime = null;
            Exit();
        }

        private void StartSignalWatchers()
        {
            DispatcherQueue dq = DispatcherQueue.GetForCurrentThread();
            _openSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, OpenSettingsEventName);
            _exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);
            _signalThread = new Thread(() =>
            {
                WaitHandle[] handles = new WaitHandle[] { _openSettingsEvent, _exitEvent };
                while (!_stopSignals)
                {
                    int i = WaitHandle.WaitAny(handles, 500);
                    if (_stopSignals)
                        break;
                    if (i == 0)
                        dq.TryEnqueue(() => _tray?.OpenSettings());
                    else if (i == 1)
                        dq.TryEnqueue(() => _tray?.ExitFromTray());
                }
            })
            {
                IsBackground = true,
                Name = "VoltDeskSignals"
            };
            _signalThread.Start();
        }
    }
}

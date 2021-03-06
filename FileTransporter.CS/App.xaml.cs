using FileTransporter.SimpleSocket;
using FzLib.Program.Runtime;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace FileTransporter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static ILog log = LogManager.GetLogger(typeof(App));
        private TrayIcon tray;

        public void ShowTray()
        {
            tray.Show();
        }

        public void SetStartup(bool run)
        {
            if (run)
            {
                FzLib.Program.Startup.CreateRegistryKey("s");
            }
            else
            {
                FzLib.Program.Startup.DeleteRegistryKey();
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            log.Info("程序启动");
#if !DEBUG
            UnhandledException.RegistAll();

            UnhandledException.UnhandledExceptionCatched += UnhandledException_UnhandledExceptionCatched;
#endif
            FzLib.Program.App.SetWorkingDirectoryToAppPath();
            FzLib.Program.Startup.AppName = Name;
            tray = new TrayIcon(new System.Drawing.Icon("./icon.ico"), App.Name);
            tray.ReShowWhenDisplayChanged = true;
            if (e.Args.Length > 0 && e.Args[0] == "s")
            {
                MainWindow = new MainWindow(true);
                ShowTray();
            }
            else
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
            }

            tray.MouseLeftClick += (p1, p2) =>
            {
                tray.Hide();
                MainWindow.Visibility = Visibility.Visible;
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
                MainWindow.Focus();
            };
        }

        private void UnhandledException_UnhandledExceptionCatched(object sender, FzLib.Program.Runtime.UnhandledExceptionEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("发生异常：" + Environment.NewLine + e.Exception.ToString());
                });
            }
            finally
            {
            }
            try
            {
                log.Error("未捕获的异常", e.Exception);
            }
            catch
            {
            }
            Dispatcher.Invoke(() =>
            {
                Shutdown();
            });
        }

        public static string Name => "FileTransporter";

        public static void Log(LogLevel level, string msg, Exception ex = null)
        {
            switch (level)
            {
                case LogLevel.Info:
                    log.Info(msg, ex);
                    break;

                case LogLevel.Debug:
                    log.Debug(msg, ex);
                    break;

                case LogLevel.Warn:
                    log.Warn(msg, ex);
                    break;

                case LogLevel.Error:
                    log.Error(msg, ex);
                    break;
            }
            NewLog?.Invoke(null, new LogEventArgs(level, msg, ex));
        }

        public static event EventHandler<LogEventArgs> NewLog;
    }
}
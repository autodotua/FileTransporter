using FileTransporter.Panels;
using FileTransporter.SimpleSocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FileTransporter
{
    public enum DialogIconType
    {
        Info = 0xE946,
        Error = 0xE783,
        Warning = 0xE7BA
    }

    public class Log
    {
        public string Content { get; set; }
        public DateTime Time { get; set; }
        public Brush TypeBrush { get; set; }
    }

    public partial class MainWindow : Window
    {
        private bool isDialogOpened = false;

        public MainWindow(bool autoStart = false)
        {
            Debug.Assert(Current == null);
            Current = this;
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Panel = new LoginPanel();

            if (FzLib.Program.Startup.IsRegistryKeyExist() == FzLib.IO.ShortcutStatus.Exist)
            {
                menuStartup.IsChecked = true;
            }
            if (autoStart)
            {
            }

            SimpleSocketUtility.NewLog += App_NewLog;
            App.NewLog += App_NewLog;
        }

        public static MainWindow Current { get; private set; }
        public bool IsClient => ViewModel.Panel is ClientPanel;
        public bool IsServer => ViewModel.Panel is ServerPanel;
        public MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

        public async Task ShowMessageAsync(string message, DialogIconType icon, string detail = null)
        {
            tbkDialogMessage.Text = message;
            smbDialogIcon.Glyph = new string(new char[] { (char)icon });
            switch (icon)
            {
                case DialogIconType.Info:
                    smbDialogIcon.Foreground = Foreground;
                    break;

                case DialogIconType.Error:
                    smbDialogIcon.Foreground = Brushes.Red;
                    break;

                case DialogIconType.Warning:
                    smbDialogIcon.Foreground = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xd7, 0x66));
                    break;
            }
            if (detail != null)
            {
                expDialogDetail.Visibility = Visibility.Visible;
                tbkDialogDetail.Text = detail;
            }
            else
            {
                expDialogDetail.Visibility = Visibility.Collapsed;
            }
            if (!isDialogOpened)
            {
                isDialogOpened = true;
                await dialog.ShowAsync();
                isDialogOpened = false;
            }
        }

        public async Task ShowMessageAsync(string message, Exception ex)
        {
            await ShowMessageAsync(message + Environment.NewLine + ex.Message, DialogIconType.Error, ex.ToString());
        }

        private void App_NewLog(object sender, LogEventArgs e)
        {
#if !DEBUG
            if (e.Level == LogLevel.Debug)
            {
                return;
            }
#endif
            Dispatcher.Invoke(() =>
            {
                Brush brush = e.Level switch
                {
                    LogLevel.Error => Brushes.Red,
                    LogLevel.Debug => Brushes.Gray,
                    LogLevel.Info => Foreground,
                    LogLevel.Warn => new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0x00)),
                    _ => throw new NotImplementedException()
                };
                ViewModel.Logs.Add(new Log()
                {
                    Time = DateTime.Now,
                    Content = e.Message + (e.Exception == null ? "" : $"（{e.Exception.Message}）"),
                    TypeBrush = brush
                });

                if (ViewModel.Logs.Count > 0)
                {
                    lvwLogs.ScrollIntoView(ViewModel.Logs[^1]);
                    while (ViewModel.Logs.Count > ViewModel.MaxLogCount)
                    {
                        ViewModel.Logs.RemoveAt(0);
                    }
                }
            });
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Logs.Clear();
        }

        private void MenuStartup_Click(object sender, RoutedEventArgs e)
        {
            if (FzLib.Program.Startup.IsRegistryKeyExist() == FzLib.IO.ShortcutStatus.Exist)
            {
                menuStartup.IsChecked = false;
                (App.Current as App).SetStartup(false);
            }
            else
            {
                menuStartup.IsChecked = true;
                (App.Current as App).SetStartup(true);
            }
        }

        private void MenuTray_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App).ShowTray();
            Visibility = Visibility.Collapsed;
        }

        private void WaitForServerOrClientOpen()
        {
            (ViewModel.Panel as LoginPanel).WaitForClientOpenedAsync().ContinueWith(p =>
            {
#if DEBUG
                Dispatcher.Invoke(() =>
                {
                    var panel = new ClientPanel(p.Result);
                    var win = new Window() { Padding = new Thickness(8), Left = 1200, Width = 400 };
                    win.Content = panel;
                    win.Show();
                    panel.SocketClosed += (p1, p2) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var disPanel = new SessionDisconnectPanel();
                            ViewModel.Panel = disPanel;
                            disPanel.ReturnToLoginPanel += (p3, p4) =>
                            {
                                ViewModel.Panel = new LoginPanel();
                                WaitForServerOrClientOpen();
                            };
                        });
                    };
                });
#else
                Dispatcher.Invoke(() =>
                {
                    var panel = new ClientPanel(p.Result);
                    panel.SocketClosed += (p1, p2) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var disPanel = new SessionDisconnectPanel();
                            ViewModel.Panel = disPanel;
                            disPanel.ReturnToLoginPanel += (p3, p4) =>
                            {
                                ViewModel.Panel = new LoginPanel();
                                WaitForServerOrClientOpen();
                            };
                        });
                    };
                    ViewModel.Panel = panel;
                });

#endif
            });

            (ViewModel.Panel as LoginPanel).WaitForServerOpenedAsync().ContinueWith(p =>
            {
#if DEBUG
                Dispatcher.Invoke(() =>
                {
                    var panel = new ServerPanel(p.Result);
                    var win = new Window() { Padding = new Thickness(8), Left = 0, Width = 400 };
                    win.Content = panel;
                    win.Show();
                    win.Closed += (p1, p2) =>
                    {
                        p.Result.Server.Close();
                    };
                });
#else
                Dispatcher.Invoke(() =>
                {
                    var panel = new ServerPanel(p.Result);
                    ViewModel.Panel = panel;
                });
#endif
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WaitForServerOrClientOpen();
        }
    }
}
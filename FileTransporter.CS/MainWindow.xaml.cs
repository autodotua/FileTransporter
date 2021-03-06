using FileTransporter.Panels;
using FileTransporter.SimpleSocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FileTransporter
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();
        public static MainWindow Current { get; private set; }
        public bool IsClient => ViewModel.Panel is ClientPanel;
#if DEBUG
        public bool IsServer = true;
#else
        public bool IsServer => ViewModel.Panel is ServerPanel;
#endif

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
                    lbxLogs.ScrollIntoView(ViewModel.Logs[^1]);
                    while (ViewModel.Logs.Count > ViewModel.MaxLogCount)
                    {
                        ViewModel.Logs.RemoveAt(0);
                    }
                }
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bool ok = false;
            (ViewModel.Panel as LoginPanel).WaitForClientOpenedAsync().ContinueWith(p =>
            {
                Debug.Assert(!ok);
#if !DEBUG
                ok = true;
#endif
                Dispatcher.Invoke(() =>
                {
                    var panel = new ClientPanel(p.Result);
                    ViewModel.Panel = panel;
                });
            });
            (ViewModel.Panel as LoginPanel).WaitForServerOpenedAsync().ContinueWith(p =>
            {
                Debug.Assert(!ok);
#if DEBUG
                Dispatcher.Invoke(() =>
                {
                    var panel = new ServerPanel(p.Result);
                    var win = new Window() { Padding = new Thickness(8), Left = 0, Width = 400 };
                    win.Content = panel;
                    win.Show();
                });
#else
                ok = true;
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

        private bool isDialogOpened = false;

        public async Task ShowMessageAsync(string message)
        {
            tbkDialogMessage.Text = message;
            if (!isDialogOpened)
            {
                isDialogOpened = true;
                await dialog.ShowAsync();
                isDialogOpened = false;
            }
        }

        public async Task ShowMessageAsync(string message, Exception ex)
        {
            await ShowMessageAsync(message + Environment.NewLine + ex.Message);
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

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Logs.Clear();
        }
    }

    public class Log
    {
        public DateTime Time { get; set; }
        public string Content { get; set; }
        public Brush TypeBrush { get; set; }
    }
}
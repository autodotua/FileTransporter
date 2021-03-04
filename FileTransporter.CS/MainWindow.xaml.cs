using FileTransporter.Panels;
using FileTransporter.SimpleSocket;
using FileTransporter.Util;
using FzLib.Extension;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileTransporter
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Log> Logs { get; } = new ObservableCollection<Log>();

        private int maxLogCount = 10000;

        public MainWindowViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int MaxLogCount
        {
            get => maxLogCount;
            set
            {
                if (value > 100000)
                {
                    value = 100000;
                }
                else if (value < 10)
                {
                    value = 10;
                }
                this.SetValueAndNotify(ref maxLogCount, value, nameof(MaxLogCount));
            }
        }

        private UserControl panel;

        public UserControl Panel
        {
            get => panel;
            set => this.SetValueAndNotify(ref panel, value, nameof(Panel));
        }
    }

    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

        public MainWindow(bool autoStart = false)
        {
            bool clientOn = Config.Instance.ClientOn;
            bool serverOn = Config.Instance.ServerOn;
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

            SimpleSocketUtility.NewLog += SimpleSocketUtility_NewLog;
        }

        private void SimpleSocketUtility_NewLog(object sender, SimpleSocketLogEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Brush brush = e.Level switch
                {
                    LogLevel.Error => Brushes.Red,
                    LogLevel.Info => Foreground,
                    LogLevel.Warn => new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0x00))
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
                ok = true;
                Dispatcher.Invoke(() =>
                {
                    var panel = new ClientPanel(p.Result);
                    ViewModel.Panel = panel;
                });
            });
            (ViewModel.Panel as LoginPanel).WaitForServerOpenedAsync().ContinueWith(p =>
            {
                Debug.Assert(!ok);
                //ok = true;
                Dispatcher.Invoke(() =>
                { });
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        public async Task ShowMessageAsync(string message)
        {
            tbkDialogMessage.Text = message;
            await dialog.ShowAsync();
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
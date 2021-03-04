using FileTransporter.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Regex rLog = new Regex(@"(?<Time>[0-9/: ]{19}) \[(?<Type>.)\] \[[^\]]+\] (?<Content>.*)", RegexOptions.Compiled);

        public MainWindow(bool autoStart = false)
        {
            bool clientOn = Config.Instance.ClientOn;
            bool serverOn = Config.Instance.ServerOn;
            InitializeComponent();
            DataContext = this;

            if (FzLib.Program.Startup.IsRegistryKeyExist() == FzLib.IO.ShortcutStatus.Exist)
            {
                menuStartup.IsChecked = true;
            }
            if (autoStart)
            {
            }
        }

        public ObservableCollection<Log> Logs { get; } = new ObservableCollection<Log>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        public async Task ShowMessage(string message)
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
            Logs.Clear();
        }

        private SocketHelper helper = new SocketHelper();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            helper.StartServer();
        }

        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            helper.StartClient();
        }
    }

    public class Log
    {
        public string Time { get; set; }
        public string Content { get; set; }
        public Brush TypeBrush { get; set; }
    }
}
using FileTransporter.FileSimpleSocket;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

namespace FileTransporter.Panels
{
    public partial class SendFilePanel : UserControl
    {
        public SendFilePanelViewModel ViewModel { get; } = new SendFilePanelViewModel();

        public SendFilePanel()
        {
            InitializeComponent();
            DataContext = ViewModel;
#if DEBUG
            //ViewModel.Files.Add(new TransporterFile(@"C:\Users\autod\Desktop\Road Rash 2002.zip"));
#endif
        }

        //public SocketHelperBase Socket { get; set; }
        //public SimpleSocketSession<SocketData> Session { get; set; }
        public static readonly DependencyProperty SocketProperty = DependencyProperty.Register(
     nameof(Socket),
      typeof(SocketHelperBase),
      typeof(SendFilePanel));

        public SocketHelperBase Socket
        {
            get => GetValue(SocketProperty) as SocketHelperBase;
            set => SetValue(SocketProperty, value);
        }

        public static readonly DependencyProperty SessionProperty = DependencyProperty.Register(
     nameof(Session),
      typeof(SimpleSocketSession<SocketData>),
      typeof(SendFilePanel));

        public SimpleSocketSession<SocketData> Session
        {
            get => GetValue(SessionProperty) as SimpleSocketSession<SocketData>;
            set => SetValue(SessionProperty, value);
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Sending = true;
            foreach (var file in ViewModel.Files.Where(p => !p.Finished))
            {
                if (Socket is ClientSocketHelper cs)
                {
                    await cs.SendFileAsync(file.File.FullName,
                        p =>
                        {
                            file.UpdateProgress(p.CurrentLength);
                            p.Cancel = ViewModel.Stopping;
                        });
                }
                else if (Socket is ServerSocketHelper ss)
                {
                    await ss.SendFileAsync(Session, file.File.FullName,
                        p =>
                        {
                            file.UpdateProgress(p.CurrentLength);
                            p.Cancel = ViewModel.Stopping;
                        });
                }
            }
            ViewModel.Sending = false;
            ViewModel.Stopping = false;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "所有文件|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    ViewModel.Files.Add(new TransporterFile(file));
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Sending = false;
            ViewModel.Stopping = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(!ViewModel.Sending);
            var file = (sender as FrameworkElement).Tag as TransporterFile;
            ViewModel.Files.Remove(file);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(!ViewModel.Sending);
            ViewModel.Files.Clear();
        }
    }
}
using FileTransporter.Dto;
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
    public partial class FileTransportPanel : UserControl
    {
        public FileTransportPanelViewModel ViewModel { get; } = new FileTransportPanelViewModel();

        public FileTransportPanel()
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
      typeof(FileTransportPanel),
      new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
      {
          if ((s as FileTransportPanel).Type == FilePanelType.Receive)
          {
              (s as FileTransportPanel).StartListenFileReceive();
          }
      })));

        public SocketHelperBase Socket
        {
            get => GetValue(SocketProperty) as SocketHelperBase;
            set => SetValue(SocketProperty, value);
        }

        public static readonly DependencyProperty SessionProperty = DependencyProperty.Register(
     nameof(Session),
      typeof(SimpleSocketSession<SocketData>),
      typeof(FileTransportPanel));

        public SimpleSocketSession<SocketData> Session
        {
            get => GetValue(SessionProperty) as SimpleSocketSession<SocketData>;
            set => SetValue(SessionProperty, value);
        }

        private bool isListenningFileReceive = false;

        public void StartListenFileReceive()
        {
            if (isListenningFileReceive)
            {
                return;
            }
            if (Socket == null)
            {
                return;
            }
            isListenningFileReceive = true;
            Socket.TransportFileProgress += Socket_TransportFileProgress;
        }

        private void Socket_TransportFileProgress(object sender, TransportFileProgressEventArgs e)
        {
            TransportFile file = ViewModel.Files.FirstOrDefault(p => p.ID == e.File.ID);
            if (file == null)
            {
                Dispatcher.Invoke(() =>
                {
                    file = new TransportFile()
                    {
                        ID = e.File.ID,
                        Length = e.File.Length,
                        Name = e.File.Name,
                        Path = e.File.Name,
                        Time = DateTime.Now,
                        Status = TransportFileStatus.Receiving,
                        From = MainWindow.Current.IsServer ? e.Session.RemoteName : ""
                    };
                    ViewModel.Files.Add(file);
                });
            }
            if (e.Cancel)//远端取消
            {
                ViewModel.Working = false;
                ViewModel.Stopping = false;
                file.Status = TransportFileStatus.Canceled;
                return;
            }
            Dispatcher.Invoke(() =>
            {
                file.UpdateProgress(e.Length);
            });
            if (ViewModel.Stopping)//本地请求停止
            {
                file.Status = TransportFileStatus.Canceled;
                e.Cancel = true;
                ViewModel.Working = false;
                ViewModel.Stopping = false;
            }
            else if (file.Length == file.TransportedLength)//传输完成
            {
                ViewModel.Working = false;
            }
            else
            {
                ViewModel.Working = true;
            }
        }

        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
     nameof(Type),
      typeof(FilePanelType),
      typeof(FileTransportPanel),
      new PropertyMetadata(FilePanelType.Send
          , new PropertyChangedCallback((s, e) =>
          {
              (s as FileTransportPanel).ViewModel.Type = (FilePanelType)e.NewValue;
              if (e.NewValue.Equals(FilePanelType.Receive))
              {
                  (s as FileTransportPanel).StartListenFileReceive();
              }
          })));

        public FilePanelType Type
        {
            get => (FilePanelType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Working = true;

            foreach (var file in ViewModel.Files.Where(p => p.Status == TransportFileStatus.Ready))
            {
                try
                {
                    file.Status = TransportFileStatus.Sending;
                    if (Socket is ClientSocketHelper cs)
                    {
                        await cs.SendFileAsync(file.File.FullName, p => SendFileProgress(file, p));
                    }
                    else if (Socket is ServerSocketHelper ss)
                    {
                        await ss.SendFileAsync(Session, file.File.FullName, p => SendFileProgress(file, p));
                    }
                }
                catch (OperationCanceledException ex)
                {
                    await MainWindow.Current.ShowMessageAsync("传输被取消");
                    file.Status = TransportFileStatus.Canceled;
                }
                catch (Exception ex)
                {
                    await MainWindow.Current.ShowMessageAsync("传输发生错误", ex);
                    file.Status = TransportFileStatus.Error;
                }
            }

            ViewModel.Working = false;
            ViewModel.Stopping = false;
        }

        private void SendFileProgress(TransportFile file, TransportProgress p)
        {
            if (ViewModel.Stopping)
            {
                p.Cancel = true;
                file.Status = TransportFileStatus.Canceled;
            }
            else
            {
                file.UpdateProgress(p.CurrentLength);
            }
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
                    ViewModel.Files.Add(new TransportFile(file));
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Working = false;
            ViewModel.Stopping = true;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(!ViewModel.Working);
            var file = (sender as FrameworkElement).Tag as TransportFile;
            ViewModel.Files.Remove(file);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(!ViewModel.Working);
            ViewModel.Files.Clear();
        }
    }

    public enum FilePanelType
    {
        Send,
        Receive
    }
}
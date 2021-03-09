using FileTransporter.FileSimpleSocket;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
    public partial class ClientPanel : SocketPanelBase
    {
        public ClientPanel(ClientSocketHelper socket)
        {
            Socket = socket;
            socket.Client.Closed += Socket_Closed;
            ViewModel = new ClientPanelViewModel(socket);
            DataContext = ViewModel;
            InitializeComponent();
        }

        public event EventHandler SocketClosed;

        public ClientPanelViewModel ViewModel { get; set; }

        private void FileBrowserPanel_DownloadStarted(object sender, EventArgs e)
        {
            tab.SelectedIndex = 1;
        }

        private void FileTransportPanel_ReceiveStarted(object sender, EventArgs e)
        {
            tab.SelectedIndex = 1;
        }

        private void Socket_Closed(object sender, EventArgs e)
        {
            SocketClosed?.Invoke(sender, e);
        }
    }
}
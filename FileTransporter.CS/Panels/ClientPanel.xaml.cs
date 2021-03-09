using FileTransporter.FileSimpleSocket;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
    public partial class ClientPanel : SocketPanelBase
    {
        public ClientPanelViewModel ViewModel { get; set; }

        public ClientPanel(ClientSocketHelper socket)
        {
            Socket = socket;
            socket.Client.Closed += Socket_Closed;
            ViewModel = new ClientPanelViewModel(socket);
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void Socket_Closed(object sender, EventArgs e)
        {
            SocketClosed?.Invoke(sender, e);
        }

        public event EventHandler SocketClosed;
    }
}
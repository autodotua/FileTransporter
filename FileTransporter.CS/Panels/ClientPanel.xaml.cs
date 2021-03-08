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
            socket.Client.Session.Disconnected += Session_Disconnected;
            ViewModel = new ClientPanelViewModel(socket);
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void Session_Disconnected(object sender, EventArgs e)
        {
            SocketDisconnect?.Invoke(sender, e);
        }

        public event EventHandler SocketDisconnect;
    }
}
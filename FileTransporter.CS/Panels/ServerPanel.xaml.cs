using FileTransporter.FileSimpleSocket;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
    public partial class ServerPanel : SocketPanelBase
    {
        public ServerPanel(ServerSocketHelper socket)
        {
            Socket = socket;
            ViewModel = new ServerPanelViewModel(socket);
            DataContext = ViewModel;
            InitializeComponent();
        }

        public ServerPanelViewModel ViewModel { get; set; }

        private void FileTransportPanel_ReceiveStarted(object sender, System.EventArgs e)
        {
            tab.SelectedIndex = 1;
        }

        private void FileTransportPanel_SendStarted(object sender, System.EventArgs e)
        {
            tab.SelectedIndex = 0;
        }
    }
}
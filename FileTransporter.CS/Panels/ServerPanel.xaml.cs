using FileTransporter.FileSimpleSocket;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
    public partial class ServerPanel : SocketPanelBase
    {
        public ServerPanelViewModel ViewModel { get; set; }

        public ServerPanel(ServerSocketHelper socket)
        {
            Socket = socket;
            ViewModel = new ServerPanelViewModel(socket);
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
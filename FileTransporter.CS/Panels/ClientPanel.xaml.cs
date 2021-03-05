using FileTransporter.FileSimpleSocket;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
    public partial class ClientPanel : UserControl
    {
        public ClientPanelViewModel ViewMode { get; set; } = new ClientPanelViewModel();

        public ClientPanel(ClientSocketHelper socket)
        {
            DataContext = ViewMode;
            InitializeComponent();
            Socket = socket;
        }

        public ClientSocketHelper Socket { get; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in ViewMode.Files)
            {
                Socket.SendFileAsync(file.File.FullName, (p, l) =>
                {
                    file.UpdateProgress(p);
                });
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
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
                    ViewMode.Files.Add(new TransporterFile(file));
                }
            }
        }
    }
}
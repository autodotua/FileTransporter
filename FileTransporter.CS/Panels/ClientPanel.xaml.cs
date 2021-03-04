using FileTransporter.Util;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// ClientPanel.xaml 的交互逻辑
    /// </summary>
    public partial class ClientPanel : UserControl
    {
        public ClientPanel(ClientSocketHelper socket)
        {
            InitializeComponent();
            Socket = socket;
        }

        public ClientSocketHelper Socket { get; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Socket.SendFileAsync(@"C:\Users\autod\Desktop\Road Rash 2002.zip");
        }
    }
}
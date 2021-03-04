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
    public class LoginPanelViewModel
    {
        public ushort ServerPort { get; set; } = 8080;
        public ushort ClientPort { get; set; } = 8080;
        public string ClientConnectAddress { get; set; } = "127.0.0.1";
        public string ServerPassword { get; set; }
        public string ClientPassword { get; set; }
    }

    public partial class LoginPanel : UserControl
    {
        public LoginPanelViewModel ViewModel { get; } = new LoginPanelViewModel();

        public LoginPanel()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private TaskCompletionSource<ServerSocketHelper> tcsServer = new TaskCompletionSource<ServerSocketHelper>();
        private TaskCompletionSource<ClientSocketHelper> tcsClient = new TaskCompletionSource<ClientSocketHelper>();

        public Task WaitForServerOpenedAsync()
        {
            return tcsServer.Task;
        }

        public Task<ClientSocketHelper> WaitForClientOpenedAsync()
        {
            return tcsClient.Task;
        }

        private async void ClientButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                ClientSocketHelper helper = new ClientSocketHelper();
                await helper.StartAsync(ViewModel.ClientConnectAddress, ViewModel.ClientPort, ViewModel.ClientPassword);
                tcsClient.SetResult(helper);
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接失败：" + ex.Message);
                (sender as Button).IsEnabled = true;
            }
        }

        private void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            try
            {
                ServerSocketHelper helper = new ServerSocketHelper();
                helper.Start(ViewModel.ServerPort, ViewModel.ServerPassword);
                tcsServer.SetResult(helper);
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message);
                (sender as Button).IsEnabled = true;
            }
        }
    }
}
using FileTransporter.FileSimpleSocket;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
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
using FileTransporter.FileSimpleSocket;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
    public partial class LoginPanel : UserControl
    {
        private TaskCompletionSource<ClientSocketHelper> tcsClient = new TaskCompletionSource<ClientSocketHelper>();
        private TaskCompletionSource<ServerSocketHelper> tcsServer = new TaskCompletionSource<ServerSocketHelper>();

        public LoginPanel()
        {
            if (Config.Instance.Login == null)
            {
                Config.Instance.Login = new LoginInfo();
            }
            ViewModel = Config.Instance.Login;

            InitializeComponent();
            DataContext = ViewModel;
        }

        public LoginInfo ViewModel { get; }

        public Task<ClientSocketHelper> WaitForClientOpenedAsync()
        {
            return tcsClient.Task;
        }

        public Task<ServerSocketHelper> WaitForServerOpenedAsync()
        {
            return tcsServer.Task;
        }

        private async void ClientButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            Config.Instance.Save();
            try
            {
                ClientSocketHelper helper = new ClientSocketHelper();
                await helper.StartAsync(ViewModel.ClientConnectAddress, ViewModel.ClientPort, ViewModel.ClientPassword, ViewModel.ClientName);
                tcsClient.SetResult(helper);
            }
            catch (Exception ex)
            {
                await MainWindow.Current.ShowMessageAsync("连接失败：", ex);
                (sender as Button).IsEnabled = true;
            }
        }

        private async void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            Config.Instance.Save();
            try
            {
                ServerSocketHelper helper = new ServerSocketHelper();
                helper.Start(ViewModel.ServerPort, ViewModel.ServerPassword);
                tcsServer.SetResult(helper);
            }
            catch (Exception ex)
            {
                await MainWindow.Current.ShowMessageAsync("启动失败：", ex);
                (sender as Button).IsEnabled = true;
            }
        }
    }
}
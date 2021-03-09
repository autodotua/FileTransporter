using AutoMapper;
using FileTransporter.FileSimpleSocket;
using FileTransporter.Model;
using FzLib.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    /// FileBrowserPanel.xaml 的交互逻辑
    /// </summary>
    public partial class FileBrowserPanel : UserControl
    {
        public static readonly DependencyProperty SocketProperty = DependencyProperty.Register(
  nameof(Socket),
   typeof(ClientSocketHelper),
   typeof(FileBrowserPanel),
   new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
   {
       (s as FileBrowserPanel).ViewModel.Socket = e.NewValue as ClientSocketHelper;
   })));

        public FileBrowserPanel()
        {
            DataContext = ViewModel;
            InitializeComponent();
        }

        public event EventHandler DownloadStarted;

        public ClientSocketHelper Socket
        {
            get => GetValue(SocketProperty) as ClientSocketHelper;
            set => SetValue(SocketProperty, value);
        }

        public FileBrowserPanelViewModel ViewModel { get; } = new FileBrowserPanelViewModel();

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(ViewModel.SelectedFile != null);
            try
            {
                DownloadStarted?.Invoke(this, new EventArgs());
                await Socket.Download(ViewModel.SelectedFile.Path);
            }
            catch (Exception ex)
            {
                await MainWindow.Current.ShowMessageAsync("下载失败", ex);
            }
        }

        private async void GotoButton_Click(object sender, RoutedEventArgs e)
        {
            btnGoto.IsEnabled = false;
            try
            {
                await LoadFilesAsync();
            }
            catch (Exception ex)
            {
                await MainWindow.Current.ShowMessageAsync("跳转失败", ex);
            }
            finally
            {
                btnGoto.IsEnabled = true;
            }
        }

        private async void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (ViewModel.SelectedFile != null)
                {
                    if (ViewModel.SelectedFile.IsDir)
                    {
                        ViewModel.Path = ViewModel.SelectedFile.Path;
                        try
                        {
                            await LoadFilesAsync();
                        }
                        catch (Exception ex)
                        {
                            await MainWindow.Current.ShowMessageAsync("打开远程文件夹失败", ex);
                        }
                    }
                }
            }
        }

        private async Task LoadFilesAsync()
        {
            ViewModel.Files.Clear();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<RemoteFile, FileListInfo>());
            IMapper map = config.CreateMapper();
            var files = await Socket.GetRemoteFileListAsync(ViewModel.Path);
            ViewModel.Path = files.Path;
            foreach (var file in files.Files)
            {
                ViewModel.Files.Add(map.Map<FileListInfo>(file));
            }
        }

        private async void UpButton_Click(object sender, RoutedEventArgs e)
        {
            await btnUp.PauseBindingAsync(IsEnabledProperty, async () =>
          {
              btnUp.IsEnabled = false;
              try
              {
                  ViewModel.Path = Directory.GetParent(ViewModel.Path)?.FullName ?? "";
                  await LoadFilesAsync();
              }
              catch (Exception ex)
              {
                  await MainWindow.Current.ShowMessageAsync("返回上级失败", ex);
              }
              finally
              {
                  btnUp.IsEnabled = true;
              }
          });
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Files.Count == 0)
            {
                try
                {
                    await LoadFilesAsync();
                }
                catch (Exception ex)
                {
                    await MainWindow.Current.ShowMessageAsync("加载远程文件失败", ex);
                }
            }
        }
    }

    public class NotNullAndFalse2BoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values[0] != null && !(bool)values[1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
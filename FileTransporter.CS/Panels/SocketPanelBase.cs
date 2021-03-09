using FileTransporter.FileSimpleSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
    public class SocketPanelBase : UserControl
    {
        public SocketPanelBase()
        {
            Unloaded += SocketPanelBase_Unloaded;
        }

        private void SocketPanelBase_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Socket.Running)
            {
                try
                {
                    Socket.Close();
                }
                catch (Exception ex)
                {
                    App.Log(LogLevel.Error, "关闭Socket失败", ex);
                }
            }
        }

        public SocketHelperBase Socket { get; set; }
    }
}
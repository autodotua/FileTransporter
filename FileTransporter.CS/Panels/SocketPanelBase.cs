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
        public SocketHelperBase Socket { get; set; }
    }
}
using FileTransporter.FileSimpleSocket;
using System.Collections.ObjectModel;

namespace FileTransporter.Panels
{
    public class ClientPanelViewModel
    {
        public ClientPanelViewModel(ClientSocketHelper socket)
        {
            Socket = socket;
        }

        public ClientSocketHelper Socket { get; }
    }
}
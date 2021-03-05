using FileTransporter.FileSimpleSocket;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using FzLib.Extension;
using System.ComponentModel;

namespace FileTransporter.Panels
{
    public class ServerPanelViewModel : INotifyPropertyChanged
    {
        public ServerSocketHelper Socket { get; }
        private SimpleSocketSession<SocketData> selectedSession;

        public event PropertyChangedEventHandler PropertyChanged;

        public SimpleSocketSession<SocketData> SelectedSession
        {
            get => selectedSession;
            set => this.SetValueAndNotify(ref selectedSession, value, nameof(SelectedSession));
        }

        public ServerPanelViewModel(ServerSocketHelper socket)
        {
            Socket = socket;
        }
    }
}
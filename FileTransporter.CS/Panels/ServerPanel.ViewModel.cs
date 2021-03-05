using FileTransporter.FileSimpleSocket;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using FzLib.Extension;
using System.Collections.Generic;
using System.ComponentModel;

namespace FileTransporter.Panels
{
    public class ServerPanelViewModel : INotifyPropertyChanged
    {
        public ServerPanelViewModel(ServerSocketHelper socket)
        {
            Socket = socket;
            SelectedSession = Sessions.Count == 0 ? null : Sessions[0];
            socket.Server.SessionsChanged += Server_SessionsChanged;
        }

        private void Server_SessionsChanged(object sender, CollectionChangeEventArgs e)
        {
            this.Notify(nameof(Sessions));
            if (e.Action == CollectionChangeAction.Remove && e.Element == SelectedSession || SelectedSession == null)
            {
                SelectedSession = Sessions.Count == 0 ? null : Sessions[0];
            }
        }

        public ServerSocketHelper Socket { get; }
        private SimpleSocketSession<SocketData> selectedSession;

        public event PropertyChangedEventHandler PropertyChanged;

        public SimpleSocketSession<SocketData> SelectedSession
        {
            get => selectedSession;
            set => this.SetValueAndNotify(ref selectedSession, value, nameof(SelectedSession));
        }

        public IReadOnlyList<SimpleSocketSession<SocketData>> Sessions => Socket.Server.Sessions;
    }
}
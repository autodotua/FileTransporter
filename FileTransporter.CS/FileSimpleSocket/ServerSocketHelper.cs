using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static FileTransporter.Model.SocketDataType;
using static FileTransporter.SimpleSocket.SimpleSocketUtility;

namespace FileTransporter.FileSimpleSocket
{
    public class ServerSocketHelper : SocketHelperBase
    {
        public SimpleSocketServer<SocketData> Server { get; private set; }

        public void Start(ushort port, string password)
        {
            Debug.Assert(!Started);
            Debug.Assert(!Closed);
            Debug.Assert(port > 0);
            Server = new SimpleSocketServer<SocketData>();
            if (!string.IsNullOrEmpty(password))
            {
                Server.SetPassword(password);
            }
            Server.SetPassword(password);
            Server.Start("0.0.0.0", port);
            Server.ReceivedData += Server_ReceivedData;
            Started = true;
        }

        private void Server_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            Log(LogLevel.Debug, "服务器接收到新数据，类型为" + e.Data.Action);
            switch (e.Data.Action)
            {
                case SocketDataAction.CheckRequest:
                    VerifyPassword(e.Session);
                    break;

                case SocketDataAction.FileSendRequest:
                    ReceiveFile(e.Session, e.Data.Get<FileHead>());
                    break;
            }
        }

        private void VerifyPassword(SimpleSocketSession<SocketData> session)
        {
            var resp = new SocketData(Response, SocketDataAction.CheckResponse);
            Send(session, resp);
        }

        public new Task SendFileAsync(SimpleSocketSession<SocketData> session, string path, Action<TransportProgress> progress = null)
        {
            return base.SendFileAsync(session, path, progress);
        }

        private void Send(SimpleSocketSession<SocketData> session, SocketData data)
        {
            session.Send(data);
        }
    }

    public class TransportProgress
    {
        public TransportProgress(long totalLength, long currentLength)
        {
            TotalLength = totalLength;
            CurrentLength = currentLength;
        }

        public long TotalLength { get; private set; }
        public long CurrentLength { get; private set; }
        public bool Cancel { get; set; }
    }
}
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
        private SimpleSocketServer<SocketData> server;

        public void Start(ushort port, string password)
        {
            Debug.Assert(!Started);
            Debug.Assert(!Closed);
            Debug.Assert(port > 0);
            server = new SimpleSocketServer<SocketData>();
            if (!string.IsNullOrEmpty(password))
            {
                server.SetPassword(password);
            }
            server.SetPassword(password);
            server.Start("0.0.0.0", port);
            server.ReceivedData += Server_ReceivedData;
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

        public new Task SendFileAsync(SimpleSocketSession<SocketData> session, string path, Action<long, long> progress,
            Func<bool> isCanceled = null)
        {
            return base.SendFileAsync(session, path, progress, isCanceled);
        }

        private void Send(SimpleSocketSession<SocketData> session, SocketData data)
        {
            session.Send(data);
        }
    }
}
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using System.Diagnostics;
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
            if (!string.IsNullOrEmpty(password))
            {
                this.password = CreateMD5(password);
            }
            server = new SimpleSocketServer<SocketData>();
            server.Start("0.0.0.0", port);
            server.ReceivedData += Server_ReceivedData;
            Started = true;
        }

        private void Server_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            Log(LogLevel.Debug, "服务器接收到新数据，类型为" + e.Data.Action);
            switch (e.Data.Action)
            {
                case SocketDataAction.PasswordRequest:
                    VerifyPassword(e.Session, e.Data);
                    break;

                case SocketDataAction.FileSendRequest:
                    ReceiveFile(e.Session, e.Data.Get<FileHead>());
                    break;
            }
        }

        private void VerifyPassword(SimpleSocketSession<SocketData> session, SocketData data)
        {
            string password = data.GetString();
            var resp = new SocketData(Response, SocketDataAction.PasswordResponse, password == this.password);
            Send(session, resp);
        }

        private void Send(SimpleSocketSession<SocketData> session, SocketData data)
        {
            session.Send(data);
        }
    }
}
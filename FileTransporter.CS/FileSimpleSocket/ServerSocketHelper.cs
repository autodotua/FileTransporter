using FileTransporter.Dto;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using static FileTransporter.Dto.SocketDataType;
using static FileTransporter.SimpleSocket.SimpleSocketUtility;

namespace FileTransporter.FileSimpleSocket
{
    public class ServerSocketHelper : SocketHelperBase
    {
        public SimpleSocketServer<SocketData> Server { get; private set; }

        public new Task SendFileAsync(SimpleSocketSession<SocketData> session, string path, Action<TransportProgress> progress = null)
        {
            return base.SendFileAsync(session, path, progress);
        }

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

        private void Send(SimpleSocketSession<SocketData> session, SocketData data)
        {
            session.Send(data);
        }

        private void SendFileList(SimpleSocketSession<SocketData> session, FileListRequest request)
        {
            string path = request.Path;
            if (string.IsNullOrEmpty(path))
            {
                path = Directory.GetCurrentDirectory();
            }
            List<RemoteFile> files = new List<RemoteFile>();
            foreach (var dir in new DirectoryInfo(path).EnumerateFileSystemInfos())
            {
                var file = new RemoteFile(dir, true);
                files.Add(file);
            }
            var data = new FileListResponse() { Files = files.OrderByDescending(p => p.IsDir).ToList(), Path = path };
            SocketData resp = new SocketData(Response, SocketDataAction.FileListResponse, data);
            Send(session, resp);
        }

        private async void Server_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            Log(LogLevel.Debug, "服务器接收到新数据，类型为" + e.Data.Action);
            switch (e.Data.Action)
            {
                case SocketDataAction.CheckRequest:
                    VerifyPassword(e.Session);
                    break;

                case SocketDataAction.FileSendRequest:
                    await ReceiveFileAsync(e.Session, e.Data.Get<RemoteFile>());
                    break;

                case SocketDataAction.FileListRequest:
                    SendFileList(e.Session, e.Data.Get<FileListRequest>());
                    break;

                case SocketDataAction.FileDownloadRequest:
                    await SendFileAsync(e.Session, e.Data.Get<FileDownloadRequest>().Path);
                    break;
            }
        }

        private void VerifyPassword(SimpleSocketSession<SocketData> session)
        {
            var resp = new SocketData(Response, SocketDataAction.CheckResponse);
            Send(session, resp);
        }
    }
}
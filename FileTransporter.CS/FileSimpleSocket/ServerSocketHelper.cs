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

        public override void Close()
        {
            Server.Close();
            Running = false;
        }

        public new Task SendFileAsync(SimpleSocketSession<SocketData> session, string path, Guid? id)
        {
            return base.SendFileAsync(session, path, id);
        }

        public void Start(ushort port, string password)
        {
            Debug.Assert(!Running);
            Debug.Assert(port > 0);
            Server = new SimpleSocketServer<SocketData>();
            if (!string.IsNullOrEmpty(password))
            {
                Server.SetPassword(password);
            }
            Server.SetPassword(password);
            Server.Start("0.0.0.0", port);
            Server.ReceivedData += Server_ReceivedData;
            Running = true;
        }

        private void Send(SimpleSocketSession<SocketData> session, SocketData data)
        {
            session.Send(data);
        }

        private void SendFileList(SimpleSocketSession<SocketData> session, FileListRequest request)
        {
            string path = request.Path;
            List<RemoteFile> files = new List<RemoteFile>();
            if (string.IsNullOrEmpty(path))
            {
                files = DriveInfo.GetDrives().Select(p => p.RootDirectory).Select(p => new RemoteFile(p, false)).ToList();
            }
            else
            {
                foreach (var dir in new DirectoryInfo(path).EnumerateFileSystemInfos())
                {
                    var file = new RemoteFile(dir, true);
                    files.Add(file);
                }
            }
            var data = new FileListResponse() { Files = files.OrderByDescending(p => p.IsDir).ToList(), Path = path };
            SocketData resp = new SocketData(Response, SocketDataAction.FileListResponse, data);
            Send(session, resp);
        }

        private async void Server_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            Log(LogLevel.Debug, "服务器接收到新数据，类型为" + e.Data.Action);
            try
            {
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
                        await SendFileAsync(e.Session, e.Data.Get<FileDownloadRequest>().Path, null);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                App.Log(LogLevel.Warn, "处理请求操作被取消：" + e.Data.Action);
            }
            catch (Exception ex)
            {
                TrySendError(e.Session, ex);
            }
        }

        private void VerifyPassword(SimpleSocketSession<SocketData> session)
        {
            var resp = new SocketData(Response, SocketDataAction.CheckResponse);
            Send(session, resp);
        }
    }
}
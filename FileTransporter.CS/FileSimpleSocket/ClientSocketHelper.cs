using FileTransporter.Dto;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static FileTransporter.Dto.SocketDataType;
using static FileTransporter.SimpleSocket.SimpleSocketUtility;

namespace FileTransporter.FileSimpleSocket
{
    public class ClientSocketHelper : SocketHelperBase
    {
        public SimpleSocketClient<SocketData> Client { get; private set; }

        public async Task StartAsync(string address, ushort port, string password, string name)
        {
            Debug.Assert(!Started);
            Debug.Assert(!Closed);
            Debug.Assert(port > 0);
            Debug.Assert(address != null);

            Client = new SimpleSocketClient<SocketData>();

            if (!string.IsNullOrEmpty(password))
            {
                Client.SetPassword(password);
            }
            await Client.StartAsync(address, port);
            try
            {
                await CheckAsync(name);
            }
            catch
            {
                Client.Close();
                Closed = true;
                throw;
            }
            Client.Session.ReceivedData += Session_ReceivedData;
            Started = true;
        }

        private async void Session_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            Log(LogLevel.Debug, "客户端接收到新数据，类型为" + e.Data.Action);
            try
            {
                switch (e.Data.Action)
                {
                    case SocketDataAction.FileSendRequest:
                        await ReceiveFileAsync(e.Session, e.Data.Get<RemoteFile>());
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

        public Task SendFileAsync(string path, Guid? id)
        {
            return SendFileAsync(Client.Session, path, id);
        }

        private async Task CheckAsync(string name)
        {
            var data = new SocketData(Request, SocketDataAction.CheckRequest);
            data.Name = name;
            Client.Session.Send(data);
            await Client.Session.WaitForNextReceiveAsync(Config.Instance.CommandTimeout);
        }

        public async Task<FileListResponse> GetRemoteFileListAsync(string root)
        {
            var data = new FileListRequest() { Path = root };
            var request = new SocketData(Request, SocketDataAction.FileListRequest, data);
            Client.Session.Send(request);
            var resp = await Client.Session.WaitForNextReceiveAsync(Config.Instance.CommandTimeout);
            return resp.Get<FileListResponse>();
        }

        public async Task Download(string path)
        {
            IsDownloading = true;
            try
            {
                var data = new FileDownloadRequest() { Path = path };
                var request = new SocketData(Request, SocketDataAction.FileDownloadRequest, data);
                Client.Session.Send(request);
                var resp = await Client.Session.WaitForNextReceiveAsync(Config.Instance.CommandTimeout, true);
                await ReceiveFileAsync(Client.Session, resp.Get<RemoteFile>());
            }
            catch (Exception ex)
            {
                App.Log(LogLevel.Error, "下载文件失败", ex);
                throw;
            }
            finally
            {
                IsDownloading = false;
            }
        }
    }
}
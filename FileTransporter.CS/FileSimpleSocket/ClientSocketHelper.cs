using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static FileTransporter.Model.SocketDataType;
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

        private void Session_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            switch (e.Data.Action)
            {
                case SocketDataAction.FileSendRequest:
                    ReceiveFile(e.Session, e.Data.Get<FileHead>());
                    break;
            }
            Log(LogLevel.Debug, "客户端接收到新数据，类型为" + e.Data.Action);
        }

        public Task SendFileAsync(string path, Action<TransportProgress> progress = null)
        {
            return SendFileAsync(Client.Session, path, progress);
        }

        private async Task CheckAsync(string name)
        {
            var data = new SocketData(Request, SocketDataAction.CheckRequest);
            data.Name = name;
            Client.Session.Send(data);
            await Client.Session.WaitForNextReceiveAsync(Config.Instance.CommandTimeout);
        }
    }
}
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
        private SimpleSocketClient<SocketData> client;

        public async Task StartAsync(string address, ushort port, string password)
        {
            Debug.Assert(!Started);
            Debug.Assert(!Closed);
            Debug.Assert(port > 0);
            Debug.Assert(address != null);

            client = new SimpleSocketClient<SocketData>();

            if (!string.IsNullOrEmpty(password))
            {
                client.SetPassword(password);
            }
            await client.StartAsync(address, port);
            try
            {
                await CheckAsync();
            }
            catch
            {
                client.Close();
                Closed = true;
                throw;
            }
            client.Session.ReceivedData += Session_ReceivedData;
            Started = true;
        }

        private void Session_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            Log(LogLevel.Debug, "客户端接收到新数据，类型为" + e.Data.Action);
        }

        public Task SendFileAsync(string path, Action<long, long> progress,
            Func<bool> isCanceled = null)
        {
            return SendFileAsync(client.Session, path, progress, isCanceled);
        }

        private async Task CheckAsync()
        {
            var data = new SocketData(Request, SocketDataAction.CheckRequest);
            client.Session.Send(data);
            await client.Session.WaitForNextReceiveAsync(Config.Instance.CommandTimeout);
        }
    }
}
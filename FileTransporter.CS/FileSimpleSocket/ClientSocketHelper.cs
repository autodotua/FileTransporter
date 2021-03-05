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

            await client.StartAsync(address, port);
            if (!string.IsNullOrEmpty(password))
            {
                this.password = CreateMD5(password);
                try
                {
                    await VerifyPasswordAsync();
                }
                catch
                {
                    client.Close();
                    Closed = true;
                    throw;
                }
            }
            client.Session.ReceivedData += Session_ReceivedData;
            Started = true;
        }

        private void Session_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            Log(LogLevel.Debug, "客户端接收到新数据，类型为" + e.Data.Action);
        }

        //private Task<SocketData> SendAndWaitForResponseAysnc(SocketData data, int timeout = 2000)
        //{
        //    return base.SendAndWaitForResponseAysnc(client.Session, data, timeout);
        //}

        public Task SendFileAsync(string path, Action<long, long> progress)
        {
            return SendFileAsync(client.Session, path, progress);
        }

        private async Task VerifyPasswordAsync()
        {
            if (string.IsNullOrEmpty(password))
            {
                return;
            }
            var data = new SocketData(Request, SocketDataAction.PasswordRequest, password);
            //var resp = await SendAndWaitForResponseAysnc(data);
            client.Session.Send(data);
            var resp = await client.Session.WaitForNextReceiveAsync(Config.Instance.CommandTimeout);
            if (!resp.GetBool())
            {
                throw new Exception("密码错误");
            }
        }
    }
}
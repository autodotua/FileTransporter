using FileTransporter.SimpleSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileTransporter.Util
{
    public class SocketHelper
    {
        private SimpleSocketServer<SocketData> server;
        private SimpleSocketClient<SocketData> client;
        private string password;
        private int type = 0;

        public void StartServer(ushort port, string password)
        {
            Debug.Assert(type == 0);
            Debug.Assert(port > 0);
            if (!string.IsNullOrEmpty(password))
            {
                this.password = CreateMD5(password);
            }
            server = new SimpleSocketServer<SocketData>();
            server.Start("0.0.0.0", port);
            server.ReceivedData += Server_ReceivedData;
            type = 1;
        }

        private void Server_ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
        {
            switch (e.Data.Action)
            {
                case SocketData.Password:
                    VerifyPassword(e.Session, e.Data);
                    break;
            }
        }

        public async Task StartClientAsync(string address, ushort port, string password)
        {
            Debug.Assert(type == 0);
            Debug.Assert(port > 0);
            Debug.Assert(address != null);

            client = new SimpleSocketClient<SocketData>();
            type = 2;

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
                    throw;
                }
            }
        }

        private void VerifyPassword(SimpleSocketSession<SocketData> session, SocketData data)
        {
            string password = data.GetString();
            var resp = new SocketData(SocketData.SocketDataType.Response, SocketData.Password);
            resp.SetBool(password == this.password);
            Send(session, resp);
        }

        private async Task VerifyPasswordAsync()
        {
            if (string.IsNullOrEmpty(password))
            {
                return;
            }
            var data = new SocketData(SocketData.SocketDataType.Request, SocketData.Password);
            data.SetString(password);
            var resp = await SendAndWaitForResponseAysnc(data);
            if (!resp.GetBool())
            {
                throw new Exception("密码错误");
            }
        }

        private void Send(SimpleSocketSession<SocketData> session, SocketData data)
        {
            session.Send(data);
        }

        private Task<SocketData> SendAndWaitForResponseAysnc(SocketData data, int timeout = 2000)
        {
            TaskCompletionSource<SocketData> tsc = new TaskCompletionSource<SocketData>();
            Debug.Assert(data.Type == SocketData.SocketDataType.Request);
            client.Session.Send(data);
            client.Session.ReceivedData += ReceivedData;
            Task.Delay(timeout).ContinueWith(p =>
            {
                if (!tsc.Task.IsCompleted)
                {
                    client.Session.ReceivedData -= ReceivedData;
                    tsc.SetException(new TimeoutException());
                }
            });
            return tsc.Task;

            void ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
            {
                client.Session.ReceivedData -= ReceivedData;
                tsc.SetResult(e.Data);
            }
        }

        private static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }

    [Serializable]
    public class SocketData : SimpleSocketDataBase
    {
        public SocketDataType Type { get; set; }
        public string Action { get; set; }
        public byte[] Data { get; set; }

        public string GetString()
        {
            return Encoding.UTF8.GetString(Data);
        }

        public void SetString(string message)
        {
            Data = Encoding.UTF8.GetBytes(message);
        }

        public bool GetBool()
        {
            return Data[0] == (byte)1;
        }

        public void SetBool(bool value)
        {
            Data = new byte[1];
            Data[0] = value ? 1 : 0;
        }

        public const string Password = "Password";

        public SocketData()
        {
        }

        public SocketData(SocketDataType type, string action, byte[] data = null) : this()
        {
            Type = type;
            Action = action;
            Data = data;
        }

        public enum SocketDataType
        {
            General = 0,
            Request = 1,
            Response = 2
        }
    }
}
using FileTransporter.SimpleSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileTransporter.Util
{
    public class SocketHelperBase
    {
        protected const int FileBufferLength = 1024 * 1024 * 10;//10M
        protected string password;
        public bool Started { get; protected set; }
        public bool Closed { get; protected set; }
        protected HashSet<SimpleSocketSession<SocketData>> PauseReceiveDataSessions { get; } = new HashSet<SimpleSocketSession<SocketData>>();
        private Dictionary<SimpleSocketSession<SocketData>, TaskCompletionSource> sesssion2TaskDic = new Dictionary<SimpleSocketSession<SocketData>, TaskCompletionSource>();

        public static string CreateMD5(string input)
        {
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        protected Task<SocketData> SendAndWaitForResponseAysnc(SimpleSocketSession<SocketData> session, SocketData data, int timeout = 2000)
        {
            TaskCompletionSource<SocketData> tcs = new TaskCompletionSource<SocketData>();
            Debug.Assert(data.Type == SocketData.SocketDataType.Request);
            session.Send(data);
            session.ReceivedData += ReceivedData;
            PauseReceiveDataSessions.Add(session);
            Task.Delay(timeout).ContinueWith(p =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    session.ReceivedData -= ReceivedData;
                    tcs.SetException(new TimeoutException());
                }
            });
            return tcs.Task;

            void ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
            {
                session.ReceivedData -= ReceivedData;
                PauseReceiveDataSessions.Remove(session);
                tcs.SetResult(e.Data);
            }
        }

        private Guid SendFileHead(SimpleSocketSession<SocketData> session, string path)
        {
            Guid id = Guid.NewGuid();
            FileInfo file = new FileInfo(path);
            FileHead head = new FileHead()
            {
                ID = id,
                Length = file.Length,
                Name = file.Name
            };
            session.Send(new SocketData(SocketData.SocketDataType.General, SocketData.FileHead, head));
            return id;
        }

        private void SendFileBuffer(SimpleSocketSession<SocketData> session, string path, long position, Guid id)
        {
            using var fs = new FileStream(path, FileMode.Open);
            fs.Position = position;
            var array = new byte[FileBufferLength];
            int length = fs.Read(array, 0, FileBufferLength);
            array = length == FileBufferLength ? array : array[0..length];
            FileBufferResponse buffer = new FileBufferResponse()
            {
                Content = array,
                ID = id,
                Length = array.Length,
                Position = position,
            };
            SocketData data = new SocketData(SocketData.SocketDataType.Response, SocketData.FileBufferResponse, buffer);
            session.Send(data);
        }

        protected Task SendFileAsync(SimpleSocketSession<SocketData> session, string path)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            var id = SendFileHead(session, path);
            session.ReceivedData += ReceivedData;
            PauseReceiveDataSessions.Add(session);
            return tcs.Task;

            void ReceivedData(object sender, DataReceivedEventArgs<SocketData> e)
            {
                Debug.Assert(e.Data.Action == SocketData.FileBufferRequest);
                var request = e.Data.Get<FileBufferRequest>();
                if (request.End)
                {
                    session.ReceivedData -= ReceivedData;
                    PauseReceiveDataSessions.Remove(session);
                    tcs.SetResult();
                }
                else
                {
                    SendFileBuffer(session, path, request.Position, id);
                }
            }
        }
    }

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
            if (PauseReceiveDataSessions.Contains(e.Session))
            {
                return;
            }
            switch (e.Data.Action)
            {
                case SocketData.Password:
                    VerifyPassword(e.Session, e.Data);
                    break;

                case SocketData.FileHead:
                    BeginReceiveFile(e.Session, e.Data.Get<FileHead>());
                    break;
            }
        }

        private async void BeginReceiveFile(SimpleSocketSession<SocketData> session, FileHead file)
        {
            using var fs = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), file.Name), FileMode.Create);
            long bufferCount = file.Length / FileBufferLength + (file.Length % FileBufferLength == 0 ? 0 : 1);
            for (long i = 0; i < bufferCount; i++)
            {
                FileBufferRequest request = new FileBufferRequest()
                {
                    ID = file.ID,
                    Position = i * FileBufferLength,
                    End = false,
                };

                SocketData data = new SocketData(SocketData.SocketDataType.Request, SocketData.FileBufferRequest, request);

                var resp = await SendAndWaitForResponseAysnc(session, data, 20000);
                switch (resp.Action)
                {
                    default:
                    case SocketData.FileBufferResponse:
                        fs.Write(resp.Get<FileBufferResponse>().Content);
                        break;

                    case SocketData.Error:
                        throw new Exception(resp.GetString());
                }
            }
            fs.Flush();
            session.Send(new SocketData(SocketData.SocketDataType.General,
                SocketData.FileBufferRequest,
                new FileBufferRequest()
                {
                    ID = file.ID,
                    End = true
                }));
        }

        private void VerifyPassword(SimpleSocketSession<SocketData> session, SocketData data)
        {
            string password = data.GetString();
            var resp = new SocketData(SocketData.SocketDataType.Response, SocketData.Password, password == this.password);
            Send(session, resp);
        }

        private void Send(SimpleSocketSession<SocketData> session, SocketData data)
        {
            session.Send(data);
        }
    }

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

            Started = true;
        }

        private Task<SocketData> SendAndWaitForResponseAysnc(SocketData data, int timeout = 2000)
        {
            return base.SendAndWaitForResponseAysnc(client.Session, data, timeout);
        }

        public Task SendFileAsync(string path)
        {
            return SendFileAsync(client.Session, path);
        }

        private async Task VerifyPasswordAsync()
        {
            if (string.IsNullOrEmpty(password))
            {
                return;
            }
            var data = new SocketData(SocketData.SocketDataType.Request, SocketData.Password, password);
            var resp = await SendAndWaitForResponseAysnc(data);
            if (!resp.GetBool())
            {
                throw new Exception("密码错误");
            }
        }
    }

    [Serializable]
    public class SocketData : SimpleSocketDataBase
    {
        public SocketDataType Type { get; set; }
        public string Action { get; set; }
        public object Data { get; set; }

        public string GetString()
        {
            return Data as string;
        }

        public bool GetBool()
        {
            return Convert.ToBoolean(Data);
        }

        public T Get<T>() where T : class => Data as T;

        public const string Password = "Password";
        public const string FileHead = "FileHead";
        public const string FileBufferResponse = "FileBufferResponse";
        public const string FileBufferRequest = "NextFileBufferRequest";
        public const string Error = "Error";

        public SocketData()
        {
        }

        public SocketData(SocketDataType type, string action, object data = null) : this()
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

    [Serializable]
    public class FileBufferResponse
    {
        public long Length { get; set; }
        public long Position { get; set; }
        public Guid ID { get; set; }
        public byte[] Content { get; set; }
    }

    [Serializable]
    public class FileHead
    {
        public long Length { get; set; }
        public string Name { get; set; }
        public Guid ID { get; set; }
    }

    [Serializable]
    public class FileBufferRequest
    {
        public Guid ID { get; set; }
        public long Position { get; set; }
        public bool End { get; set; }
    }
}
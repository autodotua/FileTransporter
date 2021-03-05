using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static FileTransporter.Model.SocketDataType;
using static FileTransporter.SimpleSocket.SimpleSocketUtility;

namespace FileTransporter.FileSimpleSocket
{
    public class SocketHelperBase
    {
        protected string password;
        public bool Started { get; protected set; }
        public bool Closed { get; protected set; }

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

        private FileHead SendFileHead(SimpleSocketSession<SocketData> session, string path)
        {
            Guid id = Guid.NewGuid();
            FileInfo file = new FileInfo(path);
            FileHead head = new FileHead()
            {
                ID = id,
                Length = file.Length,
                Name = file.Name
            };
            session.Send(new SocketData(General, SocketDataAction.FileSendRequest, head));

            Log(LogLevel.Info, "发送文件头");
            return head;
        }

        private void SendFileBuffer(SimpleSocketSession<SocketData> session, string path, long position, Guid id)
        {
            var bufferLength = Config.Instance.FileBufferLength;
            using var fs = new FileStream(path, FileMode.Open);
            fs.Position = position;
            var array = new byte[bufferLength];
            int length = fs.Read(array, 0, bufferLength);
            array = length == bufferLength ? array : array[0..length];
            FileBufferResponse buffer = new FileBufferResponse()
            {
                Content = array,
                ID = id,
                Length = array.Length,
                Position = position,
            };
            SocketData data = new SocketData(Response, SocketDataAction.FileBufferResponse, buffer);
            session.Send(data);
            Log(LogLevel.Info, $"开始发送长度为{array.Length}的文件块");
        }

        protected async Task SendFileAsync(SimpleSocketSession<SocketData> session, string path, Action<long, long> progress)
        {
            var file = SendFileHead(session, path);
            while (true)
            {
                var request = await session.WaitForNextReceiveAsync(Config.Instance.FileTimeout);

                var data = request.Get<FileBufferRequest>();
                Log(LogLevel.Info, $"收到发送文件{data.Position}请求");
                if (data.End)
                {
                    progress?.Invoke(file.Length, file.Length);
                    Log(LogLevel.Info, "发送文件完成");
                    return;
                }
                progress?.Invoke(data.Position, file.Length);
                SendFileBuffer(session, path, data.Position, file.ID);
            }
        }

        protected async void ReceiveFile(SimpleSocketSession<SocketData> session, FileHead file)
        {
            var bufferLength = Config.Instance.FileBufferLength;
            Log(LogLevel.Info, "开始接收文件");
            using var fs = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), file.ID.ToString()), FileMode.Create);
            long bufferCount = file.Length / bufferLength + (file.Length % bufferLength == 0 ? 0 : 1);
            for (long i = 0; i < bufferCount; i++)
            {
                FileBufferRequest request = new FileBufferRequest()
                {
                    ID = file.ID,
                    Position = i * bufferLength,
                    End = false,
                };

                SocketData data = new SocketData(Request, SocketDataAction.FileBufferRequest, request);

                //var resp = await SendAndWaitForResponseAysnc(session, data, 20000);
                session.Send(data);
                Log(LogLevel.Info, $"等待接收位置为{request.Position}的文件块");
                var resp = await session.WaitForNextReceiveAsync(Config.Instance.FileTimeout);
                switch (resp.Action)
                {
                    case SocketDataAction.FileBufferResponse:
                        Log(LogLevel.Info, $"接收到长度为{resp.Get<FileBufferResponse>().Length}的文件块");
                        fs.Write(resp.Get<FileBufferResponse>().Content);
                        break;

                    case SocketDataAction.Error:
                        throw new Exception(resp.GetString());

                    default:
                        Log(LogLevel.Warn, $"接收到未知指令：{resp.Action}，期望是{nameof(SocketDataAction.FileBufferResponse)}");
                        break;
                }
            }
            fs.Flush();
            fs.Close();
            Log(LogLevel.Info, "接收文件完成");
            File.Move(file.ID.ToString(), FzLib.IO.FileSystem.GetNoDuplicateFile(file.Name));
            session.Send(new SocketData(General,
                SocketDataAction.FileBufferRequest,
                new FileBufferRequest()
                {
                    ID = file.ID,
                    End = true
                }));
        }
    }
}
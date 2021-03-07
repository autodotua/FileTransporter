using FileTransporter.Dto;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static FileTransporter.Dto.SocketDataType;
using static FileTransporter.SimpleSocket.SimpleSocketUtility;

namespace FileTransporter.FileSimpleSocket
{
    public class SocketHelperBase : INotifyPropertyChanged
    {
        public event EventHandler<TransportFileProgressEventArgs> UploadProgress;

        public event EventHandler<TransportFileProgressEventArgs> DownloadProgress;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Closed { get; protected set; }
        public bool Started { get; protected set; }
        private bool isUploading;

        public bool IsUploading
        {
            get => isUploading;
            protected set
            {
                isUploading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUploading)));
            }
        }

        private bool isDownloading;

        public bool IsDownloading
        {
            get => isDownloading;
            protected set
            {
                isDownloading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDownloading)));
            }
        }

        protected async Task ReceiveFileAsync(SimpleSocketSession<SocketData> session, RemoteFile file)
        {
            try
            {
                IsDownloading = true;
                var bufferLength = Config.Instance.FileBufferLength;
                Log(LogLevel.Info, "开始接收文件");
                string tempFilePath = Path.Combine(Config.Instance.FileReceiveFolder, "temp", file.ID.ToString());
                if (!Directory.Exists(Path.GetDirectoryName(tempFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
                }
                using var fs = new FileStream(tempFilePath, FileMode.Create);
                long bufferCount = file.Length / bufferLength + (file.Length % bufferLength == 0 ? 0 : 1);
                bool canceled = false;
                try
                {
                    TransportFileProgressEventArgs e = new TransportFileProgressEventArgs(session, file, 0);
                    DownloadProgress?.Invoke(this, e);

                    for (long i = 0; i < bufferCount; i++)
                    {
                        FileBufferRequest request = new FileBufferRequest()
                        {
                            ID = file.ID,
                            Position = i * bufferLength,
                            Type = FileRequestType.Next
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

                            case SocketDataAction.FileCanceledResponse:
                                throw new OperationCanceledException();

                            default:
                                Log(LogLevel.Warn, $"接收到未知指令：{resp.Action}，期望是{nameof(SocketDataAction.FileBufferResponse)}");
                                break;
                        }

                        e = new TransportFileProgressEventArgs(session, file, i * bufferLength + resp.Get<FileBufferResponse>().Length);
                        DownloadProgress?.Invoke(this, e);
                        if (e.Cancel)
                        {
                            session.Send(new SocketData(General,
                             SocketDataAction.FileBufferRequest,
                             new FileBufferRequest()
                             {
                                 ID = file.ID,
                                 Type = FileRequestType.Cancel
                             }));
                            throw new OperationCanceledException();
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    canceled = true;
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Warn, $"接收文件失败");
                }
                finally
                {
                    fs.Flush();
                    fs.Close();
                }
                Log(LogLevel.Info, "接收文件完成");
                if (canceled)
                {
                    File.Delete(tempFilePath);
                    var e = new TransportFileProgressEventArgs(session, file, -1) { Cancel = true };
                    DownloadProgress?.Invoke(this, e);
                }
                else
                {
                    string filePath = Path.Combine(Config.Instance.FileReceiveFolder, file.Name);
                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    }
                    File.Move(tempFilePath, FzLib.IO.FileSystem.GetNoDuplicateFile(filePath));
                    session.Send(new SocketData(General,
                        SocketDataAction.FileBufferRequest,
                        new FileBufferRequest()
                        {
                            ID = file.ID,
                            Type = FileRequestType.End
                        }));
                }
            }
            finally
            {
                IsDownloading = false;
            }
        }

        protected virtual async Task SendFileAsync(SimpleSocketSession<SocketData> session, string path, Guid? id)
        {
            IsUploading = true;
            try
            {
                RemoteFile file = SendFileHead(session, path);
                if (id.HasValue)
                {
                    file.ID = id.Value;
                }
                using var fs = new FileStream(path, FileMode.Open);
                while (true)
                {
                    var request = await session.WaitForNextReceiveAsync(Config.Instance.FileTimeout);
                    var data = request.Get<FileBufferRequest>();
                    Log(LogLevel.Info, $"收到发送文件{data.Position}请求");

                    TransportFileProgressEventArgs e;
                    if (data.Type == FileRequestType.End)
                    {
                        e = new TransportFileProgressEventArgs(session, file, file.Length);
                        UploadProgress?.Invoke(this, e);
                        Log(LogLevel.Info, "发送文件完成");
                        return;
                    }
                    if (data.Type == FileRequestType.Cancel)
                    {
                        Log(LogLevel.Info, "发送文件被取消");
                        e = new TransportFileProgressEventArgs(session, file, file.Length) { Cancel = true };
                        UploadProgress?.Invoke(this, e);
                        throw new OperationCanceledException("发送文件被取消");
                    }
                    e = new TransportFileProgressEventArgs(session, file, data.Position);
                    UploadProgress?.Invoke(this, e);
                    if (e.Cancel)
                    {
                        SocketData cancelData = new SocketData(Response, SocketDataAction.FileCanceledResponse, file);
                        session.Send(cancelData);
                        Log(LogLevel.Info, $"发送取消");
                        break;
                    }
                    SendFileBuffer(fs, session, path, data.Position, file.ID);
                }
            }
            finally
            {
                IsUploading = false;
            }
        }

        private void SendFileBuffer(FileStream fs, SimpleSocketSession<SocketData> session, string path, long position, Guid id)
        {
            var bufferLength = Config.Instance.FileBufferLength;

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

        private RemoteFile SendFileHead(SimpleSocketSession<SocketData> session, string path)
        {
            RemoteFile head = new RemoteFile(new FileInfo(path));
            session.Send(new SocketData(General, SocketDataAction.FileSendRequest, head));

            Log(LogLevel.Info, "发送文件头");
            return head;
        }

        protected void TrySendError(SimpleSocketSession<SocketData> session, Exception ex)
        {
            try
            {
                App.Log(LogLevel.Error, "处理请求失败", ex);
                session.Send(new SocketData()
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex2)
            {
                App.Log(LogLevel.Error, "发送错误信息失败", ex2);
            }
        }
    }

    public class TransportFileProgressEventArgs : EventArgs
    {
        public TransportFileProgressEventArgs(SimpleSocketSession<SocketData> session, RemoteFile file, long length)
        {
            Session = session;
            File = file;
            Length = length;
        }

        public bool Cancel { get; set; }
        public RemoteFile File { get; }
        public long Length { get; }
        public SimpleSocketSession<SocketData> Session { get; }
    }
}
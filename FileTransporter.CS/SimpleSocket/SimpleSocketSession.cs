using FzLib.Extension;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FileTransporter.SimpleSocket
{
    public class DataReceivedEventArgs<T> : EventArgs where T : SimpleSocketDataBase, new()
    {
        public DataReceivedEventArgs(SimpleSocketSession<T> session, T data)
        {
            Session = session;
            Data = data;
        }

        public T Data { get; }
        public SimpleSocketSession<T> Session { get; }
    }

    public class SimpleSocketSession<T> : INotifyPropertyChanged where T : SimpleSocketDataBase, new()
    {
        private string password;
        private bool pauseEvent;
        private string remoteName;
        private Socket socket;

        private TaskCompletionSource<T> waitingTcs;

        public event EventHandler Connected;

        public event EventHandler Disconnected;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<DataReceivedEventArgs<T>> ReceivedData;

        public event EventHandler Stopping;

        public string Password
        {
            get => password;
            set => this.SetValueAndNotify(ref password, value, nameof(Password));
        }

        public string RemoteName
        {
            get => remoteName;
            set => this.SetValueAndNotify(ref remoteName, value, nameof(RemoteName));
        }

        public void Initialize(Socket socket)
        {
            try
            {
                this.socket = socket;
                OnConnected();

                SimpleSocketPackage pack = new SimpleSocketPackage();
                socket.BeginReceive(
                    pack.headBuff,
                    0,
                    pack.headLen,
                    SocketFlags.None,
                    new AsyncCallback(ReceiveHeadData),
                    pack);
            }
            catch (Exception ex)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "开始接收数据失败", ex);
            }
        }

        public void Send(T msg)
        {
            msg.Password = Password;
            byte[] data = SimpleSocketUtility.PackLenInfo(SimpleSocketUtility.Serialize(msg));
            Send(data);
        }

        public Task<T> WaitForNextReceiveAsync(int timeout, bool pauseEvent = false)
        {
            if (waitingTcs != null)
            {
                throw new Exception("已经有一个等待正在执行");
            }
            waitingTcs = new TaskCompletionSource<T>();
            var thisTcs = waitingTcs;
            this.pauseEvent = pauseEvent;
            Task.Delay(timeout).ContinueWith(p =>
            {
                if (waitingTcs != null && waitingTcs == thisTcs)
                {
                    waitingTcs.SetException(new TimeoutException("等待回应超时"));
                    waitingTcs = null;
                    this.pauseEvent = false;
                }
            });
            return waitingTcs.Task;
        }

        protected virtual void OnConnected()
        {
            SimpleSocketUtility.Log(LogLevel.Info, "新的会话已连接");
            Connected?.Invoke(this, new EventArgs());
        }

        protected virtual void OnDisconnected()
        {
            SimpleSocketUtility.Log(LogLevel.Info, "会话已断开连接");
            Disconnected?.Invoke(this, new EventArgs());
        }

        protected virtual void OnReciveData(T message)
        {
            TaskCompletionSource<T> tcs = waitingTcs;
            waitingTcs = null;
            if (!message.Success)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "对方返回错误：" + message.Message);
                if (tcs != null)
                {
                    tcs.SetException(new Exception("对方返回错误：" + message.Message));
                }
                return;
            }

            if (!(string.IsNullOrEmpty(message.Password) && string.IsNullOrEmpty(Password)) && Password != message.Password)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "接受到新的数据，密码错误");
                if (tcs != null)
                {
                    tcs.SetException(new VerificationException("接受到新的数据，密码错误"));
                }
                Send(new T() { Password = message.Password, Success = false, Message = "密码错误" });
                return;
            }
            if (string.IsNullOrEmpty(RemoteName))
            {
                if (string.IsNullOrEmpty(message.Name))
                {
                    if (socket.RemoteEndPoint is IPEndPoint ip)
                    {
                        RemoteName = ip.ToString();
                    }
                }
                else
                {
                    RemoteName = message.Name;
                }
            }
            SimpleSocketUtility.Log(LogLevel.Debug, "接受到新的数据");
            if (tcs != null)
            {
                tcs.SetResult(message);
                if (pauseEvent)
                {
                    return;
                }
            }
            ReceivedData?.Invoke(this, new DataReceivedEventArgs<T>(this, message));
        }

        private void ReceiveBodyData(IAsyncResult ar)
        {
            try
            {
                SimpleSocketPackage pack = (SimpleSocketPackage)ar.AsyncState;
                int len = socket.EndReceive(ar);
                if (len > 0)
                {
                    pack.bodyIndex += len;
                    if (pack.bodyIndex < pack.bodyLen)
                    {
                        socket.BeginReceive(pack.bodyBuff,
                            pack.bodyIndex,
                            pack.bodyLen - pack.bodyIndex,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveBodyData),
                            pack);
                    }
                    else
                    {
                        T msg = SimpleSocketUtility.Deserialize<T>(pack.bodyBuff);
                        OnReciveData(msg);

                        pack.ResetData();
                        socket.BeginReceive(
                            pack.headBuff,
                            0,
                            pack.headLen,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveHeadData),
                            pack);
                    }
                }
                else
                {
                    OnDisconnected();
                    Stop();
                }
            }
            catch (Exception ex)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "接收数据主体失败", ex);
            }
        }

        private void ReceiveHeadData(IAsyncResult ar)
        {
            try
            {
                SimpleSocketPackage pack = (SimpleSocketPackage)ar.AsyncState;
                if (socket.Available == 0)
                {
                    OnDisconnected();
                    Stop();
                    return;
                }
                int len = socket.EndReceive(ar);
                if (len > 0)
                {
                    pack.headIndex += len;
                    if (pack.headIndex < pack.headLen)
                    {
                        socket.BeginReceive(
                            pack.headBuff,
                            pack.headIndex,
                            pack.headLen - pack.headIndex,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveHeadData),
                            pack);
                    }
                    else
                    {
                        pack.InitBodyBuff();
                        socket.BeginReceive(pack.bodyBuff,
                            0,
                            pack.bodyLen,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveBodyData),
                            pack);
                    }
                }
                else
                {
                    OnDisconnected();
                    Stop();
                }
            }
            catch (Exception ex)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "接收数据头失败", ex);
            }
        }

        private void Send(byte[] data)
        {
            NetworkStream ns = null;
            try
            {
                ns = new NetworkStream(socket);
                if (ns.CanWrite)
                {
                    ns.BeginWrite(
                        data,
                        0,
                        data.Length,
                        new AsyncCallback(Send),
                        ns);
                }
            }
            catch (Exception ex)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "发送数据失败", ex);
            }
        }

        private void Send(IAsyncResult ar)
        {
            NetworkStream ns = (NetworkStream)ar.AsyncState;
            try
            {
                ns.EndWrite(ar);
                ns.Flush();
                ns.Close();
            }
            catch (Exception ex)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "发送数据失败", ex);
            }
        }

        public void Stop()
        {
            Stopping?.Invoke(this, new EventArgs());
            socket.Close();
        }
    }
}
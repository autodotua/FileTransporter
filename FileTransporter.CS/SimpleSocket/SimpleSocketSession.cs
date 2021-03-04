using System;
using System.Net.Sockets;

namespace FileTransporter.SimpleSocket
{
    public class SimpleSocketSession<T> where T : SimpleSocketDataBase
    {
        private Socket socket;

        public event EventHandler Stopping;

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
            catch (Exception e)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "StartRcvData:" + e.Message);
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
                SimpleSocketUtility.Log(LogLevel.Error, "接受数据头失败", ex);
            }
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
                SimpleSocketUtility.Log(LogLevel.Error, "接受数据主体失败", ex);
            }
        }

        public void Send(T msg)
        {
            byte[] data = SimpleSocketUtility.PackLenInfo(SimpleSocketUtility.Serialize<T>(msg));
            Send(data);
        }

        public void Send(byte[] data)
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

        private void Stop()
        {
            Stopping?.Invoke(this, new EventArgs());
            socket.Close();
        }

        protected virtual void OnConnected()
        {
            SimpleSocketUtility.Log(LogLevel.Info, "新的会话已连接");
            Connected?.Invoke(this, new EventArgs());
        }

        protected virtual void OnReciveData(T message)
        {
            SimpleSocketUtility.Log(LogLevel.Info, "接受到新的数据");
            ReceivedData?.Invoke(this, new DataReceivedEventArgs<T>(this, message));
        }

        protected virtual void OnDisconnected()
        {
            SimpleSocketUtility.Log(LogLevel.Info, "会话已断开连接");
            Disconnected?.Invoke(this, new EventArgs());
        }

        public event EventHandler<DataReceivedEventArgs<T>> ReceivedData;

        public event EventHandler Connected;

        public event EventHandler Disconnected;
    }

    public class DataReceivedEventArgs<T> : EventArgs where T : SimpleSocketDataBase
    {
        public DataReceivedEventArgs(SimpleSocketSession<T> session, T data)
        {
            Session = session;
            Data = data;
        }

        public SimpleSocketSession<T> Session { get; }
        public T Data { get; }
    }
}
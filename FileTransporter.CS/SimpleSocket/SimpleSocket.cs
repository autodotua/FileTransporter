using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileTransporter.SimpleSocket
{
    public abstract class SimpleSocketBase<T, K> where T : SimpleSocketSession<K>, new() where K : SimpleSocketDataBase
    {
        protected Socket socket = null;

        public abstract void Close();
    }

    public class SimpleSocketServer<T> : SimpleSocketServer<SimpleSocketSession<T>, T> where T : SimpleSocketDataBase
    {
    }

    public class SimpleSocketServer<T, K> : SimpleSocketBase<T, K> where T : SimpleSocketSession<K>, new() where K : SimpleSocketDataBase
    {
        public int backlog = 10;
        private List<T> sessions = new List<T>();
        public IReadOnlyList<T> Sessions => sessions.AsReadOnly();

        public SimpleSocketServer()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start(string ip, int port)
        {
            try
            {
                socket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
                socket.Listen(backlog);
                socket.BeginAccept(new AsyncCallback(NewClientConnected), socket);
                SimpleSocketUtility.Log(LogLevel.Info, "服务器启动成功");
            }
            catch (Exception ex)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "启动服务器失败", ex);
                throw;
            }
        }

        private void NewClientConnected(IAsyncResult ar)
        {
            try
            {
                Socket clientSkt = socket.EndAccept(ar);
                T session = new T();
                sessions.Add(session);
                session.ReceivedData += Session_ReceivedData;
                session.Stopping += (p1, p2) =>
                  {
                      if (sessions.Contains(session))
                      {
                          sessions.Remove(session);
                          session.ReceivedData -= Session_ReceivedData;
                      }
                  };
                session.Initialize(clientSkt);
            }
            catch (Exception e)
            {
                SimpleSocketUtility.Log(LogLevel.Error, e.Message);
            }
            socket.BeginAccept(new AsyncCallback(NewClientConnected), socket);
        }

        private void Session_ReceivedData(object sender, DataReceivedEventArgs<K> e)
        {
            ReceivedData?.Invoke(this, e);
        }

        public event EventHandler<DataReceivedEventArgs<K>> ReceivedData;

        public override void Close()
        {
            if (socket != null)
            {
                socket.Close();
            }
        }
    }

    public class SimpleSocketClient<T> : SimpleSocketClient<SimpleSocketSession<T>, T> where T : SimpleSocketDataBase
    {
    }

    public class SimpleSocketClient<T, K> : SimpleSocketBase<T, K> where T : SimpleSocketSession<K>, new() where K : SimpleSocketDataBase
    {
        public T Session { get; private set; } = null;
        public int backlog = 10;

        public SimpleSocketClient()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public Task StartAsync(string ip, int port)
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            try
            {
                socket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), port), new AsyncCallback(ar =>
                {
                    try
                    {
                        socket.EndConnect(ar);
                        Session = new T();
                        Session.Initialize(socket);
                        SimpleSocketUtility.Log(LogLevel.Info, "客户端连接服务器成功");
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        SimpleSocketUtility.Log(LogLevel.Error, "客户端连接服务器失败", ex);
                        tcs.SetException(ex);
                    }
                }), socket);
                SimpleSocketUtility.Log(LogLevel.Info, "客户端启动成功，正在连接服务器");
            }
            catch (Exception ex)
            {
                SimpleSocketUtility.Log(LogLevel.Error, "客户端启动失败", ex);
                tcs.SetException(ex);
            }
            return tcs.Task;
        }

        public override void Close()
        {
            if (socket != null)
            {
                socket.Close();
            }
        }
    }
}
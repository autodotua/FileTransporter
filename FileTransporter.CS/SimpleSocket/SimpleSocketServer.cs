using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;

namespace FileTransporter.SimpleSocket
{
    public class SimpleSocketServer<T> : SimpleSocketServer<SimpleSocketSession<T>, T> where T : SimpleSocketDataBase, new()
    {
    }

    public class SimpleSocketServer<T, K> : SimpleSocketBase<T, K> where T : SimpleSocketSession<K>, new() where K : SimpleSocketDataBase, new()
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
                session.Password = password;
                sessions.Add(session);
                SessionsChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, session));

                session.ReceivedData += Session_ReceivedData;
                session.Stopping += (p1, p2) =>
                  {
                      if (sessions.Contains(session))
                      {
                          sessions.Remove(session);
                          session.ReceivedData -= Session_ReceivedData;
                          SessionsChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Remove, session));
                      }
                  };
                session.Initialize(clientSkt);
                socket.BeginAccept(new AsyncCallback(NewClientConnected), socket);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception e)
            {
                SimpleSocketUtility.Log(LogLevel.Error, e.Message);
            }
        }

        private void Session_ReceivedData(object sender, DataReceivedEventArgs<K> e)
        {
            ReceivedData?.Invoke(this, e);
        }

        public event CollectionChangeEventHandler SessionsChanged;

        public event EventHandler<DataReceivedEventArgs<K>> ReceivedData;

        public override void Close()
        {
            if (socket != null)
            {
                foreach (var session in sessions.ToArray())
                {
                    session.Stop();
                }
                socket.Close();
            }
        }
    }
}
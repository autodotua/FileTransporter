using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileTransporter.SimpleSocket
{
    public class SimpleSocketClient<T> : SimpleSocketClient<SimpleSocketSession<T>, T> where T : SimpleSocketDataBase, new()
    {
    }

    public class SimpleSocketClient<T, K> : SimpleSocketBase<T, K>
        where T : SimpleSocketSession<K>, new()
        where K : SimpleSocketDataBase, new()
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
                socket.BeginConnect(GetIPEndPoint(ip, port), new AsyncCallback(ar =>
                 {
                     try
                     {
                         socket.EndConnect(ar);
                         Session = new T();
                         Session.Password = password;
                         Session.Initialize(socket);
                         Session.Disconnected += (p1, p2) =>
                         {
                             Close();
                         };
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

        private static IPEndPoint GetIPEndPoint(string address, int port)
        {
            if (Regex.IsMatch(address, @"((^\s*((([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]))\s*$)|(^\s*((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:)))(%.+)?\s*$))"))
            {
                return new IPEndPoint(IPAddress.Parse(address), port);
            }

            var addresses = System.Net.Dns.GetHostAddresses(address);
            if (addresses.Length == 0)
            {
                throw new ArgumentException(
                    "Unable to retrieve address from specified host name.",
                    "hostName"
                );
            }
            else if (addresses.Length > 1)
            {
                throw new ArgumentException(
                    "There is more that one IP address to the specified host.",
                    "hostName"
                );
            }
            return new IPEndPoint(addresses[0], port);
        }

        public override void Close()
        {
            if (socket != null)
            {
                socket.Close();
                socket = null;
                OnClosed();
            }
        }
    }
}
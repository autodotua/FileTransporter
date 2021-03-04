using Microsoft.Extensions.Hosting;
using PENet;
using SuperSocket;
using SuperSocket.ProtoBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Windows;

namespace FileTransporter.Util
{
    public class SocketHelper
    {
        private System.Threading.Timer timer;

        private PESocket<SocketSession, SocketData> server;
        private PESocket<SocketSession, SocketData> client;

        public void StartServer()
        {
            server = new PESocket<SocketSession, SocketData>();
            server.StartAsServer("0.0.0.0", 12345);
        }

        public void StartClient()
        {
            client = new PESocket<SocketSession, SocketData>();

            client.StartAsClient("127.0.0.1", 12345);
            //client.session = new SocketSession();

            timer = new System.Threading.Timer(p =>
            {
                Debug.WriteLine(DateTime.Now.ToLongTimeString() + ": 发送消息");
                client.session.SendMsg(new SocketData()
                {
                    Type = "hello",

                    Data = new byte[] { 1, 2, 3, 4, 5 }
                });
            }, null, 1000, 10000);
        }
    }

    [Serializable]
    public class SocketData : PEMsg
    {
        public string Type { get; set; }
        public byte[] Data { get; set; }
    }

    public class SocketSession : PESession<SocketData>
    {
        protected override void OnReciveMsg(SocketData msg)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("接收到消息：" + BitConverter.ToString(msg.Data));
            });
            base.OnReciveMsg(msg);
        }
    }
}
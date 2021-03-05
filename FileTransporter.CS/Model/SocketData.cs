using FileTransporter.SimpleSocket;
using System;

namespace FileTransporter.Model
{
    [Serializable]
    public class SocketData : SimpleSocketDataBase
    {
        public SocketDataType Type { get; set; }
        public SocketDataAction Action { get; set; }
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

        public bool Is<T>() where T : class => Data is T;

        public SocketData()
        {
        }

        public SocketData(SocketDataType type, SocketDataAction action, object data = null) : this()
        {
            Type = type;
            Action = action;
            Data = data;
        }
    }
}
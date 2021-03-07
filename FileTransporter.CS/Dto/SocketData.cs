using FileTransporter.SimpleSocket;
using System;

namespace FileTransporter.Dto
{
    public enum SocketDataAction
    {
        CheckResponse,
        CheckRequest,
        FileSendRequest,
        FileBufferResponse,
        FileCanceledResponse,
        FileBufferRequest,
        FileListRequest,
        FileDownloadRequest,
        FileListResponse,
        Error,
    }

    public enum SocketDataType
    {
        General = 0,
        Request = 1,
        Response = 2
    }

    [Serializable]
    public class SocketData : SimpleSocketDataBase
    {
        public SocketData()
        {
        }

        public SocketData(SocketDataType type, SocketDataAction action, object data = null) : this()
        {
            Type = type;
            Action = action;
            Data = data;
        }

        public SocketDataAction Action { get; set; }
        public object Data { get; set; }
        public SocketDataType Type { get; set; }

        public T Get<T>() where T : class => Data is T t ?
            t : throw new Exception($"接收到的数据不符合期望类型。接收到{ (Data == null ? "null" : Data.GetType().Name)}，期望为{typeof(T).Name}");

        public bool GetBool()
        {
            return Convert.ToBoolean(Data);
        }

        public string GetString()
        {
            return Data as string;
        }

        public bool Is<T>() where T : class => Data is T;
    }
}
namespace FileTransporter.SimpleSocket
{
    using System;

    [Serializable]
    public abstract class SimpleSocketDataBase
    {
        //public int seq;
        //public int cmd;
        //public int err;
        public string Password { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; }
    }
}
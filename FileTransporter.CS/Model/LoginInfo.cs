namespace FileTransporter.Panels
{
    public class LoginInfo
    {
        public ushort ServerPort { get; set; } = 8080;
        public ushort ClientPort { get; set; } = 8080;
        public string ClientConnectAddress { get; set; } = "127.0.0.1";
        public string ServerPassword { get; set; }
        public string ClientPassword { get; set; }
        public string ClientName { get; set; } = System.Environment.MachineName;
    }
}
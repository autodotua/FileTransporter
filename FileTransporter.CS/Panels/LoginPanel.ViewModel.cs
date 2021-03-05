namespace FileTransporter.Panels
{
    public class LoginPanelViewModel
    {
        public ushort ServerPort { get; set; } = 8080;
        public ushort ClientPort { get; set; } = 12344;
        public string ClientConnectAddress { get; set; } = "autodotua.top";
        public string ServerPassword { get; set; }
        public string ClientPassword { get; set; }
    }
}
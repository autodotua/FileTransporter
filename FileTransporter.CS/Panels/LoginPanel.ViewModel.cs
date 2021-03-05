namespace FileTransporter.Panels
{
    public class LoginPanelViewModel
    {
        public ushort ServerPort { get; set; } = 8080;
        public ushort ClientPort { get; set; } = 10402;//12344;
        public string ClientConnectAddress { get; set; } = "54.223.221.55";// "autodotua.top";
        public string ServerPassword { get; set; }
        public string ClientPassword { get; set; }
    }
}
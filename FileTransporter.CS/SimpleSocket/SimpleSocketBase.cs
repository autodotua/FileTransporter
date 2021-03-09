using System;
using System.Net.Sockets;
using System.Text;

namespace FileTransporter.SimpleSocket
{
    public abstract class SimpleSocketBase<T, K>
        where T : SimpleSocketSession<K>, new()
        where K : SimpleSocketDataBase, new()
    {
        public string password;
        protected Socket socket = null;

        public event EventHandler Closed;

        public abstract void Close();

        public void SetPassword(string pswd)
        {
            if (string.IsNullOrEmpty(pswd))
            {
                return;
            }
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(pswd);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            password = sb.ToString();
        }

        protected void OnClosed()
        {
            Closed?.Invoke(this, new EventArgs());
        }
    }
}
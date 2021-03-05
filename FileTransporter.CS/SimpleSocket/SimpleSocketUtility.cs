using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace FileTransporter.SimpleSocket
{
    public class SimpleSocketUtility
    {
        private static ILog log = LogManager.GetLogger(typeof(App));

        public static byte[] PackNetMsg<T>(T msg) where T : SimpleSocketDataBase
        {
            return PackLenInfo(Serialize(msg));
        }

        /// <summary>
        /// Add length info to package
        /// </summary>
        public static byte[] PackLenInfo(byte[] data)
        {
            int len = data.Length;
            byte[] pkg = new byte[len + 4];
            byte[] head = BitConverter.GetBytes(len);
            head.CopyTo(pkg, 0);
            data.CopyTo(pkg, 4);
            return pkg;
        }

        public static byte[] Serialize<T>(T msg) where T : SimpleSocketDataBase
        {
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, msg);
                    ms.Seek(0, SeekOrigin.Begin);
                    return Compress(ms.ToArray());
                }
                catch (SerializationException e)
                {
                    Log(LogLevel.Error, "Failed to serialize. Reason: " + e.Message);
                    return null;
                }
            }
        }

        public static T Deserialize<T>(byte[] bytes) where T : SimpleSocketDataBase
        {
            using MemoryStream ms = new MemoryStream(Decompress(bytes));
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                T msg = (T)bf.Deserialize(ms);
                return msg;
            }
            catch (SerializationException e)
            {
                Log(LogLevel.Error, "Failed to deserialize. Reason: " + e.Message + " bytesLen:" + bytes.Length);
                return null;
            }
        }

        public static byte[] Compress(byte[] input)
        {
            using MemoryStream outMS = new MemoryStream();
            using GZipStream gzs = new GZipStream(outMS, CompressionMode.Compress, true);
            gzs.Write(input, 0, input.Length);
            gzs.Close();
            return outMS.ToArray();
        }

        public static byte[] Decompress(byte[] input)
        {
            using MemoryStream inputMS = new MemoryStream(input);
            using MemoryStream outMS = new MemoryStream();
            using GZipStream gzs = new GZipStream(inputMS, CompressionMode.Decompress);
            byte[] bytes = new byte[1024];
            int len = 0;
            while ((len = gzs.Read(bytes, 0, bytes.Length)) > 0)
            {
                outMS.Write(bytes, 0, len);
            }
            //高版本可用
            //gzs.CopyTo(outMS);
            gzs.Close();
            return outMS.ToArray();
        }

        public static void Log(LogLevel level, string msg, Exception ex = null)
        {
            switch (level)
            {
                case LogLevel.Info:
                    log.Info(msg, ex);
                    break;

                case LogLevel.Debug:
                    log.Debug(msg, ex);
                    break;

                case LogLevel.Warn:
                    log.Warn(msg, ex);
                    break;

                case LogLevel.Error:
                    log.Error(msg, ex);
                    break;
            }
            Debug.WriteLine(msg);
            NewLog?.Invoke(null, new SimpleSocketLogEventArgs(level, msg, ex));
        }

        public static event EventHandler<SimpleSocketLogEventArgs> NewLog;
    }

    public class SimpleSocketLogEventArgs : EventArgs
    {
        public SimpleSocketLogEventArgs(LogLevel level, string message, Exception exception)
        {
            Level = level;
            Message = message;
            Exception = exception;
        }

        public LogLevel Level { get; }
        public string Message { get; }
        public Exception Exception { get; }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }
}
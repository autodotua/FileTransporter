using FzLib.DataStorage.Serialization;

namespace FileTransporter
{
    public class Config : JsonSerializationBase
    {
        private static Config instance;

        public static Config Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = OpenOrCreate<Config>();
                }
                return instance;
            }
        }

        /// <summary>
        /// 文件块大小
        /// </summary>
        public int FileBufferLength { get; set; } = 1024 * 1024 * 10;//10M

        /// <summary>
        /// 命令超时时间
        /// </summary>
        public int CommandTimeout { get; set; } = 2000;

        /// <summary>
        /// 文件快超时时间
        /// </summary>
        public int FileTimeout { get; set; } = 1000 * 60;

        public string FileReceiveFolder { get; set; } = "files";
    }
}
using System;

namespace FileTransporter.Model
{
    [Serializable]
    public class FileHead
    {
        public long Length { get; set; }
        public string Name { get; set; }
        public Guid ID { get; set; }
    }
}
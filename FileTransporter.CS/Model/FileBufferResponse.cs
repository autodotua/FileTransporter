using System;

namespace FileTransporter.Model
{
    [Serializable]
    public class FileBufferResponse
    {
        public long Length { get; set; }
        public long Position { get; set; }
        public Guid ID { get; set; }
        public byte[] Content { get; set; }
    }
}
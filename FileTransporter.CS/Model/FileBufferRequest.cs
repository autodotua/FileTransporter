using System;

namespace FileTransporter.Model
{
    [Serializable]
    public class FileBufferRequest
    {
        public Guid ID { get; set; }
        public long Position { get; set; }
        public bool End { get; set; }
    }
}
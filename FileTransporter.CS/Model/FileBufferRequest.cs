using System;

namespace FileTransporter.Model
{
    [Serializable]
    public class FileBufferRequest
    {
        public Guid ID { get; set; }
        public long Position { get; set; }
        public FileRequestType Type { get; set; } = FileRequestType.Next;
    }

    public enum FileRequestType
    {
        Next,
        End,
        Cancel
    }
}
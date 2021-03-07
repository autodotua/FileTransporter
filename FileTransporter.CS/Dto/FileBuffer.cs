using System;

namespace FileTransporter.Dto
{
    public enum FileRequestType
    {
        Next,
        End,
        Cancel
    }

    [Serializable]
    public class FileBufferRequest
    {
        public Guid ID { get; set; }
        public long Position { get; set; }
        public FileRequestType Type { get; set; } = FileRequestType.Next;
    }

    [Serializable]
    public class FileBufferResponse
    {
        public byte[] Content { get; set; }
        public Guid ID { get; set; }
        public long Length { get; set; }
        public long Position { get; set; }
    }
}
namespace FileTransporter.Model
{
    public class TransportProgress
    {
        public TransportProgress(long totalLength, long currentLength)
        {
            TotalLength = totalLength;
            CurrentLength = currentLength;
        }

        public long TotalLength { get; private set; }
        public long CurrentLength { get; private set; }
        public bool Cancel { get; set; }
    }
}
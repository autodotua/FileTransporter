namespace FileTransporter.Model
{
    public enum SocketDataAction
    {
        CheckResponse,
        CheckRequest,
        FileSendRequest,
        FileBufferResponse,
        FileCanceledResponse,
        FileBufferRequest,
        Error,
    }
}
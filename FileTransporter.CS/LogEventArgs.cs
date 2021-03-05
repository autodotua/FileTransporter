using FileTransporter.SimpleSocket;
using System;

namespace FileTransporter
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(LogLevel level, string message, Exception exception)
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
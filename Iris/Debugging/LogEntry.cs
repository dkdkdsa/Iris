using System;

namespace Iris.Debugging
{
    public readonly struct LogEntry
    {
        public LogLevel Level { get; }
        public string Channel { get; }
        public string Message { get; }
        public string StackTrace { get; }
        public object Context { get; }
        public DateTime TimeUtc { get; }

        public LogEntry(LogLevel level, string channel, string message, string stackTrace, object context, DateTime timeUtc)
        {
            Level = level;
            Channel = channel;
            Message = message;
            StackTrace = stackTrace;
            Context = context;
            TimeUtc = timeUtc;
        }
    }
}

namespace Iris.Debugging
{
    public interface ILogSink
    {
        void Write(in LogEntry entry);
    }
}

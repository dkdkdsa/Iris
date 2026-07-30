using System;

namespace Iris.Debugging
{
    public readonly struct DebugChannel
    {
        private readonly string _name;

        public DebugChannel(string name)
        {
            _name = name;
        }

        public string Name => _name;

        public void Trace(object message, object context = null)
        {
            Debug.Write(LogLevel.Trace, _name, message, context);
        }

        public void Log(object message, object context = null)
        {
            Debug.Write(LogLevel.Info, _name, message, context);
        }

        public void LogWarning(object message, object context = null)
        {
            Debug.Write(LogLevel.Warning, _name, message, context);
        }

        public void LogError(object message, object context = null)
        {
            Debug.Write(LogLevel.Error, _name, message, context);
        }

        public void LogException(Exception exception, object context = null)
        {
            Debug.WriteException(_name, null, exception, context, false);
        }

        public void LogException(object message, Exception exception, object context = null)
        {
            Debug.WriteException(_name, message, exception, context, false);
        }

        public bool LogOnce(LogLevel level, object message, object context = null)
        {
            return Debug.WriteOnce(level, _name, message, context);
        }

        public bool LogExceptionOnce(Exception exception, object context = null)
        {
            return Debug.WriteException(_name, null, exception, context, true);
        }

        public bool LogExceptionOnce(object message, Exception exception, object context = null)
        {
            return Debug.WriteException(_name, message, exception, context, true);
        }
    }
}

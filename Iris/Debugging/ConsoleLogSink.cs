using Iris.Core;
using System;
using System.Text;

namespace Iris.Debugging
{
    public sealed class ConsoleLogSink : ILogSink
    {
        private readonly object _gate = new();
        private readonly StringBuilder _builder = new();
        private readonly bool _useColor;

        public bool ShowTimestamp { get; set; } = true;
        public bool ShowStackTrace { get; set; } = true;

        public ConsoleLogSink(bool useColor = true)
        {
            _useColor = useColor && !Console.IsOutputRedirected;
        }

        public void Write(in LogEntry entry)
        {
            lock (_gate)
            {
                _builder.Clear();

                if (ShowTimestamp)
                {
                    _builder.Append(entry.TimeUtc.ToLocalTime().ToString("HH:mm:ss.fff"));
                    _builder.Append("  ");
                }

                _builder.Append(LabelOf(entry.Level));
                _builder.Append("  ");

                if (!string.IsNullOrEmpty(entry.Channel))
                {
                    _builder.Append('[');
                    _builder.Append(entry.Channel);
                    _builder.Append("] ");
                }

                _builder.Append(entry.Message);

                if (entry.Context != null)
                {
                    _builder.Append("  <");
                    _builder.Append(LogContext.Describe(entry.Context));
                    _builder.Append('>');
                }

                if (ShowStackTrace && !string.IsNullOrEmpty(entry.StackTrace))
                {
                    _builder.AppendLine();
                    _builder.Append(entry.StackTrace.TrimEnd());
                }

                Emit(_builder.ToString(), entry.Level);
            }
        }

        private void Emit(string text, LogLevel level)
        {
            if (!_useColor)
            {
                Console.Out.WriteLine(text);
                return;
            }

            var previous = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = ColorOf(level);
                Console.Out.WriteLine(text);
            }
            finally
            {
                Console.ForegroundColor = previous;
            }
        }

        private static string LabelOf(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return "TRACE";
                case LogLevel.Warning: return "WARN ";
                case LogLevel.Error: return "ERROR";
                default: return "INFO ";
            }
        }

        private static ConsoleColor ColorOf(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return ConsoleColor.DarkGray;
                case LogLevel.Warning: return ConsoleColor.Yellow;
                case LogLevel.Error: return ConsoleColor.Red;
                default: return ConsoleColor.Gray;
            }
        }
    }
}

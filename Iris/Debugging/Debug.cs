using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Iris.Debugging
{
    public static class Debug
    {
        private const int MaxInnerExceptionDepth = 8;

        private static readonly object _gate = new();
        private static readonly HashSet<string> _onceKeys = new(StringComparer.Ordinal);

        private static volatile ILogSink[] _sinks = { new ConsoleLogSink() };

        public static LogLevel MinLevel { get; set; } = LogLevel.Trace;

        public static LogLevel StackTraceMinLevel { get; set; } = LogLevel.Warning;

        public static string DefaultChannel { get; set; } = "Iris";

        public static DebugChannel Channel(string name)
        {
            return new DebugChannel(name);
        }

        public static void AddSink(ILogSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            lock (_gate)
            {
                var current = _sinks;

                for (int i = 0; i < current.Length; i++)
                {
                    if (ReferenceEquals(current[i], sink))
                        return;
                }

                var next = new ILogSink[current.Length + 1];
                Array.Copy(current, next, current.Length);
                next[current.Length] = sink;

                _sinks = next;
            }
        }

        public static bool RemoveSink(ILogSink sink)
        {
            if (sink == null)
                return false;

            lock (_gate)
            {
                var current = _sinks;
                int index = -1;

                for (int i = 0; i < current.Length; i++)
                {
                    if (ReferenceEquals(current[i], sink))
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                    return false;

                var next = new ILogSink[current.Length - 1];
                Array.Copy(current, 0, next, 0, index);
                Array.Copy(current, index + 1, next, index, current.Length - index - 1);

                _sinks = next;
                return true;
            }
        }

        public static void ClearSinks()
        {
            lock (_gate)
                _sinks = Array.Empty<ILogSink>();
        }

        public static void Trace(object message, object context = null)
        {
            Write(LogLevel.Trace, null, message, context);
        }

        public static void Log(object message, object context = null)
        {
            Write(LogLevel.Info, null, message, context);
        }

        public static void LogWarning(object message, object context = null)
        {
            Write(LogLevel.Warning, null, message, context);
        }

        public static void LogError(object message, object context = null)
        {
            Write(LogLevel.Error, null, message, context);
        }

        public static void LogException(Exception exception, object context = null)
        {
            WriteException(null, null, exception, context, false);
        }

        public static void LogException(object message, Exception exception, object context = null)
        {
            WriteException(null, message, exception, context, false);
        }

        public static bool LogOnce(LogLevel level, object message, object context = null)
        {
            return WriteOnce(level, null, message, context);
        }

        public static bool LogExceptionOnce(Exception exception, object context = null)
        {
            return WriteException(null, null, exception, context, true);
        }

        public static bool LogExceptionOnce(object message, Exception exception, object context = null)
        {
            return WriteException(null, message, exception, context, true);
        }

        public static bool Assert(bool condition, object message = null, object context = null)
        {
            if (condition)
                return true;

            Write(LogLevel.Error, null, message ?? "Assertion failed", context);
            return false;
        }

        public static void ResetOnce()
        {
            lock (_gate)
                _onceKeys.Clear();
        }

        internal static void Write(LogLevel level, string channel, object message, object context)
        {
            if (level < MinLevel)
                return;

            var sinks = _sinks;

            if (sinks.Length == 0)
                return;

            string text = message?.ToString() ?? string.Empty;
            string stack = level >= StackTraceMinLevel ? CaptureStack() : null;

            Dispatch(sinks, new LogEntry(level, channel ?? DefaultChannel, text, stack, context, DateTime.UtcNow));
        }

        internal static bool WriteOnce(LogLevel level, string channel, object message, object context)
        {
            if (level < MinLevel)
                return false;

            var sinks = _sinks;

            if (sinks.Length == 0)
                return false;

            channel ??= DefaultChannel;

            string text = message?.ToString() ?? string.Empty;

            if (!TryClaimOnce(channel, text))
                return false;

            string stack = level >= StackTraceMinLevel ? CaptureStack() : null;

            Dispatch(sinks, new LogEntry(level, channel, text, stack, context, DateTime.UtcNow));
            return true;
        }

        internal static bool WriteException(string channel, object message, Exception exception, object context, bool once)
        {
            if (LogLevel.Error < MinLevel)
                return false;

            var sinks = _sinks;

            if (sinks.Length == 0)
                return false;

            string detail = exception == null
                ? "null exception"
                : $"{exception.GetType().Name}: {exception.Message}";

            channel ??= DefaultChannel;

            string prefix = message?.ToString();
            string text = string.IsNullOrEmpty(prefix) ? detail : prefix + " — " + detail;

            if (once && !TryClaimOnce(channel, text))
                return false;

            string stack = FormatException(exception) ?? CaptureStack();

            Dispatch(sinks, new LogEntry(LogLevel.Error, channel, text, stack, context, DateTime.UtcNow));
            return true;
        }

        private static bool TryClaimOnce(string channel, string text)
        {
            lock (_gate)
                return _onceKeys.Add(channel == null ? text : channel + ' ' + text);
        }

        private static void Dispatch(ILogSink[] sinks, in LogEntry entry)
        {
            for (int i = 0; i < sinks.Length; i++)
            {
                try
                {
                    sinks[i].Write(entry);
                }
                catch
                {
                }
            }
        }

        private static string CaptureStack()
        {
            var builder = new StringBuilder();
            AppendFrames(builder, new StackTrace(1, true).GetFrames(), skipDebugFrames: true);

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static string FormatException(Exception exception)
        {
            if (exception == null)
                return null;

            var builder = new StringBuilder();
            var current = exception;

            for (int depth = 0; current != null && depth < MaxInnerExceptionDepth; depth++)
            {
                if (depth > 0)
                {
                    builder.Append("   --- Inner exception: ");
                    builder.Append(current.GetType().Name);
                    builder.Append(": ");
                    builder.Append(current.Message);
                    builder.AppendLine();
                }

                AppendFrames(builder, new StackTrace(current, true).GetFrames(), skipDebugFrames: false);

                if (ReferenceEquals(current.InnerException, current))
                    break;

                current = current.InnerException;
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static void AppendFrames(StringBuilder builder, StackFrame[] frames, bool skipDebugFrames)
        {
            if (frames == null)
                return;

            bool skipping = skipDebugFrames;

            for (int i = 0; i < frames.Length; i++)
            {
                var method = frames[i].GetMethod();

                if (method == null)
                    continue;

                var declaring = method.DeclaringType;

                if (skipping)
                {
                    if (declaring == typeof(Debug) || declaring == typeof(DebugChannel))
                        continue;

                    skipping = false;
                }

                builder.Append("   at ");
                builder.Append(declaring?.FullName ?? "?");
                builder.Append('.');
                builder.Append(method.Name);
                builder.Append("()");

                string file = frames[i].GetFileName();

                if (!string.IsNullOrEmpty(file))
                {
                    builder.Append(" in ");
                    builder.Append(file);
                    builder.Append(":line ");
                    builder.Append(frames[i].GetFileLineNumber());
                }

                builder.AppendLine();
            }
        }
    }
}

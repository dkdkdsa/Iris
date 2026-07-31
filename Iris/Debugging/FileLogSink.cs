using System;
using System.IO;
using System.Text;

namespace Iris.Debugging
{
    public sealed class FileLogSink : ILogSink, IDisposable
    {
        public const string DefaultFileName = "game.log";

        private const long DefaultMaxBytes = 8L * 1024 * 1024;

        private readonly object _gate = new();
        private readonly StringBuilder _builder = new();

        private StreamWriter _writer;
        private long _written;
        private bool _capped;

        public string FilePath { get; }

        public long MaxBytes { get; set; } = DefaultMaxBytes;

        public LogLevel FlushLevel { get; set; } = LogLevel.Trace;

        public bool ShowStackTrace { get; set; } = true;

        public FileLogSink(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("The log file path is empty.", nameof(filePath));

            FilePath = Path.GetFullPath(filePath);

            string directory = Path.GetDirectoryName(FilePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            RotatePrevious(FilePath);

            var stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(stream, new UTF8Encoding(false));
        }

        public void Write(in LogEntry entry)
        {
            lock (_gate)
            {
                if (_writer == null || _capped)
                    return;

                _builder.Clear();
                _builder.Append(entry.TimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));
                _builder.Append("  ");
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

                string text = _builder.ToString();

                _writer.WriteLine(text);
                _written += text.Length + Environment.NewLine.Length;

                if (entry.Level >= FlushLevel)
                    _writer.Flush();

                if (MaxBytes > 0 && _written >= MaxBytes)
                {
                    _capped = true;
                    _writer.WriteLine($"--- log limit reached ({MaxBytes} bytes); further entries are dropped ---");
                    _writer.Flush();
                }
            }
        }

        public void Flush()
        {
            lock (_gate)
                _writer?.Flush();
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_writer == null)
                    return;

                try
                {
                    _writer.Flush();
                    _writer.Dispose();
                }
                catch
                {
                }

                _writer = null;
            }
        }

        private static void RotatePrevious(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                string previous = Path.ChangeExtension(filePath, null) + ".prev" + Path.GetExtension(filePath);
                File.Move(filePath, previous, true);
            }
            catch
            {
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
    }
}

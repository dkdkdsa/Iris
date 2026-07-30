using System;

namespace Iris.Debugging
{
    public sealed class BufferLogSink : ILogSink
    {
        private readonly object _gate = new();
        private readonly LogEntry[] _entries;

        private int _start;
        private int _count;
        private int _version;

        public BufferLogSink(int capacity = 1024)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _entries = new LogEntry[capacity];
        }

        public int Capacity => _entries.Length;

        public int Count
        {
            get
            {
                lock (_gate)
                    return _count;
            }
        }

        public int Version
        {
            get
            {
                lock (_gate)
                    return _version;
            }
        }

        public void Write(in LogEntry entry)
        {
            lock (_gate)
            {
                _entries[(_start + _count) % _entries.Length] = entry;

                if (_count == _entries.Length)
                    _start = (_start + 1) % _entries.Length;
                else
                    _count++;

                _version++;
            }
        }

        public LogEntry[] Snapshot()
        {
            lock (_gate)
            {
                var result = new LogEntry[_count];
                CopyTo(result);

                return result;
            }
        }

        public int Snapshot(LogEntry[] destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            lock (_gate)
            {
                int length = Math.Min(_count, destination.Length);
                int offset = _count - length;

                for (int i = 0; i < length; i++)
                    destination[i] = _entries[(_start + offset + i) % _entries.Length];

                return length;
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                Array.Clear(_entries, 0, _entries.Length);
                _start = 0;
                _count = 0;
                _version++;
            }
        }

        private void CopyTo(LogEntry[] destination)
        {
            for (int i = 0; i < _count; i++)
                destination[i] = _entries[(_start + i) % _entries.Length];
        }
    }
}

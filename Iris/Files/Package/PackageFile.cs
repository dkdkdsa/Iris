using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Iris.Files.Package
{
    public sealed class PackageFile : IDisposable
    {
        private readonly SafeFileHandle _handle;
        private readonly Dictionary<ulong, PackageEntry> _entries = new();
        private readonly PackageHeader _header;

        public int EntryCount => _entries.Count;

        internal PackageFile(string path)
        {
            _handle = File.OpenHandle(path);

            try
            {
                long fileLength = RandomAccess.GetLength(_handle);

                if (fileLength < PackageFormat.HeaderSize)
                    throw new InvalidDataException($"Package file is too small: {path}");

                Span<byte> headerBytes = stackalloc byte[PackageFormat.HeaderSize];
                ReadExactly(0, headerBytes);

                _header = MemoryMarshal.Read<PackageHeader>(headerBytes);

                if (_header.magic != PackageFormat.MagicValue)
                    throw new InvalidDataException($"Not a package file: {path}");

                if (_header.version != PackageFormat.CurrentVersion)
                    throw new InvalidDataException($"Unsupported package version: {_header.version} (supported: {PackageFormat.CurrentVersion})");

                long indexSize = (long)_header.entryCount * PackageFormat.EntrySize;

                if ((long)_header.indexOffset < PackageFormat.HeaderSize || (long)_header.indexOffset + indexSize > fileLength)
                    throw new InvalidDataException($"Package index is corrupted: {path}");

                byte[] indexBytes = new byte[indexSize];
                ReadExactly((long)_header.indexOffset, indexBytes);

                ReadOnlySpan<PackageEntry> entries = MemoryMarshal.Cast<byte, PackageEntry>(indexBytes);

                foreach (var entry in entries)
                {
                    if ((long)entry.offset < PackageFormat.HeaderSize || (long)entry.offset + entry.storedSize > fileLength)
                        throw new InvalidDataException($"Package entry is corrupted: {path}");

                    _entries[entry.hash] = entry;
                }
            }
            catch
            {
                _handle.Dispose();
                throw;
            }
        }

        public bool Contains(ulong hash)
        {
            return _entries.ContainsKey(hash);
        }

        public bool TryRead(ulong hash, out byte[] data)
        {
            data = null;

            if (!_entries.TryGetValue(hash, out var entry))
                return false;

            byte[] stored = new byte[entry.storedSize];
            ReadExactly((long)entry.offset, stored);

            if ((entry.flags & PackageEntryFlag.Brotli) != 0)
            {
                byte[] raw = new byte[entry.rawSize];

                if (!BrotliDecoder.TryDecompress(stored, raw, out int written) || written != raw.Length)
                    return false;

                data = raw;
                return true;
            }

            data = stored;
            return true;
        }

        private void ReadExactly(long offset, Span<byte> buffer)
        {
            int total = 0;

            while (total < buffer.Length)
            {
                int read = RandomAccess.Read(_handle, buffer.Slice(total), offset + total);

                if (read == 0)
                    throw new EndOfStreamException("Package file is truncated.");

                total += read;
            }
        }

        public void Dispose()
        {
            _handle.Close();
            _entries.Clear();
        }
    }
}

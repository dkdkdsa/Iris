using Iris.Files.Hash;
using Iris.Files.Package;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Iris.Build.Package
{
    public sealed class PackageFileWriter : IDisposable
    {
        private FileStream _stream;
        private readonly List<PackageEntry> _entries = new();
        private readonly Dictionary<ulong, string> _keysByHash = new();

        private FileStream Stream => _stream ?? throw new InvalidOperationException("CreateFile을 먼저 호출해야 합니다.");

        public int EntryCount => _entries.Count;

        public void CreateFile(string packageFilePath)
        {
            if (_stream != null)
                throw new InvalidOperationException("이미 파일이 열려 있습니다.");

            _stream = File.Create(packageFilePath);
            _stream.Position = PackageFormat.HeaderSize;
        }

        public void Add(string key, string originFilePath)
        {
            AddBytes(key, File.ReadAllBytes(originFilePath));
        }

        public void AddBytes(string key, byte[] rawData)
        {
            var stream = Stream;

            string normalized = PackageFormat.NormalizeKey(key);
            ulong hash = Hashing.Compute(normalized);

            if (_keysByHash.TryGetValue(hash, out var existing))
                throw new InvalidDataException($"키 중복 또는 해시 충돌: '{existing}' / '{normalized}'");

            if (rawData.LongLength > uint.MaxValue)
                throw new InvalidDataException($"4GB를 넘는 파일은 담을 수 없습니다: {normalized}");

            _keysByHash[hash] = normalized;

            byte[] stored = rawData;
            var flags = PackageEntryFlag.None;

            if (ShouldTryCompress(normalized))
            {
                var compressed = TryCompress(rawData);

                if (compressed != null)
                {
                    stored = compressed;
                    flags = PackageEntryFlag.Brotli;
                }
            }

            _entries.Add(new PackageEntry
            {
                hash = hash,
                offset = (ulong)stream.Position,
                rawSize = (uint)rawData.Length,
                storedSize = (uint)stored.Length,
                typeId = 0,
                flags = flags,
            });

            stream.Write(stored, 0, stored.Length);
        }

        public void Save()
        {
            var stream = Stream;

            ulong indexOffset = (ulong)stream.Position;

            if (_entries.Count > 0)
            {
                var index = _entries.ToArray();
                stream.Write(MemoryMarshal.AsBytes(index.AsSpan()));
            }

            var header = new PackageHeader
            {
                magic = PackageFormat.MagicValue,
                version = PackageFormat.CurrentVersion,
                flags = PackageHeaderFlags.None,
                entryCount = (uint)_entries.Count,
                indexOffset = indexOffset,
            };

            stream.Position = 0;

            Span<byte> headerBytes = stackalloc byte[PackageFormat.HeaderSize];
            MemoryMarshal.Write(headerBytes, in header);
            stream.Write(headerBytes);

            stream.Flush();
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }

        private static byte[] TryCompress(byte[] rawData)
        {
            int maxSize = BrotliEncoder.GetMaxCompressedLength(rawData.Length);
            byte[] buffer = new byte[maxSize];

            if (!BrotliEncoder.TryCompress(rawData, buffer, out int written))
                return null;

            if (written >= rawData.Length * 0.95)
                return null;

            return buffer.AsSpan(0, written).ToArray();
        }

        private static bool ShouldTryCompress(string key)
        {
            string ext = Path.GetExtension(key);

            return !ext.Equals(".png", StringComparison.OrdinalIgnoreCase) &&
                   !ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
                   !ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
                   !ext.Equals(".gif", StringComparison.OrdinalIgnoreCase) &&
                   !ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase) &&
                   !ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
        }
    }
}

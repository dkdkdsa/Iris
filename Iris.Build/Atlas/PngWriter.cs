using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.IO.Hashing;

namespace Iris.Build.Atlas
{
    public static class PngWriter
    {
        private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static void Write(string path, int width, int height, ReadOnlySpan<byte> rgba)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Atlas page must have a positive size.");

            if (rgba.Length < width * height * 4)
                throw new ArgumentException("Pixel buffer is smaller than the declared size.", nameof(rgba));

            using var file = File.Create(path);

            file.Write(Signature);

            Span<byte> header = stackalloc byte[13];
            BinaryPrimitives.WriteInt32BigEndian(header[..4], width);
            BinaryPrimitives.WriteInt32BigEndian(header.Slice(4, 4), height);
            header[8] = 8;
            header[9] = 6;
            header[10] = 0;
            header[11] = 0;
            header[12] = 0;

            WriteChunk(file, "IHDR", header);
            WriteChunk(file, "IDAT", Deflate(width, height, rgba));
            WriteChunk(file, "IEND", ReadOnlySpan<byte>.Empty);
        }

        private static byte[] Deflate(int width, int height, ReadOnlySpan<byte> rgba)
        {
            int stride = width * 4;
            var raw = new byte[(stride + 1) * height];

            for (int y = 0; y < height; y++)
            {
                int destination = y * (stride + 1);
                raw[destination] = 0;

                rgba.Slice(y * stride, stride).CopyTo(raw.AsSpan(destination + 1, stride));
            }

            using var buffer = new MemoryStream();

            using (var deflate = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
                deflate.Write(raw, 0, raw.Length);

            return buffer.ToArray();
        }

        private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
        {
            Span<byte> length = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
            stream.Write(length);

            Span<byte> name = stackalloc byte[4];

            for (int i = 0; i < 4; i++)
                name[i] = (byte)type[i];

            stream.Write(name);
            stream.Write(data);

            var crc = new Crc32();
            crc.Append(name);
            crc.Append(data);

            Span<byte> checksum = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(checksum, crc.GetCurrentHashAsUInt32());
            stream.Write(checksum);
        }
    }
}

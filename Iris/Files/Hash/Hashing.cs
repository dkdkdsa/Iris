using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Iris.Files.Hash
{
    public static class Hashing
    {
        private const int StackThreshold = 256;

        private static readonly Dictionary<HashAlgorithmId, IHashAlgorithm> _algorithms = new();
        private static IHashAlgorithm _current;

        static Hashing()
        {
            Register(new XxHash3Algorithm());
            _current = Resolve(HashAlgorithmId.XxHash3);
        }

        public static IHashAlgorithm Current => _current;

        public static HashAlgorithmId CurrentId => _current.Id;

        public static void Register(IHashAlgorithm algorithm)
        {
            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm));

            if (algorithm.Id == HashAlgorithmId.None)
                throw new ArgumentException("해시 알고리즘 식별자가 None이면 패키지에 기록할 수 없습니다.", nameof(algorithm));

            _algorithms[algorithm.Id] = algorithm;
        }

        public static void SetCurrent(HashAlgorithmId id)
        {
            _current = Resolve(id);
        }

        public static IHashAlgorithm Resolve(HashAlgorithmId id)
        {
            if (_algorithms.TryGetValue(id, out var algorithm))
                return algorithm;

            throw new NotSupportedException($"등록되지 않은 해시 알고리즘입니다: {id}");
        }

        public static bool TryResolve(HashAlgorithmId id, out IHashAlgorithm algorithm)
        {
            return _algorithms.TryGetValue(id, out algorithm);
        }

        public static ulong Compute(ReadOnlySpan<byte> data)
        {
            return _current.Compute(data);
        }

        public static ulong Compute(ReadOnlySpan<byte> data, HashAlgorithmId id)
        {
            return Resolve(id).Compute(data);
        }

        public static ulong Compute(string text)
        {
            return Compute(text, _current);
        }

        public static ulong Compute(string text, HashAlgorithmId id)
        {
            return Compute(text, Resolve(id));
        }

        private static ulong Compute(string text, IHashAlgorithm algorithm)
        {
            if (string.IsNullOrEmpty(text))
                return algorithm.Compute(ReadOnlySpan<byte>.Empty);

            int maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);

            if (maxBytes <= StackThreshold)
            {
                Span<byte> buffer = stackalloc byte[StackThreshold];
                int written = Encoding.UTF8.GetBytes(text, buffer);

                return algorithm.Compute(buffer.Slice(0, written));
            }

            byte[] rented = ArrayPool<byte>.Shared.Rent(maxBytes);

            try
            {
                int written = Encoding.UTF8.GetBytes(text, rented);

                return algorithm.Compute(rented.AsSpan(0, written));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}

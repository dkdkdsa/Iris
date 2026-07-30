using System;
using System.IO.Hashing;

namespace Iris.Files.Hash
{
    public sealed class XxHash3Algorithm : IHashAlgorithm
    {
        public HashAlgorithmId Id => HashAlgorithmId.XxHash3;

        public ulong Compute(ReadOnlySpan<byte> data)
        {
            return XxHash3.HashToUInt64(data);
        }
    }
}

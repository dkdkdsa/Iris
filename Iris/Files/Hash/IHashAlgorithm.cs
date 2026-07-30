using System;

namespace Iris.Files.Hash
{
    public interface IHashAlgorithm
    {
        HashAlgorithmId Id { get; }

        ulong Compute(ReadOnlySpan<byte> data);
    }
}

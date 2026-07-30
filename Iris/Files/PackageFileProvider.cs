using Iris.Files.Hash;
using Iris.Files.Package;
using System.IO;

namespace Iris.Files
{
    internal class PackageFileProvider : IVirtualFileProvider
    {
        private readonly PackageFile _file;

        internal PackageFileProvider(string packagePath)
        {
            _file = new PackageFile(packagePath);
        }

        public int EntryCount => _file.EntryCount;

        public bool Exists(string key)
        {
            return _file.Contains(Hashing.Compute(key));
        }

        public byte[] ReadAllBytes(string key)
        {
            if (_file.TryRead(Hashing.Compute(key), out var data))
                return data;

            throw new FileNotFoundException($"패키지에 없는 파일입니다: {key}");
        }
    }
}

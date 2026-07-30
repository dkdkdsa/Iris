using System.IO;

namespace Iris.Files
{
    internal class LooseFileProvider : IVirtualFileProvider
    {
        private readonly string _root;

        public LooseFileProvider(string root)
        {
            _root = Path.GetFullPath(root);
        }

        public bool Exists(string key)
        {
            return File.Exists(Resolve(key));
        }

        public byte[] ReadAllBytes(string key)
        {
            return File.ReadAllBytes(Resolve(key));
        }

        private string Resolve(string key)
        {
            return Path.IsPathRooted(key) ? key : Path.Combine(_root, key);
        }
    }
}

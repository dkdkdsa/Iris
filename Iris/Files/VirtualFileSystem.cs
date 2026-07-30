using Iris.Core;
using System.IO;
using System.Text;

namespace Iris.Files
{
    internal class VirtualFileSystem
    {
        private static IVirtualFileProvider _provider;

        public static void InjectProvider(IVirtualFileProvider provider)
        {
            _provider = provider;
        }

        public static string Canonicalize(string path)
        {
            if (Path.IsPathRooted(path))
                path = Path.GetRelativePath(SceneLoader.ContentRoot, path);

            return Package.PackageFormat.NormalizeKey(path);
        }

        public static byte[] ReadAllBytes(string path)
        {
            if (TryGetKey(path, out var key))
                return _provider.ReadAllBytes(key);

            return File.ReadAllBytes(path);
        }

        public static string ReadAllText(string path)
        {
            if (!TryGetKey(path, out var key))
                return File.ReadAllText(path);

            var bytes = _provider.ReadAllBytes(key);

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

            return Encoding.UTF8.GetString(bytes);
        }

        public static bool Exists(string path)
        {
            if (TryGetKey(path, out var key))
                return _provider.Exists(key);

            return File.Exists(path);
        }

        private static bool TryGetKey(string path, out string key)
        {
            key = null;

            if (_provider == null)
                return false;

            string canonical = Canonicalize(path);

            if (canonical.StartsWith("..") || Path.IsPathRooted(canonical))
                return false;

            key = canonical;
            return true;
        }
    }
}

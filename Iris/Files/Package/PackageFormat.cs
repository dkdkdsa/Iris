using System.Runtime.CompilerServices;

namespace Iris.Files.Package
{
    public static class PackageFormat
    {
        public const uint MagicValue = 0x4B505249;
        public const uint CurrentVersion = 1;
        public const string DefaultFileName = "content.pak";

        public static int HeaderSize => Unsafe.SizeOf<PackageHeader>();

        public static int EntrySize => Unsafe.SizeOf<PackageEntry>();

        public static string NormalizeKey(string key)
        {
            return key.Replace('\\', '/').ToLowerInvariant();
        }
    }
}

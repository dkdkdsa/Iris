using System.Runtime.InteropServices;

namespace Iris.Files.Package
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PackageEntry
    {
        public ulong hash;
        public ulong offset;
        public uint rawSize;
        public uint storedSize;
        public uint typeId;             //나중에 경로기반이 아니라 메타 기반등으로 바꿀때에 대한 대안
        public PackageEntryFlag flags;
    }
}

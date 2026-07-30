using System.Runtime.InteropServices;

namespace Iris.Files.Package
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PackageHeader             //48
    {
        public uint magic;                  //4
        public uint version;                //4
        public PackageHeaderFlags flags;    //4
        public uint entryCount;             //4
        public ulong indexOffset;           //8
        public ulong reserved1;             //8
        public ulong reserved2;             //8
        public ulong reserved3;             //8
        //미래의 나여 감사하거라 내가 24바이트의 자비로운 예약영역을 두었으니
    }
}

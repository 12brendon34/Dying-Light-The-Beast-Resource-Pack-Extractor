using System.Runtime.InteropServices;

namespace ChromED_RP6;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RDPMainHeader
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Magic;

    public uint Version;
    public uint Flags;
    public uint PhysResCount;
    public uint PhysResTypeCount;
    public uint ResourceNamesCount;
    public uint ResourceNamesBlockSize;
    public uint LogResCount;
    public uint SectorAlignment;
}
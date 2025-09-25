using System.Runtime.InteropServices;

namespace ChromED_RP6;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RDPResourceTypeHeader
{
    public uint Bitfields;
    public uint DataFileOffset;
    public uint DataByteSize;
    public uint CompressedByteSize;
    public uint ResourceCount;
}
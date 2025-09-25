using System.Runtime.InteropServices;

namespace ChromED_RP6;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RDPResourceEntryHeader
{
    public uint Bitfields;
    public uint DataOffset;
    public uint DataByteSize;
    public short CompressedByteSize;
    public short ReferencedResource;
}
using System.Runtime.InteropServices;

namespace ChromED_RP6;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RDPLogicalResourceEntryHeader
{
    public uint Bitfields;
    public uint FirstNameIndex;
    public uint FirstResource;
}
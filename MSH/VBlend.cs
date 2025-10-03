using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VBlend
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Bone;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public ushort[] Weight;

    public VBlend()
    {
        Bone = new byte[4];
        Weight = new ushort[4];
    }
}
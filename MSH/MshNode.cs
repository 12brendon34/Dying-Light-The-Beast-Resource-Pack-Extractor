using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential)]
public struct MshNode
{
    public MshType Type;

    // Fixed-length name stored in the file.
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string Name;

    public short Parent;
    public short Children;
    public uint NumLods;
    public Matrix3X4 Local;
    public Matrix3X4 BoneTransform;
    public Aabb Bounds;
    public uint Flags;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] Unused;
}
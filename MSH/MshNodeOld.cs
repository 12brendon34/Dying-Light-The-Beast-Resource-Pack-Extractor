using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential)]
public struct MshNodeOld
{
    public MshType Type;

    // Fixed-length name stored in the file.
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string Name;

    public short Parent;
    public short Children;
    public uint NumLods;
    
    //only thing changed in the "old" node I think.
    public Matrix4X4 Local;
    public Matrix4X4 BoneTransform;
    public Aabb Bounds;
    public uint Flags;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] Unused;
    
    public static MshNode Convert(MshNodeOld oldNode)
    {
        return new MshNode
        {
            Type = oldNode.Type,
            Name = oldNode.Name,
            Parent = oldNode.Parent,
            Children = oldNode.Children,
            NumLods = oldNode.NumLods,
            Local = Matrix4X4.ToMatrix3X4(oldNode.Local),
            BoneTransform = Matrix4X4.ToMatrix3X4(oldNode.BoneTransform),
            Bounds = oldNode.Bounds,
            Flags = oldNode.Flags,
            Unused = oldNode.Unused
        };
    }
}
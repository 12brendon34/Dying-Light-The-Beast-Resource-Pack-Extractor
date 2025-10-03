using System;
using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SfDesc
{
    public ushort MatId;
        
    public uint Offset;
    public uint Count;

    public IntPtr Bones;

    public ushort NumBones;
}
using System.Runtime.InteropServices;
using Utils.Resources;

namespace ChromED_RP6;
//changed in DL2's CE 6.5
/*
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RTextureInfo
{
    public ushort Width;
    public ushort Height;
    public ushort Depth;
    public ushort ArraySize;
    public ushort MipLevels;
    public ushort Flags;
    public Utils.EFormat Format;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public uint[] MipLevelOffsets;
}
*/

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RTextureInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Magic; // "IMGC"

    public uint ID;
    public uint Header_Size;
    public uint Unk;

    public Color Col_Min;
    public Color Col_Max;
    public Color Col_Average;

    public ushort Width;
    public ushort Height;
    public ushort Depth;
    public ResourceTypeInfo.EFormat Format;

    private byte Tex_Mip; // lower 2 bits = tex_type, upper 6 bits = mip_count
    public byte TexType
    {
        get => (byte)(Tex_Mip & 0x03);
        set => Tex_Mip = (byte)((Tex_Mip & 0xFC) | (value & 0x03));
    }

    public byte MipLevels
    {
        get => (byte)((Tex_Mip >> 2) & 0x3F);
        set => Tex_Mip = (byte)((Tex_Mip & 0x03) | ((value & 0x3F) << 2));
    }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public ushort[] Reserved;
}

[StructLayout(LayoutKind.Sequential)]
public struct Color
{
    public float R;
    public float G;
    public float B;
    public float A;

    public override string ToString()
    {
        return $"{R}, {G}, {B}, {A}";
    }
}
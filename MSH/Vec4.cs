using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential)]
public struct Vec4
{
    public float X;
    public float Y;
    public float Z;
    public float W;
}
using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential)]
public struct Matrix4X4
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public float[] M;

    public float this[int row, int col]
    {
        get => M[row * 4 + col];
        set => M[row * 4 + col] = value;
    }
    
    public static Matrix3X4 ToMatrix3X4(Matrix4X4 m)
    {
        return new Matrix3X4
        {
            M =
            [
                m[0,0], m[0,1], m[0,2], m[0,3],
                m[1,0], m[1,1], m[1,2], m[1,3],
                m[2,0], m[2,1], m[2,2], m[2,3]
            ]
        };
    }
}
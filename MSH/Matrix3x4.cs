using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential)]
public struct Matrix3X4
{
    // Flattened 3x4 matrix (3 rows, 4 columns) stored row-major: row*4 + col
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public float[] M;

    // Convenient indexer: row in [0..2], col in [0..3]
    public float this[int row, int col]
    {
        get => M[row * 4 + col];
        set => M[row * 4 + col] = value;
    }
}
using System.Runtime.InteropServices;

namespace MSH
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Mpack4
    {
        public MvFmt Fmt;
        public uint Stride;
        public Vec4 BiasScale;

        public IntPtr Data;
    }
}
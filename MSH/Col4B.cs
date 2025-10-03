using System.Runtime.InteropServices;

namespace MSH
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Col4B
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;
    }
}
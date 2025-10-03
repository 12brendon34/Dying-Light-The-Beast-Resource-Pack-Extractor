using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential)]
public struct Aabb
{
    public Vector3 Origin;
    public Vector3 Span;
}
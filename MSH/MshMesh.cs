using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential)]
public struct MshMesh
{
    public uint NumVertices;
    public uint NumIndices;
    public uint NumSurfaces;
    public uint NumTargets;
}
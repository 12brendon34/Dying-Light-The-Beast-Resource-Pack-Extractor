using System.Runtime.InteropServices;

namespace MSH;

[StructLayout(LayoutKind.Sequential)]
public struct MshRoot
{
    public uint NumNodes;
    public uint NumMaterials;
    public uint NumSurfaceTypes;
}
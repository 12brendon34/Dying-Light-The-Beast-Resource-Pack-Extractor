using System.Runtime.InteropServices;

namespace MSH;

public enum ChunkTypes : uint
{
    Header = 4739917,           // magic id
    Material = 0x500,
    NodeV1 = 1,                 // actually marked as old in editor
    NodeV2 = 2,
    NodeV3 = 3,
    CCollTreePacked = 0x601,
    CCollTreePackedAlso = 0x602,
    Surface = 0x700,

    // sub-chunk stuff
    MTOOl_FMT = 0x100,
    VertexFormat = 0x160,
    VertexBuffer = 0x101,

    VertexNormal0 = 0x102,
    VertexNormal1 = 0x181,
    VertexTangent0 = 0x103,
    VertexTangent1 = 0x191,
    VertexBitangent0 = 0x195,
    VertexBitangent1 = 0x196,
    VertexPosition1 = 0x171,

    VertexUV0 = 0x120,
    VertexUV1 = 0x121,

    IndexBuffer = 0x140,

    SurfaceDesc = 0x150,
    SurfaceDescAlt = 0x151
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Chunk
{
    public ChunkTypes Id;
    public uint Version;
    public uint ChunkSize;
    public uint DataSize;
}
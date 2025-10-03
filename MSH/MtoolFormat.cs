using System;

namespace MSH;

public class SurfaceDescManaged
{
    public ushort MatId;
    public uint Offset;
    public uint Count;
    public ushort NumBones;
    public ushort[] Bones;
}

public class MtoolFormat
{
    public Mpack4[] Vxyz { get; } = new Mpack4[2];

    // Managed raw buffers (these replace the raw pointer usage in C++ for convenience)
    public byte[] VxyzData0 { get; set; }    // position buffer (12 * numVertices)
    public byte[] VxyzData1 { get; set; }

    public byte[] VNormalData0 { get; set; } // normals (12 * numVertices)
    public byte[] VNormalData1 { get; set; }

    public byte[] VTangentData0 { get; set; }
    public byte[] VBitangentData0 { get; set; }

    public byte[] VUvData0 { get; set; }     // uv buffers (8 * numVertices)
    public byte[] VUvData1 { get; set; }
    public byte[] VUvData2 { get; set; }
    public byte[] VUvData3 { get; set; }

    public ushort[] Indices { get; set; }    // index buffer

    // Surface descriptions read from file as managed objects
    public SurfaceDescManaged[] Surfaces { get; set; }

    public MvFmt VNormalFmt { get; set; }
    public float VNormalScale { get; set; }
    public uint VNormalStride { get; set; }

    public MvFmt VTangentFmt { get; set; }
    public float VTangentScale { get; set; }
    public uint VTangentStride { get; set; }

    public MvFmt VBitangentFmt { get; set; }
    public float VBitangentScale { get; set; }
    public uint VBitangentStride { get; set; }

    public MvFmt VUvFmt { get; set; }
    public float VUvScale { get; set; }
    public uint VUvStride { get; set; }

    public uint NumSurfaces { get; set; }
    public uint NumVertices { get; set; }
    public uint NumIndices { get; set; }
    public byte NumBpv { get; set; }

    public ushort[] FaceMatId { get; set; }
    public uint[] FaceAttr { get; set; }

    public MtoolFormat()
    {
        VNormalFmt = MvFmt.Unknown;
        VTangentFmt = MvFmt.Unknown;
        VBitangentFmt = MvFmt.Unknown;
        VUvFmt = MvFmt.Unknown;
        Vxyz[0] = default;
        Vxyz[1] = default;
    }
}
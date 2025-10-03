using System.Runtime.InteropServices;
using System.Text;
using Utils.IO;

namespace MSH;

static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.WriteLine("Usage: {0} input_file.MSH [output_dir]", AppDomain.CurrentDomain.FriendlyName);
            return 1;
        }

        string inputFile = args[0];
        string outputDir = args.Length >= 2
            ? args[1]
            : Path.Combine(Path.GetDirectoryName(inputFile) ?? ".", Path.GetFileNameWithoutExtension(inputFile));

        try
        {
            var data = new MshData();
            uint nodeCount = 0;
            
            using var input = File.OpenRead(inputFile);
            while (input.Position < input.Length)
            {
                var currentChunk = StreamHelpers.ReadStruct<Chunk>(input);

                Console.WriteLine($"Chunk ID: {currentChunk.Id}, size: {currentChunk.ChunkSize}");

                switch (currentChunk.Id)
                {
                    case ChunkTypes.Header:
                        data.Root = StreamHelpers.ReadStruct<MshRoot>(input);
                        Console.WriteLine("Number of Nodes in msh header {0}", data.Root.NumNodes);
                        break;

                    case ChunkTypes.Material:
                    {
                        string materialString = StreamHelpers.ReadString(input, Encoding.ASCII, (int)currentChunk.DataSize);
                        data.Mats ??= [];

                        data.Mats.Add(materialString);
                    }
                        break;

                    case ChunkTypes.Surface:
                    {
                        string surfaceString = StreamHelpers.ReadString(input, Encoding.ASCII, (int)currentChunk.DataSize);
                        data.SurfaceTypes ??= [];

                        data.SurfaceTypes.Add(surfaceString);
                    }
                        break;

                    case ChunkTypes.NodeV3:
                    {
                        if (data.Tree == null)
                        {
                            data.Tree = new MshTree[data.Root.NumNodes];

                            for (int i = 0; i < data.Tree.Length; i++)
                            {
                                data.Tree[i].Mesh = new MtoolFormat[4];
                            }
                        }

                        if (nodeCount >= data.Tree.Length)
                        {
                            Console.Error.WriteLine("[WARN] nodeCount >= Root.NumNodes, skipping extra node");
                            // skip the chunk body
                            input.Seek(currentChunk.DataSize, SeekOrigin.Current);
                        }
                        else
                        {
                            // pass by ref so we modify the array entry
                            LoadNode(input, ref data.Tree[nodeCount], currentChunk);

                            data.Tree[nodeCount].Index = nodeCount;
                            nodeCount++;
                        }
                    }
                        break;
                    /*
                    //old
                    case ChunkTypes.NodeV1:
                    {
                        
                        if (data.Tree == null)
                        {
                            data.Tree = new MshTree[data.Root.NumNodes];

                            for (int i = 0; i < data.Tree.Length; i++)
                            {
                                data.Tree[i].Mesh = new MtoolFormat[4];
                            }
                        }

                        if (nodeCount >= data.Tree.Length)
                        {
                            Console.Error.WriteLine("[WARN] nodeCount >= Root.NumNodes, skipping extra node");
                            // skip the chunk body
                            input.Seek(currentChunk.DataSize, SeekOrigin.Current);
                        }
                        else
                        {
                            // pass by ref so we modify the array entry
                            LoadNodeOld(input, ref data.Tree[nodeCount], currentChunk);

                            data.Tree[nodeCount].Index = nodeCount;
                            nodeCount++;
                        }
                    }
                        break;
                    
                    */
                    default:
                        input.Seek(currentChunk.DataSize, SeekOrigin.Current); // skip data
                        Console.WriteLine($"Skipping Chunk Data");
                        break;
                }
            }

            PrintMshData(data, outputDir);
            Console.WriteLine("[INFO] Done.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] " + ex);
            return 2;
        }
    }

    private static void LoadNode(FileStream input, ref MshTree tree, Chunk nodeChunk)
    {
        // chunkSize includes the chunk header itself in this format.
        // after reading the header, the remaining bytes for this chunk are:
        // nodeChunk.ChunkSize - sizeof(Chunk)
        long remaining = nodeChunk.ChunkSize - Marshal.SizeOf<Chunk>();
        long endPosition = input.Position + remaining;

        tree.Node = StreamHelpers.ReadStruct<MshNode>(input);

        Console.WriteLine(tree.Node.Name);

        //each MTOOL instance marks the start of tree.Mesh[], usually, each node chunk would only have one instance of MTOOl_FMT
        int meshCount = 0;

        while (input.Position < endPosition)
        {
            var subChunk = StreamHelpers.ReadStruct<Chunk>(input);
            Console.WriteLine($"Sub-chunk ID: {subChunk.Id}, size: {subChunk.ChunkSize}");

            switch (subChunk.Id)
            {
                case ChunkTypes.MTOOl_FMT:
                {
                    var mesh = StreamHelpers.ReadStruct<MshMesh>(input);

                    var mtool = new MtoolFormat
                    {
                        NumIndices = mesh.NumIndices,
                        NumVertices = mesh.NumVertices,
                        NumSurfaces = mesh.NumSurfaces
                    };

                    if (mtool.Vxyz == null || mtool.Vxyz.Length < 2)
                        throw new InvalidOperationException("Vxyz array not initialized.");

                    mtool.Vxyz[0].Fmt = MvFmt.Float3;
                    mtool.Vxyz[0].BiasScale = new Vec4 { X = 0f, Y = 0f, Z = 0f, W = 1f };
                    mtool.Vxyz[0].Stride = 0xC; // 12 bytes -> 3 floats

                    mtool.VNormalFmt = MvFmt.Float3;
                    mtool.VNormalScale = 1.0f;
                    mtool.VNormalStride = 0xC;

                    mtool.VTangentFmt = MvFmt.Float3;
                    mtool.VTangentScale = 1.0f;
                    mtool.VTangentStride = 0xC;

                    mtool.VBitangentFmt = MvFmt.Float3;
                    mtool.VBitangentScale = 1.0f;
                    mtool.VBitangentStride = 0xC;

                    mtool.VUvFmt = MvFmt.Float2;
                    mtool.VUvScale = 1.0f;
                    mtool.VUvStride = 8u;

                    mtool.Vxyz[1] = default;

                    tree.Mesh[meshCount] = mtool;
                    meshCount++;
                }
                    break;

                case ChunkTypes.VertexFormat:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.Vxyz[0].Fmt = (MvFmt)StreamHelpers.ReadU32(input);

                    // --- v_xyz[0].bias_scale.x/y/z/w (4 floats) ---
                    mtool.Vxyz[0].BiasScale.X = StreamHelpers.ReadSingle(input);
                    mtool.Vxyz[0].BiasScale.Y = StreamHelpers.ReadSingle(input);
                    mtool.Vxyz[0].BiasScale.Z = StreamHelpers.ReadSingle(input);
                    mtool.Vxyz[0].BiasScale.W = StreamHelpers.ReadSingle(input);

                    // --- v_xyz[0].stride (uint) ---
                    mtool.Vxyz[0].Stride = StreamHelpers.ReadU32(input);

                    // copy slot0 -> slot1 (matches the C++ behavior)
                    mtool.Vxyz[1] = mtool.Vxyz[0];

                    // --- normals/tangents/bitangents ---
                    mtool.VNormalFmt = (MvFmt)StreamHelpers.ReadU32(input);
                    mtool.VNormalScale = StreamHelpers.ReadSingle(input);
                    mtool.VNormalStride = StreamHelpers.ReadU32(input);

                    mtool.VTangentFmt = (MvFmt)StreamHelpers.ReadU32(input);
                    mtool.VBitangentFmt = mtool.VTangentFmt;

                    mtool.VTangentScale = StreamHelpers.ReadSingle(input);
                    mtool.VBitangentScale = mtool.VTangentScale;

                    mtool.VTangentStride = StreamHelpers.ReadU32(input);
                    mtool.VBitangentStride = mtool.VTangentStride;

                    // --- UV ---
                    mtool.VUvFmt = (MvFmt)StreamHelpers.ReadU32(input);
                    mtool.VUvScale = StreamHelpers.ReadSingle(input);
                    mtool.VUvStride = StreamHelpers.ReadU32(input);
                }
                    break;

                case ChunkTypes.VertexBuffer:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];
                    if (mtool == null)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }

                    // position buffer: raw bytes (usually 12 * numVertices)
                    mtool.VxyzData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexNormal0:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VNormalData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexTangent0:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VTangentData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexBitangent0:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VBitangentData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexUV0:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VUvData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexUV1:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VUvData1 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.IndexBuffer:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    int indexCount = (int)subChunk.DataSize / 2; // 2 bytes per ushort
                    mtool.Indices = StreamHelpers.ReadU16Array(input, indexCount);
                    break;
                }

                case ChunkTypes.SurfaceDescAlt:
                case ChunkTypes.SurfaceDesc:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    // allocate managed surface array
                    int surfaces = (int)mtool.NumSurfaces;
                    if (surfaces <= 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }

                    var surfArr = new SurfaceDescManaged[surfaces];
                    for (int i = 0; i < surfaces; i++)
                    {
                        var s = new SurfaceDescManaged
                        {
                            MatId = StreamHelpers.ReadU16(input),
                            Offset = StreamHelpers.ReadU32(input),
                            Count = StreamHelpers.ReadU32(input),
                            NumBones = StreamHelpers.ReadU16(input)
                        };

                        s.Bones = s.NumBones > 0 ? StreamHelpers.ReadU16Array(input, s.NumBones) : [];

                        surfArr[i] = s;
                    }

                    mtool.Surfaces = surfArr;
                    break;
                }

                case ChunkTypes.CCollTreePacked:
                case ChunkTypes.CCollTreePackedAlso:
                    // skip these sub-chunks for now
                    input.Seek(subChunk.DataSize, SeekOrigin.Current);
                    break;

                default:
                    input.Seek(subChunk.DataSize, SeekOrigin.Current);
                    Console.WriteLine($"Skipping Sub Chunk Data");
                    break;
            }
        }
    }
    
    //actually called load_node_old in engine lol

    private static void LoadNodeOld(FileStream input, ref MshTree tree, Chunk nodeChunk)
    {
        long remaining = nodeChunk.ChunkSize - Marshal.SizeOf<Chunk>();
        long endPosition = input.Position + remaining;

        tree.Node = MshNodeOld.Convert(StreamHelpers.ReadStruct<MshNodeOld>(input));

        Console.WriteLine(tree.Node.Name);
        Console.WriteLine(tree.Node.Type);
        
        //each MTOOL instance marks the start of tree.Mesh[], usually, each node chunk would only have one instance of MTOOl_FMT
        int meshCount = 0;

        while (input.Position < endPosition)
        {
            var subChunk = StreamHelpers.ReadStruct<Chunk>(input);
            Console.WriteLine($"Sub-chunk ID: {subChunk.Id}, size: {subChunk.ChunkSize}");

            switch (subChunk.Id)
            {
                //implement id 260, 272, 305
                
                case ChunkTypes.MTOOl_FMT:
                {
                    var mesh = StreamHelpers.ReadStruct<MshMesh>(input);

                    var mtool = new MtoolFormat
                    {
                        NumIndices = mesh.NumIndices,
                        NumVertices = mesh.NumVertices,
                        NumSurfaces = mesh.NumSurfaces
                    };

                    if (mtool.Vxyz == null || mtool.Vxyz.Length < 2)
                        throw new InvalidOperationException("Vxyz array not initialized.");

                    mtool.Vxyz[0].Fmt = MvFmt.Float3;
                    mtool.Vxyz[0].BiasScale = new Vec4 { X = 0f, Y = 0f, Z = 0f, W = 1f };
                    mtool.Vxyz[0].Stride = 0xC; // 12 bytes -> 3 floats

                    mtool.VNormalFmt = MvFmt.Float3;
                    mtool.VNormalScale = 1.0f;
                    mtool.VNormalStride = 0xC;

                    mtool.VTangentFmt = MvFmt.Float3;
                    mtool.VTangentScale = 1.0f;
                    mtool.VTangentStride = 0xC;

                    mtool.VBitangentFmt = MvFmt.Float3;
                    mtool.VBitangentScale = 1.0f;
                    mtool.VBitangentStride = 0xC;

                    mtool.VUvFmt = MvFmt.Float2;
                    mtool.VUvScale = 1.0f;
                    mtool.VUvStride = 8u;

                    mtool.Vxyz[1] = default;

                    tree.Mesh[meshCount] = mtool;
                    meshCount++;
                }
                    break;

                case ChunkTypes.VertexFormat:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.Vxyz[0].Fmt = (MvFmt)StreamHelpers.ReadU32(input);

                    // --- v_xyz[0].bias_scale.x/y/z/w (4 floats) ---
                    mtool.Vxyz[0].BiasScale.X = StreamHelpers.ReadSingle(input);
                    mtool.Vxyz[0].BiasScale.Y = StreamHelpers.ReadSingle(input);
                    mtool.Vxyz[0].BiasScale.Z = StreamHelpers.ReadSingle(input);
                    mtool.Vxyz[0].BiasScale.W = StreamHelpers.ReadSingle(input);

                    // --- v_xyz[0].stride (uint) ---
                    mtool.Vxyz[0].Stride = StreamHelpers.ReadU32(input);

                    // copy slot0 -> slot1 (matches the C++ behavior)
                    mtool.Vxyz[1] = mtool.Vxyz[0];

                    // --- normals/tangents/bitangents ---
                    mtool.VNormalFmt = (MvFmt)StreamHelpers.ReadU32(input);
                    mtool.VNormalScale = StreamHelpers.ReadSingle(input);
                    mtool.VNormalStride = StreamHelpers.ReadU32(input);

                    mtool.VTangentFmt = (MvFmt)StreamHelpers.ReadU32(input);
                    mtool.VBitangentFmt = mtool.VTangentFmt;

                    mtool.VTangentScale = StreamHelpers.ReadSingle(input);
                    mtool.VBitangentScale = mtool.VTangentScale;

                    mtool.VTangentStride = StreamHelpers.ReadU32(input);
                    mtool.VBitangentStride = mtool.VTangentStride;

                    // --- UV ---
                    mtool.VUvFmt = (MvFmt)StreamHelpers.ReadU32(input);
                    mtool.VUvScale = StreamHelpers.ReadSingle(input);
                    mtool.VUvStride = StreamHelpers.ReadU32(input);
                }
                    break;

                case ChunkTypes.VertexBuffer:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];
                    if (mtool == null)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }

                    // position buffer: raw bytes (usually 12 * numVertices)
                    mtool.VxyzData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexNormal0:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VNormalData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexTangent0:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VTangentData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexBitangent0:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VBitangentData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexUV0:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VUvData0 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.VertexUV1:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    mtool.VUvData1 = StreamHelpers.ReadBytes(input, (int)subChunk.DataSize);
                    break;
                }

                case ChunkTypes.IndexBuffer:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    int indexCount = (int)subChunk.DataSize / 2; // 2 bytes per ushort
                    mtool.Indices = StreamHelpers.ReadU16Array(input, indexCount);
                    break;
                }

                case ChunkTypes.SurfaceDescAlt:
                case ChunkTypes.SurfaceDesc:
                {
                    if (tree.Mesh == null || meshCount == 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }
                    var mtool = tree.Mesh[meshCount - 1];

                    // allocate managed surface array
                    int surfaces = (int)mtool.NumSurfaces;
                    if (surfaces <= 0)
                    {
                        input.Seek(subChunk.DataSize, SeekOrigin.Current);
                        break;
                    }

                    var surfArr = new SurfaceDescManaged[surfaces];
                    for (int i = 0; i < surfaces; i++)
                    {
                        var s = new SurfaceDescManaged
                        {
                            MatId = StreamHelpers.ReadU16(input),
                            Offset = StreamHelpers.ReadU32(input),
                            Count = StreamHelpers.ReadU32(input),
                            NumBones = StreamHelpers.ReadU16(input)
                        };

                        s.Bones = s.NumBones > 0 ? StreamHelpers.ReadU16Array(input, s.NumBones) : [];

                        surfArr[i] = s;
                    }

                    mtool.Surfaces = surfArr;
                    break;
                }

                case ChunkTypes.CCollTreePacked:
                case ChunkTypes.CCollTreePackedAlso:
                    // skip these sub-chunks for now
                    input.Seek(subChunk.DataSize, SeekOrigin.Current);
                    break;

                default:
                    input.Seek(subChunk.DataSize, SeekOrigin.Current);
                    Console.WriteLine($"Skipping Sub Chunk Data");
                    break;
            }
        }
    }
    
    public static void PrintMshData(MshData data, string? filename = null)
    {
        Console.WriteLine("msh_data:");

        // Root Info
        Console.WriteLine("  Root:");
        Console.WriteLine($"    num_nodes: {data.Root.NumNodes}");
        Console.WriteLine($"    num_materials: {data.Root.NumMaterials}");
        Console.WriteLine($"    num_surface_types: {data.Root.NumSurfaceTypes}");

        // Tree traversal
        if (data.Tree != null)
        {
            for (uint i = 0; i < data.Root.NumNodes && i < data.Tree.Length; ++i)
            {
                var tree = data.Tree[i];
                Console.WriteLine($"  Tree Node [{tree.Index}]:");

                var node = tree.Node;
                // If Name is null/empty we treat node as absent
                if (!string.IsNullOrEmpty(node.Name))
                {
                    Console.WriteLine($"    Name: {node.Name}");
                    Console.WriteLine($"    Type: {node.Type}");
                    Console.WriteLine($"    Parent: {node.Parent}");
                    Console.WriteLine($"    Children: {node.Children}");
                    Console.WriteLine($"    LODs: {node.NumLods}");
                    Console.WriteLine($"    Flags: {node.Flags}");

                    // Local Matrix (3x4)
                    Console.WriteLine("    Local Matrix:");
                    for (int r = 0; r < 3; ++r)
                    {
                        Console.Write("      [ ");
                        for (int c = 0; c < 4; ++c)
                        {
                            float v;
                            try { v = node.Local[r, c]; }
                            catch { v = 0f; }
                            Console.Write($"{v,8:0.0000}");
                            if (c < 3) Console.Write(", ");
                        }
                        Console.WriteLine(" ]");
                    }

                    // Bone Transform Matrix (3x4)
                    Console.WriteLine("    Bone Transform:");
                    for (int r = 0; r < 3; ++r)
                    {
                        Console.Write("      [ ");
                        for (int c = 0; c < 4; ++c)
                        {
                            float v;
                            try { v = node.BoneTransform[r, c]; }
                            catch { v = 0f; }
                            Console.Write($"{v,8:0.0000}");
                            if (c < 3) Console.Write(", ");
                        }
                        Console.WriteLine(" ]");
                    }

                    // Bounds
                    Console.WriteLine("    Bounds:");
                    Console.WriteLine($"      Origin: ({node.Bounds.Origin.X}, {node.Bounds.Origin.Y}, {node.Bounds.Origin.Z})");
                    Console.WriteLine($"      Span:   ({node.Bounds.Span.X}, {node.Bounds.Span.Y}, {node.Bounds.Span.Z})");
                }
                else
                {
                    Console.WriteLine("    (node is empty)");
                }

                // Meshes (up to 4 slots)
                Console.WriteLine("    Meshes:");
                if (tree.Mesh != null)
                {
                    for (int mi = 0; mi < tree.Mesh.Length; ++mi)
                    {
                        var mesh = tree.Mesh[mi];
                        if (mesh != null)
                        {
                            Console.WriteLine($"      Mesh[{mi}]:");
                            Console.WriteLine($"        num_vertices: {mesh.NumVertices}");
                            Console.WriteLine($"        num_indices: {mesh.NumIndices}");
                            Console.WriteLine($"        num_surfaces: {mesh.NumSurfaces}");

                            Console.WriteLine($"        v_normal_fmt: {(int)mesh.VNormalFmt} ({mesh.VNormalFmt})");
                            Console.WriteLine($"        v_normal_stride: {mesh.VNormalStride}");
                            Console.WriteLine($"        v_normal_scale: {mesh.VNormalScale}");

                            Console.WriteLine($"        v_tangent_fmt: {(int)mesh.VTangentFmt} ({mesh.VTangentFmt})");
                            Console.WriteLine($"        v_tangent_stride: {mesh.VTangentStride}");
                            Console.WriteLine($"        v_tangent_scale: {mesh.VTangentScale}");

                            Console.WriteLine($"        v_bitangent_fmt: {(int)mesh.VBitangentFmt} ({mesh.VBitangentFmt})");
                            Console.WriteLine($"        v_bitangent_stride: {mesh.VBitangentStride}");
                            Console.WriteLine($"        v_bitangent_scale: {mesh.VBitangentScale}");

                            Console.WriteLine($"        v_uv_fmt: {(int)mesh.VUvFmt} ({mesh.VUvFmt})");
                            Console.WriteLine($"        v_uv_stride: {mesh.VUvStride}");
                            Console.WriteLine($"        v_uv_scale: {mesh.VUvScale}");
                            

                            // v_uv buffers (managed byte[] or IntPtr depending on your MtoolFormat)
                            try
                            {
                                for (int c = 0; c < 4; ++c)
                                {
                                    // try a few common field names (managed or IntPtr)
                                    string uvDesc = "unknown";
                                    // managed version with VUvDataN
                                    var propName = $"VUvData{c}";
                                    var prop = mesh.GetType().GetProperty(propName);
                                    if (prop != null)
                                    {
                                        var val = prop.GetValue(mesh);
                                        if (val is byte[] b) uvDesc = $"byte[{b.Length}]";
                                        else uvDesc = val == null ? "null" : val.ToString()!;
                                    }
                                    else
                                    {
                                        // fallback: VUv array of IntPtr or object[]
                                        var p2 = mesh.GetType().GetProperty("VUv");
                                        if (p2 != null)
                                        {
                                            var arr = p2.GetValue(mesh) as Array;
                                            if (arr != null && arr.Length > c)
                                            {
                                                var el = arr.GetValue(c);
                                                uvDesc = el == null ? "null" : el.ToString()!;
                                            }
                                        }
                                    }

                                    Console.WriteLine($"        v_uv[{c}]: {uvDesc}");
                                }
                            }
                            catch
                            {
                                // ignore reflective failures
                            }

                            // indices
                            try
                            {
                                var indices = mesh.Indices;
                                Console.WriteLine($"        indices: {(indices != null ? $"ushort[{indices.Length}]" : "null")}");
                            }
                            catch
                            {
                                Console.WriteLine($"        indices: (unknown)");
                            }

                            // surfaces
                            try
                            {
                                var surfaces = mesh.Surfaces;
                                Console.WriteLine($"        surface: {(surfaces != null ? $"present ({surfaces.Length})" : "null")}");
                            }
                            catch
                            {
                                Console.WriteLine($"        surface: (unknown)");
                            }

                            // other bookkeeping
                            Console.WriteLine($"        num_bpv: {mesh.NumBpv}");
                            Console.WriteLine($"        f_matid: {(mesh.FaceMatId != null ? $"ushort[{mesh.FaceMatId.Length}]" : "null")}");
                            Console.WriteLine($"        f_attr: {(mesh.FaceAttr != null ? $"uint[{mesh.FaceAttr.Length}]" : "null")}");

                            // Vertex XYZ data (v_xyz[0..1])
                            for (int vi = 0; vi < 2; ++vi)
                            {
                                Console.WriteLine($"        v_xyz[{vi}]:");
                                try
                                {
                                    var vxyz = mesh.Vxyz != null && mesh.Vxyz.Length > vi ? mesh.Vxyz[vi] : default(Mpack4);
                                    Console.WriteLine($"          fmt: {(int)vxyz.Fmt} ({vxyz.Fmt})");
                                    Console.WriteLine($"          stride: {vxyz.Stride}");
                                    Console.WriteLine($"          bias_scale: ({vxyz.BiasScale.X}, {vxyz.BiasScale.Y}, {vxyz.BiasScale.Z}, {vxyz.BiasScale.W})");
                                }
                                catch
                                {
                                    Console.WriteLine("          (v_xyz info unavailable)");
                                }

                                // Print data buffer length if managed variant exists (VxyzData0 / VxyzData1)
                                try
                                {
                                    var propName = vi == 0 ? "VxyzData0" : "VxyzData1";
                                    var prop = mesh.GetType().GetProperty(propName);
                                    if (prop != null)
                                    {
                                        var val = prop.GetValue(mesh) as byte[];
                                        Console.WriteLine($"          data: {(val != null ? $"byte[{val.Length}]" : "null")}");
                                    }
                                    else
                                    {
                                        // fallback to Mpack4.Data IntPtr
                                        var vxyz = mesh.Vxyz != null && mesh.Vxyz.Length > vi ? mesh.Vxyz[vi] : default(Mpack4);
                                        Console.WriteLine($"          data (ptr): {vxyz.Data}");
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("          data: (unknown)");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"      Mesh[{mi}]: NULL");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("      (no mesh array)");
                }

                // Collision geometry (presence)
                // For managed placeholder ICollTree structs we try to detect non-default via Equals
                try
                {
                    var cg = tree.CollGeom;
                    var ch = tree.CollHull;
                    if (!Equals(cg, default(ICollTree)))
                        Console.WriteLine($"    Tree Collision Geometry: (present)");
                    if (!Equals(ch, default(ICollTree)))
                        Console.WriteLine($"    Tree Collision Hull: (present)");
                }
                catch
                {
                    // ignore
                }
            }
        }
        else
        {
            Console.WriteLine("  (no tree data)");
        }

        // Materials
        Console.WriteLine("  Materials:");
        if (data.Mats != null)
        {
            for (int i = 0; i < data.Mats.Count; ++i)
                Console.WriteLine($"    [{i}]: {data.Mats[i]}");
        }
        else
        {
            Console.WriteLine("    (none)");
        }

        // Surface Types
        Console.WriteLine("  Surface Types:");
        if (data.SurfaceTypes != null)
        {
            for (int i = 0; i < data.SurfaceTypes.Count; ++i)
                Console.WriteLine($"    [{i}]: {data.SurfaceTypes[i]}");
        }
        else
        {
            Console.WriteLine("    (none)");
        }
        GltfExporter.DumpToGltf(data, filename); // <-- commented out per your request
    }
}
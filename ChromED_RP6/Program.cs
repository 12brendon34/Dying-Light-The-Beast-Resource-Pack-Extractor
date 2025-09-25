using System.Diagnostics;
using System.Text;
using Ionic.Zlib;

namespace ChromED_RP6
{
    abstract class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length is < 1 or > 2)
            {
                Console.WriteLine("Usage: {0} input_file.rpack [output_dir]", AppDomain.CurrentDomain.FriendlyName);
                return 1;
            }

            string inputFile = args[0];
            string outputDir = args.Length >= 2
                ? args[1]
                : Path.Combine(Path.GetDirectoryName(inputFile) ?? ".", Path.GetFileNameWithoutExtension(inputFile));

            try
            {
                Directory.CreateDirectory(outputDir);
                RpackProcessor.ProcessRpack(inputFile, outputDir);
                Console.WriteLine("[INFO] Done.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[ERROR] " + ex);
                return 2;
            }
        }
    }

    static class RpackProcessor
    {
        public static void ProcessRpack(string inputFile, string outputDir)
        {
            using var input = File.OpenRead(inputFile);
            long fileLength = input.Length;

            var mainHeader = Utils.StructReader.ReadStruct<RDPMainHeader>(input);

            var definedTypes = Utils.ReadArray<RDPResourceTypeHeader>(input, (int)mainHeader.PhysResTypeCount);
            var physEntries = Utils.ReadArray<RDPResourceEntryHeader>(input, (int)mainHeader.PhysResCount);
            var logHeaders = Utils.ReadArray<RDPLogicalResourceEntryHeader>(input, (int)mainHeader.ResourceNamesCount);

            // names offsets
            uint[] namesIndices = new uint[mainHeader.ResourceNamesCount];
            for (int i = 0; i < namesIndices.Length; i++)
                namesIndices[i] = input.ReadUInt32();

            if (mainHeader.ResourceNamesBlockSize > int.MaxValue)
                throw new InvalidDataException("Invalid names block size.");

            int namesBlockSize = (int)mainHeader.ResourceNamesBlockSize;
            byte[] namesBufBytes = new byte[namesBlockSize];
            int actuallyRead = input.Read(namesBufBytes, 0, namesBlockSize);
            if (actuallyRead != namesBlockSize)
                throw new EndOfStreamException($"Unable to read names block: wanted {namesBlockSize}, read {actuallyRead}.");
            
            string namesBuffer = Encoding.ASCII.GetString(namesBufBytes);

            // decompress / read defined types
            var decompressedSections = DecompressDefinedTypes(input, definedTypes, fileLength);
            // extract logical resources and write them
            ExtractLogicalResources(input, physEntries, logHeaders, namesBuffer, namesIndices, definedTypes, decompressedSections, outputDir);
        }

        // Helper to convert 16-byte-units -> bytes
        private static ulong UnitsToBytes(long units)
        {
            // convert via ulong to avoid sign/overflow surprises when shifting
            return ((ulong)units) << 4;
        }

        private static List<byte[]?> DecompressDefinedTypes(Stream input, RDPResourceTypeHeader[] definedTypes, long fileLength)
        {
            var result = new List<byte[]?>(definedTypes.Length);

            for (int i = 0; i < definedTypes.Length; i++)
            {
                var dt = definedTypes[i];
                long dataFileOffsetUnits = dt.DataFileOffset; // unit value (16-byte units)
                long compressedSize = dt.CompressedByteSize; // bytes
                long uncompressedSize = dt.DataByteSize; // bytes

                ulong dataFileOffsetBytesU = UnitsToBytes(dataFileOffsetUnits);
                if (dataFileOffsetBytesU > long.MaxValue)
                {
                    result.Add(null);
                    continue;
                }
                long dataFileOffsetBytes = (long)dataFileOffsetBytesU;
                
                if (dataFileOffsetBytes > fileLength)
                {
                    Console.Error.WriteLine($"[WARN] invalid dataFileOffset for defined type {i}: {dataFileOffsetUnits} units ({dataFileOffsetBytes} bytes). Skipping.");
                    result.Add(null);
                    continue;
                }

                if (compressedSize > 0)
                {
                    if (dataFileOffsetBytes + compressedSize > fileLength)
                    {
                        Console.Error.WriteLine($"[WARN] compressed blob for type {i} extends beyond file; skipping.");
                        result.Add(null);
                        continue;
                    }

                    input.Seek(dataFileOffsetBytes, SeekOrigin.Begin);
                    byte[] compressedBuf = new byte[compressedSize];
                    int cRead = input.Read(compressedBuf, 0, (int)compressedSize);
                    if (cRead != compressedSize)
                    {
                        Console.Error.WriteLine($"[WARN] short read of compressed blob for type {i}: {cRead}/{compressedSize}.");
                        result.Add(null);
                        continue;
                    }

                    try
                    {
                        if (compressedBuf is [0x78, ..]) // zlib (0x78 is common zlib header)
                        {
                            using var mem = new MemoryStream(compressedBuf);
                            using var z = new ZlibStream(mem, CompressionMode.Decompress, true);
                            byte[] outBuf = new byte[uncompressedSize];
                            int got = 0;
                            while (got < outBuf.Length)
                            {
                                int r = z.Read(outBuf, got, outBuf.Length - got);
                                if (r <= 0) break;
                                got += r;
                            }

                            if (got != outBuf.Length)
                                Console.Error.WriteLine($"[WARN] zlib produced {got}/{outBuf.Length} bytes for type {i}.");
                            
                            result.Add(outBuf);
                            Debug.WriteLine($"[INFO] DefinedType[{i}] zlib-decompressed: {uncompressedSize} bytes.");
                        }
                        else
                        {
                            using var mem = new MemoryStream(compressedBuf);
                            using var decoder = new Lzma.DecoderStream(mem);
                            decoder.Initialize(Lzma.DecoderProperties.Default);
                            byte[] outBuf = new byte[uncompressedSize];
                            int got = 0;
                            while (got < outBuf.Length)
                            {
                                int r = decoder.Read(outBuf, got, outBuf.Length - got);
                                if (r <= 0) break;
                                got += r;
                            }

                            if (got != outBuf.Length)
                                Console.Error.WriteLine($"[WARN] LZMA produced {got}/{outBuf.Length} bytes for type {i}.");
                            
                            result.Add(outBuf);
                            Console.WriteLine($"[INFO] DefinedType[{i}] LZMA-decompressed: {uncompressedSize} bytes.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[ERROR] decompressing defined type {i}: {ex.Message}");
                        result.Add(null);
                    }
                }
                else
                {
                    result.Add(null);
                    Debug.WriteLine($"[INFO] DefinedType[{i}] not compressed (offset {dataFileOffsetUnits} units / {dataFileOffsetBytes} bytes, size {uncompressedSize}).");
                }
            }

            return result;
        }

        private static void ExtractLogicalResources(Stream input, RDPResourceEntryHeader[] physEntries, RDPLogicalResourceEntryHeader[] logHeaders, string namesBuffer, uint[] namesIndices, RDPResourceTypeHeader[] definedTypes, List<byte[]?> decompressedSections, string outputRoot)
        {
            for (int i = 0; i < logHeaders.Length; i++)
            {
                var logHeader = logHeaders[i];
                uint filetype = (logHeader.Bitfields >> 16) & 0xFFu;
                int entryCount = (int)(logHeader.Bitfields & 0xFFu);
                int currentResource = (int)logHeader.FirstResource;

                string fullText = Utils.GetNullTerminatedString(namesBuffer, (int)namesIndices[i]);
                string baseName = Utils.SanitizeFileName(fullText);
                string typeName = Utils.GetResourceName((int)filetype);

                var fileParts = new List<byte[]>();
                for (int p = 0; p < entryCount; p++)
                {
                    if (currentResource < 0 || currentResource >= physEntries.Length)
                    {
                        Console.Error.WriteLine($"[WARN] physical resource index {currentResource} out of range.");
                        break;
                    }

                    var phys = physEntries[currentResource];
                    int physSection = (int)(phys.Bitfields & 0xFFu);
                    long dataSize = phys.DataByteSize;
                    long dataOffsetUnits = phys.DataOffset; // units (16-byte units)
                    
                    bool sectionCompressed = false;
                    if (physSection >= 0 && physSection < definedTypes.Length)
                    {
                        long sectionBaseUnits = definedTypes[physSection].DataFileOffset; // units
                        sectionCompressed = definedTypes[physSection].CompressedByteSize > 0;

                        // convert and add
                        ulong sectionBaseBytesU = UnitsToBytes(sectionBaseUnits);
                        ulong partOffsetBytesU = UnitsToBytes(dataOffsetUnits);
                        ulong fileOffsetU = sectionBaseBytesU + partOffsetBytesU;
                        if (fileOffsetU > long.MaxValue)
                            throw new InvalidDataException("Computed file offset too large.");
                    }

                    if (sectionCompressed)
                    {
                        currentResource++;
                        continue;
                    }

                    byte[] part = new byte[dataSize];

                    if (physSection >= 0 && physSection < decompressedSections.Count && decompressedSections[physSection] != null)
                    {
                        byte[] dec = decompressedSections[physSection]!;
                        if (UnitsToBytes(dataOffsetUnits) + (ulong)dataSize > (ulong)dec.Length)
                        {
                            Console.Error.WriteLine($"[WARN] requested slice outside decompressed buffer (section {physSection}).");
                            break;
                        }

                        Buffer.BlockCopy(dec, (int)UnitsToBytes(dataOffsetUnits), part, 0, (int)dataSize);
                        Debug.WriteLine($"[INFO] Read part {p} from decompressed section {physSection} offset {dataOffsetUnits} units ({UnitsToBytes(dataOffsetUnits)} bytes) size {dataSize}");
                    }
                    else
                    {
                        if (physSection < 0 || physSection >= definedTypes.Length)
                        {
                            Console.Error.WriteLine($"[WARN] invalid physSection {physSection} for part {p}");
                            break;
                        }

                        long baseOffsetUnits = definedTypes[physSection].DataFileOffset;
                        ulong absoluteOffsetU = UnitsToBytes(baseOffsetUnits) + UnitsToBytes(dataOffsetUnits);
                        if (absoluteOffsetU > long.MaxValue)
                        {
                            Console.Error.WriteLine($"[WARN] computed absolute offset too large.");
                            break;
                        }

                        long absoluteOffsetBytes = (long)absoluteOffsetU;

                        if (absoluteOffsetBytes + dataSize > input.Length)
                        {
                            Console.Error.WriteLine($"[WARN] attempted file read outside bounds: offset={absoluteOffsetBytes}, size={dataSize}");
                            break;
                        }

                        input.Seek(absoluteOffsetBytes, SeekOrigin.Begin);
                        int read = input.Read(part, 0, (int)dataSize);
                        if (read != dataSize)
                        {
                            Console.Error.WriteLine($"[WARN] short read for part {p}: {read}/{dataSize}");
                            break;
                        }

                        Debug.WriteLine($"[INFO] Read part {p} from file offset {absoluteOffsetBytes} size {dataSize}");
                    }

                    fileParts.Add(part);
                    currentResource++;
                } // end parts loop

                if (fileParts.Count == 0)
                    continue;

                string resourceOutputDir = Path.Combine(outputRoot, typeName); //baseName, typeName);
                Directory.CreateDirectory(resourceOutputDir);
                
                var info = new ResourceInfo
                {
                    LogicalIndex = i,
                    BaseName = baseName,
                    TypeName = typeName,
                    FileType = (int)filetype,
                    Parts = fileParts,
                    OutputDir = resourceOutputDir
                };

                ResourceWriter.WriteResource(info);
            } // end logHeaders loop
        }

        private class ResourceInfo
        {
            public int LogicalIndex { get; init; }
            public required string BaseName { get; init; }
            public required string TypeName { get; init; }
            public int FileType { get; init; }
            public List<byte[]> Parts { get; init; } = [];
            public string OutputDir { get; init; } = ".";
        }

        private static class ResourceWriter
        {
            private static readonly Dictionary<int, Action<ResourceInfo>> Handlers = new()
            {
                { (int)Utils.ResourceType.Texture, WriteTexture },
                { (int)Utils.ResourceType.Animation, WriteAnimation },
                //{ (int)Utils.ResourceType.Fx, WriteFx }, //removed
                { (int)Utils.ResourceType.BuilderInformation, WriteBuilderInformation }
                // add more mappings here
            };

            public static void WriteResource(ResourceInfo info)
            {
                if (Handlers.TryGetValue(info.FileType, out var handler))
                {
                    try
                    {
                        handler(info);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[ERROR] writing resource {info.BaseName} type={info.TypeName}: {ex.Message}");
                        // fallback to binary dump
                        WriteBinary(info);
                    }
                }
                else
                {
                    WriteBinary(info);
                }
            }

            private static void WriteTexture(ResourceInfo info)
            {
                if (info.Parts.Count < 2)
                {
                    Console.Error.WriteLine($"[WARN] Texture {info.BaseName} does not have enough parts.");
                    return;
                }
                
                string outputFile = Path.Combine(info.OutputDir, info.BaseName + ".dds");
                outputFile = Utils.MakeUniqueFilename(outputFile);

                using var textureHeaderStream = new MemoryStream(info.Parts[0]);
                var textureHeader = Utils.StructReader.ReadStruct<RTextureInfo>(textureHeaderStream);
                
                var infoFmt = Utils.FormatInfo.Get(textureHeader.Format);
                uint pitchOrLinearSize;
                
                if (infoFmt.IsBlockCompressed)
                {
                    // block count = ceil(width/4) * ceil(height/4)
                    int blockWidth = (textureHeader.Width + 3) / 4;
                    int blockHeight = (textureHeader.Height + 3) / 4;
                    pitchOrLinearSize = (uint)(blockWidth * blockHeight * infoFmt.BlockSizeBytes);
                    
                }
                else
                {
                    pitchOrLinearSize = (uint)((textureHeader.Width * infoFmt.BytesPerPixel + 3) & ~3);
                }
                
                // DDS constants
                const uint DDS_MAGIC = 0x20534444; // "DDS "
                const uint DDS_HEADER_SIZE = 124;
                const uint DDSCAPS_TEXTURE = 0x1000;
                const uint DDSCAPS_MIPMAP = 0x00400000;
                const uint DDSCAPS_COMPLEX = 0x00000008;
                
                const uint DDSD_CAPS = 0x1;
                const uint DDSD_HEIGHT = 0x2;
                const uint DDSD_WIDTH = 0x4;
                const uint DDSD_PITCH = 0x8;
                const uint DDSD_PIXELFORMAT = 0x1000;
                const uint DDSD_MIPMAPCOUNT = 0x20000;
                const uint DDSD_LINEARSIZE = 0x80000;
                //const uint DDSD_DEPTH = 0x800000;
                
                uint ddsFlags = DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT;
                if (infoFmt.IsBlockCompressed)
                    ddsFlags |= DDSD_LINEARSIZE;
                else
                    ddsFlags |= DDSD_PITCH;

                if (textureHeader.MipLevels > 1)
                    ddsFlags |= DDSD_MIPMAPCOUNT;
                
                // Clamp mip levels
                uint mipCount = textureHeader.MipLevels == 0 ? 1u : textureHeader.MipLevels;
                
                var header = new DDS.DDS_HEADER
                {
                    Size = DDS_HEADER_SIZE,
                    Flags = ddsFlags,
                    Height = textureHeader.Height,
                    Width = textureHeader.Width,
                    PitchOrLinearSize = pitchOrLinearSize,
                    Depth = textureHeader.Depth,
                    MipMapCount = mipCount,
                    Reserved1 = new uint[11],
                    PixelFormat = DDS.GetPixelFormat(textureHeader.Format),
                    Caps = DDSCAPS_TEXTURE | (textureHeader.MipLevels > 1 ? DDSCAPS_MIPMAP | DDSCAPS_COMPLEX : 0),
                    Caps2 = 0,
                    Caps3 = 0,
                    Caps4 = 0,
                    Reserved2 = 0
                };

                const uint DDSCAPS2_VOLUME = 0x00200000;
                const uint DDSCAPS2_CUBEMAP = 0x00000200;
                const uint DDSCAPS2_CUBEMAP_POSITIVEX = 0x00000400;
                const uint DDSCAPS2_CUBEMAP_NEGATIVEX = 0x00000800;
                const uint DDSCAPS2_CUBEMAP_POSITIVEY = 0x00001000;
                const uint DDSCAPS2_CUBEMAP_NEGATIVEY = 0x00002000;
                const uint DDSCAPS2_CUBEMAP_POSITIVEZ = 0x00004000;
                const uint DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x00008000;
                const uint DDSCAPS2_CUBEMAP_ALLFACES = (DDSCAPS2_CUBEMAP_POSITIVEX | DDSCAPS2_CUBEMAP_NEGATIVEX | DDSCAPS2_CUBEMAP_POSITIVEY | DDSCAPS2_CUBEMAP_NEGATIVEY | DDSCAPS2_CUBEMAP_POSITIVEZ | DDSCAPS2_CUBEMAP_NEGATIVEZ);
                
                switch(textureHeader.TexType)
                {
                    case 1: header.Caps2 = DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_ALLFACES; break;
                    case 2: header.Caps2 = DDSCAPS2_VOLUME; break;
                }

                // extended DX10 header if needed
                var dx10Header = new DDS.DDS_HEADER_DX10
                {
                    DxgiFormat = Utils.GetDXGIFormat(textureHeader.Format),
                    ResourceDimension = DDS.D3D10_RESOURCE_DIMENSION.Texture2D,
                    MiscFlag = 0,
                    ArraySize = 1,
                    MiscFlags2 = 0
                };
                
                if (header.PixelFormat.FourCC == DDS.MakeFourCC("DX10"))
                {
                    if (dx10Header.DxgiFormat == DDS.DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                    {
                        Console.Error.WriteLine($"[WARN] Texture {info.BaseName}, with textureHeader.Format of {textureHeader.Format} does not have matching DxgiFormat.");
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine($"[INFO] Texture {info.BaseName}, with textureHeader.Format of {textureHeader.Format} supports only DX9.");
                }

                using var output = File.OpenWrite(outputFile);

                // write magic
                Utils.WriteU32(output, DDS_MAGIC);

                // write header
                Utils.WriteU32(output, header.Size);
                Utils.WriteU32(output, header.Flags);
                Utils.WriteU32(output, header.Height);
                Utils.WriteU32(output, header.Width);
                Utils.WriteU32(output, header.PitchOrLinearSize);
                Utils.WriteU32(output, header.Depth);
                Utils.WriteU32(output, header.MipMapCount);

                foreach (uint v in header.Reserved1)
                    Utils.WriteU32(output, v);

                // pixel format
                Utils.WriteU32(output, header.PixelFormat.Size);
                Utils.WriteU32(output, header.PixelFormat.Flags);
                Utils.WriteU32(output, header.PixelFormat.FourCC);
                Utils.WriteU32(output, header.PixelFormat.RGBBitCount);
                Utils.WriteU32(output, header.PixelFormat.RBitMask);
                Utils.WriteU32(output, header.PixelFormat.GBitMask);
                Utils.WriteU32(output, header.PixelFormat.BBitMask);
                Utils.WriteU32(output, header.PixelFormat.ABitMask);

                // caps
                Utils.WriteU32(output, header.Caps);
                Utils.WriteU32(output, header.Caps2);
                Utils.WriteU32(output, header.Caps3);
                Utils.WriteU32(output, header.Caps4);
                Utils.WriteU32(output, header.Reserved2);

                // optional DX10
                if (header.PixelFormat.FourCC == DDS.MakeFourCC("DX10"))
                {
                    Utils.WriteU32(output, (uint)dx10Header.DxgiFormat);
                    Utils.WriteU32(output, (uint)dx10Header.ResourceDimension);
                    Utils.WriteU32(output, dx10Header.MiscFlag);
                    Utils.WriteU32(output, dx10Header.ArraySize);
                    Utils.WriteU32(output, dx10Header.MiscFlags2);
                }

                // write texture data
                output.Write(info.Parts[1], 0, info.Parts[1].Length);
                Debug.WriteLine($"[OUT] Wrote {outputFile} ({new FileInfo(outputFile).Length} bytes)");
            }

            private static void WriteAnimation(ResourceInfo info)
            {
                if (info.Parts.Count > 1)
                    Console.Error.WriteLine($"[WARN] Animation {info.BaseName} contains unexpected parts, data loss may occur.");

                byte[] part = info.Parts[0];
                string outName = info.BaseName + ".anm2";

                string outputFile = Path.Combine(info.OutputDir, outName);
                outputFile = Utils.MakeUniqueFilename(outputFile);

                File.WriteAllBytes(outputFile, part);
            }

            //may not be used anymore
            private static void WriteBuilderInformation(ResourceInfo info)
            {
                for (int f = 0; f < info.Parts.Count; f++)
                {
                    byte[] part = info.Parts[f];
                    string outName = info.BaseName + ".txt";
                    if (f >= 1)
                    {
                        outName = info.BaseName + "_Part_" + f.ToString() + ".txt";
                    }

                    string outputFile = Path.Combine(info.OutputDir, outName);
                    outputFile = Utils.MakeUniqueFilename(outputFile);

                    File.WriteAllBytes(outputFile, part);
                }
            }
            
            //default
            private static void WriteBinary(ResourceInfo info)
            {
                int index = 0;
                foreach (byte[] part in info.Parts)
                {
                    string filename = Path.Combine(info.OutputDir, $"{info.LogicalIndex:D4}_{info.BaseName}_part{index:D2}.bin");

                    filename = Utils.MakeUniqueFilename(filename);
                    File.WriteAllBytes(filename, part);
                    index++;
                }
            }
            
            
            
            
            /*
            private static void WriteBinary(ResourceInfo info)
            {
                string filename = Path.Combine(info.OutputDir, $"{info.LogicalIndex:D4}_{info.BaseName}.bin");

                filename = Utils.MakeUniqueFilename(filename);

                using var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
                foreach (byte[] part in info.Parts)
                {
                    fs.Write(part, 0, part.Length);
                }
            }
            */
        }
    }
}
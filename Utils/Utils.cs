using System.Runtime.InteropServices;
using System.Text;

namespace Utils.IO
{
    public enum Endianness
    {
        LittleEndian,
        BigEndian
    }

    public static class StreamHelpers
    {
        public static T[] ReadArray<T>(Stream s, int count) where T : struct
        {
            var arr = new T[count];
            for (int i = 0; i < count; i++)
                arr[i] = ReadStruct<T>(s);
            return arr;
        }

        public static T ReadStruct<T>(Stream stream) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = new byte[size];
            int bytesRead = stream.Read(buffer, 0, size);
            if (bytesRead != size)
                throw new EndOfStreamException($"Expected {size} bytes, got {bytesRead}.");

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject())!;
            }
            finally
            {
                handle.Free();
            }
        }

        public static uint ReadU32(Stream stream, Endianness endianness = Endianness.LittleEndian)
        {
            Span<byte> buf = stackalloc byte[4];
            int read = stream.Read(buf);
            if (read != 4) throw new EndOfStreamException("Failed to read 4 bytes for uint32");
            if (BitConverter.IsLittleEndian && endianness == Endianness.BigEndian
                || !BitConverter.IsLittleEndian && endianness == Endianness.LittleEndian)
            {
                buf.Reverse();
            }
            return BitConverter.ToUInt32(buf);
        }
        public static byte[] ReadBytes(Stream stream, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0) throw new EndOfStreamException($"Expected {count} bytes, got {offset}.");
                offset += read;
            }
            return buffer;
        }

        public static ushort ReadU16(Stream stream, Endianness endianness = Endianness.LittleEndian)
        {
            byte[] buffer = new byte[2];
            int read = stream.Read(buffer, 0, 2);
            if (read != 2) throw new EndOfStreamException("Failed to read 2 bytes for UInt16");
            bool needReverse = (BitConverter.IsLittleEndian && endianness == Endianness.BigEndian)
                               || (!BitConverter.IsLittleEndian && endianness == Endianness.LittleEndian);
            if (needReverse) Array.Reverse(buffer);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public static ushort[] ReadU16Array(Stream stream, int count, Endianness endianness = Endianness.LittleEndian)
        {
            if (count <= 0) return Array.Empty<ushort>();
            var result = new ushort[count];
            for (int i = 0; i < count; i++) result[i] = ReadU16(stream, endianness);
            return result;
        }


        public static byte ReadU8(Stream stream)
        {
            int b = stream.ReadByte();
            if (b == -1) throw new EndOfStreamException("Failed to read 1 byte");
            return (byte)b;
        }
        
        public static float ReadSingle(Stream stream, Endianness endianness = Endianness.LittleEndian)
        {
            byte[] buffer = new byte[4];
            int read = stream.Read(buffer, 0, 4);
            if (read != 4) throw new EndOfStreamException("Failed to read 4 bytes for Single");
            bool needReverse = (BitConverter.IsLittleEndian && endianness == Endianness.BigEndian)
                               || (!BitConverter.IsLittleEndian && endianness == Endianness.LittleEndian);
            if (needReverse) Array.Reverse(buffer);
            return BitConverter.ToSingle(buffer, 0);
        }

        public static T ReadEnum32<T>(Stream stream) where T : Enum
        {
            uint raw = ReadU32(stream, Endianness.LittleEndian);
            return (T)Enum.ToObject(typeof(T), raw);
        }

        public static string ReadString(Stream stream, Encoding encoding, int size)
        {
            using var reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true);
            byte[] array = reader.ReadBytes(size);

            // Check for incomplete reads
            return array.Length != size ? throw new EndOfStreamException("Unexpected end of stream while reading string.") : encoding.GetString(array);
        }

        public static void WriteU32(FileStream stream, uint value, Endianness endianness = Endianness.LittleEndian)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            bool needReverse = (BitConverter.IsLittleEndian && endianness == Endianness.BigEndian)
                               || (!BitConverter.IsLittleEndian && endianness == Endianness.LittleEndian);
            if (needReverse) Array.Reverse(bytes);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}

namespace Utils.Files
{
    public static class FileHelpers
    {
        public static string GetNullTerminatedString(string buffer, int offset)
        {
            if (offset < 0 || offset >= buffer.Length)
                return string.Empty;

            int end = buffer.IndexOf('\0', offset);
            if (end == -1)
                end = buffer.Length;

            return buffer.Substring(offset, end - offset);
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unnamed";
            return Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c, '_'));
        }

        public static string MakeUniqueFilename(string path)
        {
            string dir = Path.GetDirectoryName(path) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            string candidate = path;
            int i = 1;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(dir, $"{baseName}_{i}{ext}");
                i++;
            }
            return candidate;
        }
    }
}

namespace Utils.Resources
{
    public static class ResourceTypeInfo
    {
//texture
        public enum EFormat : byte
        {
            R8_UNORM = 0,
            R8_SNORM,
            R8_UINT,
            R8_SINT,
            A8_UNORM,
            L8,
            R16_FLOAT,
            R16_UNORM,
            R16_SNORM,
            R16_UINT,
            R16_SINT,
            L16,
            R32_FLOAT,
            R32_UINT,
            R32_SINT,
            R8G8_UNORM,
            R8G8_SNORM,
            R8G8_UINT,
            R8G8_SINT,
            R16G16_FLOAT,
            R16G16_UNORM,
            R16G16_SNORM,
            R16G16_UINT,
            R16G16_SINT,
            R32G32_FLOAT,
            R32G32_UINT,
            R32G32_SINT,
            R5G6B5,
            R8G8B8,
            B8G8R8,
            R11G11B10_FLOAT,
            B32G32R32F,
            A8R8G8B8,
            A8R8G8B8_GAMMA,
            X8R8G8B8,
            B8G8R8A8,
            B8G8R8X8,
            X8B8G8R8,
            R8G8B8A8_UNORM,
            R8G8B8A8_SNORM,
            R8G8B8A8_UINT,
            R8G8B8A8_SINT,
            A2R10G10B10,
            A2R10G10B10_GAMMA,
            R10G10B10A2_UNORM,
            R10G10B10A2_UINT,
            R16G16B16A16_FLOAT,
            R16G16B16A16_UNORM,
            R16G16B16A16_SNORM,
            R16G16B16A16_UINT,
            R16G16B16A16_SINT,
            R32G32B32A32_FLOAT,
            R32G32B32A32_UINT,
            R32G32B32A32_SINT,
            D16_UNORM,
            D24_UNORM_S8_UINT,
            D32_FLOAT,
            D24FS8,
            D32_FLOAT_S8X24_UINT,
            BC1_UNORM,
            BC2_UNORM,
            BC3_UNORM,
            BC4_SNORM,
            BC4_UNORM,
            BC5_SNORM,
            BC5_UNORM,
            BC6H_UF16,
            BC6H_SF16,
            BC7_UNORM,
            R8_UNORM_NO_TYPELESS
        }

        private static readonly Dictionary<EFormat, DDS.DXGI_FORMAT> TextureToDXGIMap = new()
        {
            { EFormat.R8_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R8_UNORM },
            { EFormat.R8_SNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R8_SNORM },
            { EFormat.R8_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R8_UINT },
            { EFormat.R8_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R8_SINT },
            { EFormat.A8_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
            { EFormat.L8, DDS.DXGI_FORMAT.DXGI_FORMAT_R8_UNORM }, // Luminance -> map to single-channel R8
            { EFormat.R16_FLOAT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT },
            { EFormat.R16_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R16_UNORM },
            { EFormat.R16_SNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R16_SNORM },
            { EFormat.R16_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16_UINT },
            { EFormat.R16_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16_SINT },
            { EFormat.L16, DDS.DXGI_FORMAT.DXGI_FORMAT_R16_UNORM }, // Luminance16 -> map to R16_UNORM
            { EFormat.R32_FLOAT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT },
            { EFormat.R32_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32_UINT },
            { EFormat.R32_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32_SINT },

            { EFormat.R8G8_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM },
            { EFormat.R8G8_SNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM },
            { EFormat.R8G8_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_UINT },
            { EFormat.R8G8_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_SINT },

            { EFormat.R16G16_FLOAT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT },
            { EFormat.R16G16_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM },
            { EFormat.R16G16_SNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16_SNORM },
            { EFormat.R16G16_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16_UINT },
            { EFormat.R16G16_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16_SINT },

            { EFormat.R32G32_FLOAT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT },
            { EFormat.R32G32_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32_UINT },
            { EFormat.R32G32_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32_SINT },

            { EFormat.R11G11B10_FLOAT, DDS.DXGI_FORMAT.DXGI_FORMAT_R11G11B10_FLOAT },
            { EFormat.B32G32R32F, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT }, // channel-order differs (BGR -> RGB); shader swizzle may be required

            { EFormat.R8G8B8A8_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM },
            { EFormat.R8G8B8A8_SNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SNORM },
            { EFormat.R8G8B8A8_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT },
            { EFormat.R8G8B8A8_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT },

            { EFormat.A2R10G10B10, DDS.DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM }, // D3D9 ordering may differ; use R10G10B10A2
            { EFormat.A2R10G10B10_GAMMA, DDS.DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM }, // no SRGB variant in DXGI for this format; gamma must be handled externally

            { EFormat.R10G10B10A2_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM },
            { EFormat.R10G10B10A2_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UINT },

            { EFormat.R16G16B16A16_FLOAT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT },
            { EFormat.R16G16B16A16_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM },
            { EFormat.R16G16B16A16_SNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM },
            { EFormat.R16G16B16A16_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT },
            { EFormat.R16G16B16A16_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT },

            { EFormat.R32G32B32A32_FLOAT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT },
            { EFormat.R32G32B32A32_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT },
            { EFormat.R32G32B32A32_SINT, DDS.DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT },

            // Depth/stencil
            { EFormat.D16_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_D16_UNORM },
            { EFormat.D24_UNORM_S8_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_D24_UNORM_S8_UINT },
            { EFormat.D32_FLOAT, DDS.DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT },
            { EFormat.D24FS8, DDS.DXGI_FORMAT.DXGI_FORMAT_R24_UNORM_X8_TYPELESS }, // best-effort typeless variant for 24-bit depth + 8 bits (legacy)
            { EFormat.D32_FLOAT_S8X24_UINT, DDS.DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT },

            // BC (block-compressed)
            { EFormat.BC1_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
            { EFormat.BC2_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
            { EFormat.BC3_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
            { EFormat.BC4_SNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM },
            { EFormat.BC4_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM },
            { EFormat.BC5_SNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM },
            { EFormat.BC5_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM },
            { EFormat.BC6H_UF16, DDS.DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16 },
            { EFormat.BC6H_SF16, DDS.DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16 },
            { EFormat.BC7_UNORM, DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM },

            // Single-channel R8 fallback for "no typeless" variant
            { EFormat.R8_UNORM_NO_TYPELESS, DDS.DXGI_FORMAT.DXGI_FORMAT_R8_UNORM }


            // All other legacy formats are handled in DDS.GetPixelFormat
        };

        public static DDS.DXGI_FORMAT GetDXGIFormat(EFormat textureFormat)
        {
            return TextureToDXGIMap.GetValueOrDefault(textureFormat, DDS.DXGI_FORMAT.DXGI_FORMAT_UNKNOWN);
        }

        public static class FormatInfo
        {
            public struct Info
            {
                public int BytesPerPixel; // for uncompressed formats
                public bool IsBlockCompressed; // true if BC/DXT/etc.
                public int BlockSizeBytes; // size of one 4x4 block if compressed
            }

            public static Info Get(EFormat fmt)
            {
                return fmt switch
                {
                    // --- single 8-bit channel ---
                    EFormat.R8_UNORM or
                        EFormat.R8_SNORM or
                        EFormat.R8_UINT or
                        EFormat.R8_SINT or
                        EFormat.A8_UNORM or
                        EFormat.L8 or
                        EFormat.R8_UNORM_NO_TYPELESS
                        => new Info { BytesPerPixel = 1 },

                    // --- single 16-bit channel ---
                    EFormat.R16_FLOAT or
                        EFormat.R16_UNORM or
                        EFormat.R16_SNORM or
                        EFormat.R16_UINT or
                        EFormat.R16_SINT or
                        EFormat.L16
                        => new Info { BytesPerPixel = 2 },

                    // --- single 32-bit channel ---
                    EFormat.R32_FLOAT or
                        EFormat.R32_UINT or
                        EFormat.R32_SINT
                        => new Info { BytesPerPixel = 4 },

                    // --- 2-channel 8-bit pairs (2 bytes total) ---
                    EFormat.R8G8_UNORM or
                        EFormat.R8G8_SNORM or
                        EFormat.R8G8_UINT or
                        EFormat.R8G8_SINT
                        => new Info { BytesPerPixel = 2 },

                    // --- 2-channel 16-bit pairs (4 bytes total) ---
                    EFormat.R16G16_FLOAT or
                        EFormat.R16G16_UNORM or
                        EFormat.R16G16_SNORM or
                        EFormat.R16G16_UINT or
                        EFormat.R16G16_SINT
                        => new Info { BytesPerPixel = 4 },

                    // --- 2-channel 32-bit pairs (8 bytes total) ---
                    EFormat.R32G32_FLOAT or
                        EFormat.R32G32_UINT or
                        EFormat.R32G32_SINT
                        => new Info { BytesPerPixel = 8 },

                    // --- 3-channel formats ---
                    EFormat.R5G6B5 => new Info { BytesPerPixel = 2 }, // 16-bit packed
                    EFormat.R8G8B8 or
                        EFormat.B8G8R8 => new Info { BytesPerPixel = 3 }, // 24-bit packed (legacy)
                    EFormat.B32G32R32F => new Info { BytesPerPixel = 12 }, // 3×32-bit floats

                    // --- packed 32-bit 3- or 4-channel formats (4 bytes total) ---
                    EFormat.R11G11B10_FLOAT => new Info { BytesPerPixel = 4 }, // packed 32-bit
                    EFormat.A8R8G8B8 or
                        EFormat.A8R8G8B8_GAMMA or
                        EFormat.X8R8G8B8 or
                        EFormat.B8G8R8A8 or
                        EFormat.B8G8R8X8 or
                        EFormat.X8B8G8R8 or
                        EFormat.R8G8B8A8_UNORM or
                        EFormat.R8G8B8A8_SNORM or
                        EFormat.R8G8B8A8_UINT or
                        EFormat.R8G8B8A8_SINT or
                        EFormat.A2R10G10B10 or
                        EFormat.A2R10G10B10_GAMMA or
                        EFormat.R10G10B10A2_UNORM or
                        EFormat.R10G10B10A2_UINT
                        => new Info { BytesPerPixel = 4 },

                    // --- 4 × 16-bit channels (8 bytes total) ---
                    EFormat.R16G16B16A16_FLOAT or
                        EFormat.R16G16B16A16_UNORM or
                        EFormat.R16G16B16A16_SNORM or
                        EFormat.R16G16B16A16_UINT or
                        EFormat.R16G16B16A16_SINT
                        => new Info { BytesPerPixel = 8 },

                    // --- 4 × 32-bit channels (16 bytes total) ---
                    EFormat.R32G32B32A32_FLOAT or
                        EFormat.R32G32B32A32_UINT or
                        EFormat.R32G32B32A32_SINT
                        => new Info { BytesPerPixel = 16 },

                    // --- Depth / stencil combos (padded sizes) ---
                    EFormat.D16_UNORM
                        => new Info { BytesPerPixel = 2 }, // 16-bit depth

                    EFormat.D24_UNORM_S8_UINT or
                        EFormat.D24FS8
                        => new Info { BytesPerPixel = 4 }, // 24-bit depth + 8-bit stencil (packed into 4 bytes)

                    EFormat.D32_FLOAT
                        => new Info { BytesPerPixel = 4 }, // 32-bit depth

                    EFormat.D32_FLOAT_S8X24_UINT
                        => new Info { BytesPerPixel = 8 }, // typically represented as 64 bits in packed forms

                    // --- Block compressed (BC) ---
                    EFormat.BC1_UNORM
                        => new Info { IsBlockCompressed = true, BlockSizeBytes = 8 }, // BC1 / DXT1: 8 bytes per 4x4 block

                    EFormat.BC2_UNORM or
                        EFormat.BC3_UNORM or
                        EFormat.BC5_SNORM or
                        EFormat.BC5_UNORM or
                        EFormat.BC6H_UF16 or
                        EFormat.BC6H_SF16 or
                        EFormat.BC7_UNORM
                        => new Info { IsBlockCompressed = true, BlockSizeBytes = 16 }, // BC2/3/5/6/7: 16 bytes per 4x4 block

                    EFormat.BC4_SNORM or
                        EFormat.BC4_UNORM
                        => new Info { IsBlockCompressed = true, BlockSizeBytes = 8 }, // BC4: 8 bytes per 4x4 block

                    // Unknown / not handled
                    _ => throw new NotSupportedException($"Format {fmt} not handled")
                };
            }
        }
    }
}
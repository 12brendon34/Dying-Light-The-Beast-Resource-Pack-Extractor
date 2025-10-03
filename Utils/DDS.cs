
using Utils.Resources;

namespace Utils;

/// <summary>
/// DirectDraw Surface (DDS) format definitions.
/// Based on: https://github.com/Microsoft/DirectXTex/blob/main/DirectXTex/DDS.h
/// </summary>
public static class DDS
{
    // Structs
    public struct DDS_HEADER
    {
        public uint Size;
        public uint Flags;
        public uint Height;
        public uint Width;
        public uint PitchOrLinearSize;
        public uint Depth; // Only if DDS_HEADER_FLAGS_VOLUME is set
        public uint MipMapCount;
        public uint[] Reserved1;
        public DDS_PIXELFORMAT PixelFormat;
        public uint Caps;
        public uint Caps2;
        public uint Caps3;
        public uint Caps4;
        public uint Reserved2;
    }

    public struct DDS_PIXELFORMAT
    {
        public uint Size;
        public uint Flags;
        public uint FourCC;
        public uint RGBBitCount;
        public uint RBitMask;
        public uint GBitMask;
        public uint BBitMask;
        public uint ABitMask;
    }

    public struct DDS_HEADER_DX10
    {
        public DXGI_FORMAT DxgiFormat;
        public D3D10_RESOURCE_DIMENSION ResourceDimension;
        public uint MiscFlag;
        public uint ArraySize;
        public uint MiscFlags2;
    }

    // Enums
    [Flags]
    internal enum PixelFormatFlags : uint
    {
        AlphaPixels   = 0x00000001,
        Alpha         = 0x00000002,
        FourCC        = 0x00000004,
        Palette8      = 0x00000020,
        Palette8Alpha = 0x00000021,
        Rgb           = 0x00000040,
        Rgba          = 0x00000041,
        Luminance     = 0x00020000,
        LuminanceA    = 0x00020001,
        BumpLuminance = 0x00040000,
        BumpDuDv      = 0x00080000,
        BumpDuDvA     = 0x00080001
    }

    public enum D3D10_RESOURCE_DIMENSION : uint
    {
        Unknown   = 0,
        Buffer    = 1,
        Texture1D = 2,
        Texture2D = 3,
        Texture3D = 4
    }

    // Full DXGI_FORMAT enum preserved
    public enum DXGI_FORMAT : uint
    {
        DXGI_FORMAT_UNKNOWN = 0,
        DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
        DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
        DXGI_FORMAT_R32G32B32A32_UINT = 3,
        DXGI_FORMAT_R32G32B32A32_SINT = 4,
        DXGI_FORMAT_R32G32B32_TYPELESS = 5,
        DXGI_FORMAT_R32G32B32_FLOAT = 6,
        DXGI_FORMAT_R32G32B32_UINT = 7,
        DXGI_FORMAT_R32G32B32_SINT = 8,
        DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
        DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
        DXGI_FORMAT_R16G16B16A16_UNORM = 11,
        DXGI_FORMAT_R16G16B16A16_UINT = 12,
        DXGI_FORMAT_R16G16B16A16_SNORM = 13,
        DXGI_FORMAT_R16G16B16A16_SINT = 14,
        DXGI_FORMAT_R32G32_TYPELESS = 15,
        DXGI_FORMAT_R32G32_FLOAT = 16,
        DXGI_FORMAT_R32G32_UINT = 17,
        DXGI_FORMAT_R32G32_SINT = 18,
        DXGI_FORMAT_R32G8X24_TYPELESS = 19,
        DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
        DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
        DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
        DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
        DXGI_FORMAT_R10G10B10A2_UNORM = 24,
        DXGI_FORMAT_R10G10B10A2_UINT = 25,
        DXGI_FORMAT_R11G11B10_FLOAT = 26,
        DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
        DXGI_FORMAT_R8G8B8A8_UNORM = 28,
        DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
        DXGI_FORMAT_R8G8B8A8_UINT = 30,
        DXGI_FORMAT_R8G8B8A8_SNORM = 31,
        DXGI_FORMAT_R8G8B8A8_SINT = 32,
        DXGI_FORMAT_R16G16_TYPELESS = 33,
        DXGI_FORMAT_R16G16_FLOAT = 34,
        DXGI_FORMAT_R16G16_UNORM = 35,
        DXGI_FORMAT_R16G16_UINT = 36,
        DXGI_FORMAT_R16G16_SNORM = 37,
        DXGI_FORMAT_R16G16_SINT = 38,
        DXGI_FORMAT_R32_TYPELESS = 39,
        DXGI_FORMAT_D32_FLOAT = 40,
        DXGI_FORMAT_R32_FLOAT = 41,
        DXGI_FORMAT_R32_UINT = 42,
        DXGI_FORMAT_R32_SINT = 43,
        DXGI_FORMAT_R24G8_TYPELESS = 44,
        DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
        DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
        DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
        DXGI_FORMAT_R8G8_TYPELESS = 48,
        DXGI_FORMAT_R8G8_UNORM = 49,
        DXGI_FORMAT_R8G8_UINT = 50,
        DXGI_FORMAT_R8G8_SNORM = 51,
        DXGI_FORMAT_R8G8_SINT = 52,
        DXGI_FORMAT_R16_TYPELESS = 53,
        DXGI_FORMAT_R16_FLOAT = 54,
        DXGI_FORMAT_D16_UNORM = 55,
        DXGI_FORMAT_R16_UNORM = 56,
        DXGI_FORMAT_R16_UINT = 57,
        DXGI_FORMAT_R16_SNORM = 58,
        DXGI_FORMAT_R16_SINT = 59,
        DXGI_FORMAT_R8_TYPELESS = 60,
        DXGI_FORMAT_R8_UNORM = 61,
        DXGI_FORMAT_R8_UINT = 62,
        DXGI_FORMAT_R8_SNORM = 63,
        DXGI_FORMAT_R8_SINT = 64,
        DXGI_FORMAT_A8_UNORM = 65,
        DXGI_FORMAT_R1_UNORM = 66,
        DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
        DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
        DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
        DXGI_FORMAT_BC1_TYPELESS = 70,
        DXGI_FORMAT_BC1_UNORM = 71,
        DXGI_FORMAT_BC1_UNORM_SRGB = 72,
        DXGI_FORMAT_BC2_TYPELESS = 73,
        DXGI_FORMAT_BC2_UNORM = 74,
        DXGI_FORMAT_BC2_UNORM_SRGB = 75,
        DXGI_FORMAT_BC3_TYPELESS = 76,
        DXGI_FORMAT_BC3_UNORM = 77,
        DXGI_FORMAT_BC3_UNORM_SRGB = 78,
        DXGI_FORMAT_BC4_TYPELESS = 79,
        DXGI_FORMAT_BC4_UNORM = 80,
        DXGI_FORMAT_BC4_SNORM = 81,
        DXGI_FORMAT_BC5_TYPELESS = 82,
        DXGI_FORMAT_BC5_UNORM = 83,
        DXGI_FORMAT_BC5_SNORM = 84,
        DXGI_FORMAT_B5G6R5_UNORM = 85,
        DXGI_FORMAT_B5G5R5A1_UNORM = 86,
        DXGI_FORMAT_B8G8R8A8_UNORM = 87,
        DXGI_FORMAT_B8G8R8X8_UNORM = 88,
        DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
        DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
        DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
        DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
        DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
        DXGI_FORMAT_BC6H_TYPELESS = 94,
        DXGI_FORMAT_BC6H_UF16 = 95,
        DXGI_FORMAT_BC6H_SF16 = 96,
        DXGI_FORMAT_BC7_TYPELESS = 97,
        DXGI_FORMAT_BC7_UNORM = 98,
        DXGI_FORMAT_BC7_UNORM_SRGB = 99,
        DXGI_FORMAT_AYUV = 100,
        DXGI_FORMAT_Y410 = 101,
        DXGI_FORMAT_Y416 = 102,
        DXGI_FORMAT_NV12 = 103,
        DXGI_FORMAT_P010 = 104,
        DXGI_FORMAT_P016 = 105,
        DXGI_FORMAT_420_OPAQUE = 106,
        DXGI_FORMAT_YUY2 = 107,
        DXGI_FORMAT_Y210 = 108,
        DXGI_FORMAT_Y216 = 109,
        DXGI_FORMAT_NV11 = 110,
        DXGI_FORMAT_AI44 = 111,
        DXGI_FORMAT_IA44 = 112,
        DXGI_FORMAT_P8 = 113,
        DXGI_FORMAT_A8P8 = 114,
        DXGI_FORMAT_B4G4R4A4_UNORM = 115,
        DXGI_FORMAT_P208 = 130,
        DXGI_FORMAT_V208 = 131,
        DXGI_FORMAT_V408 = 132,
        DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 189,
        DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 190,
        DXGI_FORMAT_FORCE_UINT = 0xffffffff
    }
    
    // Methods
    public static uint MakeFourCC(string fourcc)
    {
        if (string.IsNullOrEmpty(fourcc) || fourcc.Length != 4)
            throw new ArgumentException("FourCC string must be 4 characters long.", nameof(fourcc));

        return (uint)(fourcc[0] | (fourcc[1] << 8) | (fourcc[2] << 16) | (fourcc[3] << 24));
    }

    /// <summary>
    /// Gets the DDS_PIXELFORMAT definition for ResourceTypeInfo.EFormat.
    /// </summary>
    public static DDS_PIXELFORMAT GetPixelFormat(ResourceTypeInfo.EFormat textureFormat)
    {
        const uint DDS_PIXELFORMAT_SIZE = 32;

        var pixelFormat = new DDS_PIXELFORMAT
        {
            Size = DDS_PIXELFORMAT_SIZE,
            Flags = (uint)PixelFormatFlags.FourCC,
            FourCC = MakeFourCC("DX10")
        };

        switch (textureFormat)
        {
            // --- DX9 legacy formats (previously commented out) ---
            case ResourceTypeInfo.EFormat.R5G6B5:
                pixelFormat = CreateBitmaskFormat(PixelFormatFlags.Rgb, 16, 0xF800, 0x07E0, 0x001F, 0);
                break;

            case ResourceTypeInfo.EFormat.R8G8B8:
                pixelFormat = CreateBitmaskFormat(PixelFormatFlags.Rgb, 24, 0x00FF0000, 0x0000FF00, 0x000000FF, 0);
                break;

            case ResourceTypeInfo.EFormat.B8G8R8:
                pixelFormat = CreateBitmaskFormat(PixelFormatFlags.Rgb, 24, 0x000000FF, 0x0000FF00, 0x00FF0000, 0);
                break;

            case ResourceTypeInfo.EFormat.A8R8G8B8:
            case ResourceTypeInfo.EFormat.A8R8G8B8_GAMMA:
                pixelFormat = CreateBitmaskFormat(PixelFormatFlags.Rgba, 32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);
                break;

            case ResourceTypeInfo.EFormat.X8B8G8R8:
                pixelFormat = CreateBitmaskFormat(PixelFormatFlags.Rgb, 32, 0x0000FF00, 0x00FF0000, 0xFF000000, 0);
                break;

            case ResourceTypeInfo.EFormat.B8G8R8A8:
            case ResourceTypeInfo.EFormat.B8G8R8X8:
                pixelFormat = CreateBitmaskFormat(PixelFormatFlags.Rgba, 32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
                break;

            case ResourceTypeInfo.EFormat.X8R8G8B8:
                pixelFormat = CreateBitmaskFormat(PixelFormatFlags.Rgb, 32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0);
                break;

            case ResourceTypeInfo.EFormat.D24FS8:
                pixelFormat = CreateBitmaskFormat(PixelFormatFlags.Luminance, 32, 0xFFFFFF00, 0, 0, 0x000000FF);
                break;

            // All other formats default to DX10 FourCC
        }

        return pixelFormat;
    }
    private static DDS_PIXELFORMAT CreateBitmaskFormat(PixelFormatFlags flags, uint bitCount, uint rMask, uint gMask, uint bMask, uint aMask)
    {
        return new DDS_PIXELFORMAT
        {
            Size = 32,
            Flags = (uint)flags,
            FourCC = 0,
            RGBBitCount = bitCount,
            RBitMask = rMask,
            GBitMask = gMask,
            BBitMask = bMask,
            ABitMask = aMask
        };
    }
}
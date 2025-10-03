namespace ChromED_RP6;

//in c++ this is a namespace
public class EResType
{
    public enum Type : uint
    {
        Invalid = 0,
        Mesh = 16,
        MeshFixups = 17, //new
        Skin = 18,
        Model = 24,
        Texture = 32,
        TextureBitmapData = 33,
        TextureMipBitmapData = 34,
        Material = 48,
        Shader = 49,
        Animation = 64,
        AnimationStream = 65,
        AnimationScr = 66,
        AnimationScrFixups = 67,
        ANM2Header = 68,
        ANM2Payload = 69,
        ANM2Fallback = 70,
        AnimGraphBank = 71,
        AnimGraphBankFixups = 72,
        AnimCustomResource = 73,
        AnimCustomResourceFixups = 74,
        GpuFx = 81,
        EnvprobeBin = 85,
        VoxelizerBin = 86, //new
        Area = 90,
        PrefabText = 96,
        Prefab = 97,
        PrefabFixUps = 98,
        Sound = 101,
        Music = 102,
        Speech = 103,
        SFX_stream = 104,
        SFX_local = 105,
        VertexData = 240,
        IndexData = 241,
        GeometryData = 242,
        ClothData = 243, //new
        TinyObjects = 248,
        BuilderInformation = 255
    }

    private readonly struct Entry(
        string name,
        string longName,
        string shortName,
        string prettyName,
        Type id,
        ushort memCategory,
        ushort version)
    {
        public string Name { get; } = name;
        public string LongName { get; } = longName;
        public string ShortName { get; } = shortName;
        public string PrettyName { get; } = prettyName;
        public Type Id { get; } = id;
        public ushort MemCategory { get; } = memCategory;
        public ushort Version { get; } = version;
    }

    private static readonly Entry[] Table = new[]
    {
        new Entry("EResType::Invalid", "_INVALID_", "INVALID", "Invalid", 0, 0, 1),
        new Entry("EResType::Mesh", "_MESH_", "MESH", "Mesh", Type.Mesh, 84, 60),
        new Entry("EResType::MeshFixups", "_MESH_FIXUPS_", "MESH_FIX", "MeshFixups", Type.MeshFixups, 84, 60),
        new Entry("EResType::Skin", "_SKIN_", "SKIN", "Skin", Type.Skin, 85, 13),
        new Entry("EResType::Model", "_MODEL_", "MODEL", "Model", Type.Model, 83, 3),
        new Entry("EResType::Texture", "_TEXTURE_", "TEXTURE", "Texture", Type.Texture, 109, 11),
        new Entry("EResType::TextureBitmapData", "_TEXTURE_BITMAP_DATA_", "BITMAP", "TextureBitmapData", Type.TextureBitmapData, 116, 11),
        new Entry("EResType::TextureMipBitmapData", "_TEXTURE_MIP_BITMAP_DATA_", "STRMBMP", "TextureMipBitmapData", Type.TextureMipBitmapData, 116, 11),
        new Entry("EResType::Material", "_MATERIAL_", "MATERIAL", "Material", Type.Material, 82, 13),
        new Entry("EResType::Shader", "_SHADER_", "SHADER", "Shader", Type.Shader, 114, 13),
        new Entry("EResType::Animation", "_ANIMATION_", "ANIM", "Animation", Type.Animation, 5, 4),
        new Entry("EResType::AnimationStream", "_ANIMATION_STREAM_", "ANIMSTRM", "AnimationStream", Type.AnimationStream, 5, 4),
        new Entry("EResType::AnimationScr", "_ANIMATION_SCR_", "ANIMSCR", "AnimationScr", Type.AnimationScr, 8, 4),
        new Entry("EResType::AnimationScrFixups", "_ANIMATION_SCRFIXUPS_", "ANIMSFIX", "AnimationScrFixups", Type.AnimationScrFixups, 8, 4),
        new Entry("EResType::ANM2Header", "_ANM2_METADATA_", "ANM_META", "ANM2Header", Type.ANM2Header, 5, 2),
        new Entry("EResType::ANM2Payload", "_ANM2_PAYLOAD_", "ANM_DATA", "ANM2Payload", Type.ANM2Payload, 5, 2),
        new Entry("EResType::ANM2Fallback", "_ANM2_FALLBACK_", "ANM_FLBK", "ANM2Fallback", Type.ANM2Fallback, 5, 2),
        new Entry("EResType::AnimGraphBank", "_ANIM_GRAPH_BANK_", "ANMGRAPH", "AnimGraphBank", Type.AnimGraphBank, 11, 140),
        new Entry("EResType::AnimGraphBankFixups", "_ANIM_GRAPH_BANK_FIXUPS_", "AGRPHFIX", "AnimGraphBankFixups", Type.AnimGraphBankFixups, 11, 140),
        new Entry("EResType::AnimCustomResource", "_ANIM_CUSTOM_RESOURCE_", "ACSTMRES", "AnimCustomResource", Type.AnimCustomResource, 14, 4),
        new Entry("EResType::AnimCustomResourceFixups", "_ANIM_CUSTOM_RESOURCE_FIXUPS_", "ACRESFIX", "AnimCustomResourceFixups", Type.AnimCustomResourceFixups, 14, 4),
        new Entry("EResType::GpuFx", "_GPUFX_", "GPUFX", "GpuFx", Type.GpuFx, 178, 2),
        new Entry("EResType::EnvprobeBin", "_ENV_BIN_", "ENV_BIN", "EnvprobeBin", Type.EnvprobeBin, 77, 2),
        new Entry("EResType::VoxelizerBin", "_VXL_BIN_", "VXL_BIN", "VoxelizerBin", Type.VoxelizerBin, 77, 2),
        new Entry("EResType::Area", "_AREA_", "AREA", "Area", Type.Area, 122, 2),
        new Entry("EResType::PrefabText", "_PREFAB_TEXT_", "PRFBTXT", "PrefabText", Type.PrefabText, 148, 2),
        new Entry("EResType::Prefab", "_PREFAB_", "PREFAB", "Prefab", Type.Prefab, 148, 8),
        new Entry("EResType::PrefabFixUps", "_PREFAB_DATA_FIXUPS_", "PRFBFXUP", "PrefabFixUps", Type.PrefabFixUps, 148, 8),
        new Entry("EResType::Sound", "_SOUND_", "SOUND", "Sound", Type.Sound, 18, 2),
        new Entry("EResType::Music", "_SOUND_MUSIC_", "MUSIC", "Music", Type.Music, 18, 2),
        new Entry("EResType::Speech", "_SOUND_SPEECH_", "SPEECH", "Speech", Type.Speech, 18, 2),
        new Entry("EResType::SFX_stream", "_SOUND_STREAM_", "SNDSTRM", "SFX_stream", Type.SFX_stream, 18, 2),
        new Entry("EResType::SFX_local", "_SOUND_LOCAL_", "SNDLOCAL", "SFX_local", Type.SFX_local, 18, 2),
        new Entry("EResType::VertexData", "_VERTEX_DATA_", "VERTEXES", "VertexData", Type.VertexData, 115, 5),
        new Entry("EResType::IndexData", "_INDEX_DATA_", "INDEXES", "IndexData", Type.IndexData, 115, 4),
        new Entry("EResType::GeometryData", "_GEOMETRY_DATA_", "GEOMETRY", "GeometryData", Type.GeometryData, 115, 4),
        new Entry("EResType::ClothData", "_CLOTH_DATA_", "CLOTH", "ClothData", Type.ClothData, 115, 2),
        new Entry("EResType::TinyObjects", "_TINY_OBJECTS_", "TINYOBJS", "TinyObjects", Type.TinyObjects, 75, 8),
        new Entry("EResType::BuilderInformation", "_BUILDER_INFORMATION_", "BUILDER", "BuilderInformation", Type.BuilderInformation, 107, 2),
    };
    
    public static Type FromInt(int param)
    {
        uint id = (uint)param;
        foreach (var e in Table)
        {
            if (e.Id != (Type)id)
                continue;
            if (string.Equals(e.Name, "_INVALID_", StringComparison.Ordinal))
                return Type.Invalid;
            return e.Id;
        }
        return Type.Invalid;
    }

    public static Type GetByName(string name)
    {
        return string.IsNullOrEmpty(name) ? Type.Invalid : (from e in Table where string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase) select e.Id).FirstOrDefault();
    }

    public static string GetName(Type t)
    {
        foreach (var e in Table)
        {
            if (e.Id == t) return e.Name;
        }
        return "_INVALID_";
    }
    
    public static string GetPrettyName(Type t)
    {
        foreach (var e in Table)
        {
            if (e.Id == t) return e.PrettyName;
        }
        return "_INVALID_";
    }
}
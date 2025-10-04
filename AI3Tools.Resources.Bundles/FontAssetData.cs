using AssetsTools.NET;
using MessagePack;

namespace AI3Tools;

[MessagePackObject]
public partial class FontAssetData : IWriteTo
{
    [Key(00)] public string? m_Version;
    [Key(01)] public FaceInfo? m_FaceInfo;
    [Key(02)] public Glyph[]? m_GlyphTable;
    [Key(03)] public Character[]? m_CharacterTable;
    [Key(04)] public GlyphRect[]? m_UsedGlyphRects;
    [Key(05)] public GlyphRect[]? m_FreeGlyphRects;
    [Key(06)] public int m_AtlasWidth;
    [Key(07)] public int m_AtlasHeight;
    [Key(08)] public int m_AtlasPadding;
    [Key(09)] public int m_AtlasRenderMode;
    [Key(10)] public float normalStyle;
    [Key(11)] public float normalSpacingOffset;
    [Key(12)] public float boldStyle;
    [Key(13)] public float boldSpacing;
    [Key(14)] public byte italicStyle;
    [Key(15)] public byte tabSize;

    [MessagePackObject]
    public class FaceInfo
    {
        [Key(00)] public int m_FaceIndex;
        [Key(01)] public string? m_FamilyName;
        [Key(02)] public string? m_StyleName;
        [Key(03)] public int m_PointSize;
        [Key(04)] public float m_Scale;
        [Key(05)] public float m_LineHeight;
        [Key(06)] public float m_AscentLine;
        [Key(07)] public float m_CapLine;
        [Key(08)] public float m_MeanLine;
        [Key(09)] public float m_Baseline;
        [Key(10)] public float m_DescentLine;
        [Key(11)] public float m_SuperscriptOffset;
        [Key(12)] public float m_SuperscriptSize;
        [Key(13)] public float m_SubscriptOffset;
        [Key(14)] public float m_SubscriptSize;
        [Key(15)] public float m_UnderlineOffset;
        [Key(16)] public float m_UnderlineThickness;
        [Key(17)] public float m_StrikethroughOffset;
        [Key(18)] public float m_StrikethroughThickness;
        [Key(19)] public float m_TabWidth;
    }

    [MessagePackObject]
    public class Glyph
    {
        [Key(0)] public uint m_Index;
        [Key(1)] public GlyphMetrics? m_Metrics;
        [Key(2)] public GlyphRect? m_GlyphRect;
        [Key(3)] public float m_Scale;
        [Key(4)] public int m_AtlasIndex;
        [Key(5)] public int m_ClassDefinitionType;
    }

    [MessagePackObject]
    public class GlyphMetrics
    {
        [Key(0)] public float m_Width;
        [Key(1)] public float m_Height;
        [Key(2)] public float m_HorizontalBearingX;
        [Key(3)] public float m_HorizontalBearingY;
        [Key(4)] public float m_HorizontalAdvance;
    }

    [MessagePackObject]
    public class GlyphRect
    {
        [Key(0)] public int m_X;
        [Key(1)] public int m_Y;
        [Key(2)] public int m_Width;
        [Key(3)] public int m_Height;
    }

    [MessagePackObject]
    public class Character
    {
        [Key(0)] public byte m_ElementType;
        [Key(1)] public uint m_Unicode;
        [Key(2)] public int m_GlyphIndex;
        [Key(3)] public float m_Scale;
    }

    public static FontAssetData ReadFontAsset(AssetTypeValueField baseField)
    {
        var data = new FontAssetData();
        baseField["m_Version"].Read(ref data.m_Version);
        baseField["m_FaceInfo"].Read(ref data.m_FaceInfo, ReadFaceInfo);
        baseField["m_GlyphTable"].Read(ref data.m_GlyphTable, ReadGlyph);
        baseField["m_CharacterTable"].Read(ref data.m_CharacterTable, ReadCharacter);
        baseField["m_UsedGlyphRects"].Read(ref data.m_UsedGlyphRects, ReadGlyphRect);
        baseField["m_FreeGlyphRects"].Read(ref data.m_FreeGlyphRects, ReadGlyphRect);
        baseField["m_AtlasWidth"].Read(ref data.m_AtlasWidth);
        baseField["m_AtlasHeight"].Read(ref data.m_AtlasHeight);
        baseField["m_AtlasPadding"].Read(ref data.m_AtlasPadding);
        baseField["m_AtlasRenderMode"].Read(ref data.m_AtlasRenderMode);
        baseField["normalStyle"].Read(ref data.normalStyle);
        baseField["normalSpacingOffset"].Read(ref data.normalSpacingOffset);
        baseField["boldStyle"].Read(ref data.boldStyle);
        baseField["boldSpacing"].Read(ref data.boldSpacing);
        baseField["italicStyle"].Read(ref data.italicStyle);
        baseField["tabSize"].Read(ref data.tabSize);
        return data;
    }

    private static FaceInfo ReadFaceInfo(AssetTypeValueField baseField)
    {
        var data = new FaceInfo();
        baseField["m_FaceIndex"].Read(ref data.m_FaceIndex);
        baseField["m_FamilyName"].Read(ref data.m_FamilyName);
        baseField["m_StyleName"].Read(ref data.m_StyleName);
        baseField["m_PointSize"].Read(ref data.m_PointSize);
        baseField["m_Scale"].Read(ref data.m_Scale);
        baseField["m_LineHeight"].Read(ref data.m_LineHeight);
        baseField["m_AscentLine"].Read(ref data.m_AscentLine);
        baseField["m_CapLine"].Read(ref data.m_CapLine);
        baseField["m_MeanLine"].Read(ref data.m_MeanLine);
        baseField["m_Baseline"].Read(ref data.m_Baseline);
        baseField["m_DescentLine"].Read(ref data.m_DescentLine);
        baseField["m_SuperscriptOffset"].Read(ref data.m_SuperscriptOffset);
        baseField["m_SuperscriptSize"].Read(ref data.m_SuperscriptSize);
        baseField["m_SubscriptOffset"].Read(ref data.m_SubscriptOffset);
        baseField["m_SubscriptSize"].Read(ref data.m_SubscriptSize);
        baseField["m_UnderlineOffset"].Read(ref data.m_UnderlineOffset);
        baseField["m_UnderlineThickness"].Read(ref data.m_UnderlineThickness);
        baseField["m_StrikethroughOffset"].Read(ref data.m_StrikethroughOffset);
        baseField["m_StrikethroughThickness"].Read(ref data.m_StrikethroughThickness);
        baseField["m_TabWidth"].Read(ref data.m_TabWidth);
        return data;
    }

    private static Glyph ReadGlyph(AssetTypeValueField baseField)
    {
        var data = new Glyph();
        baseField["m_Index"].Read(ref data.m_Index);
        baseField["m_Metrics"].Read(ref data.m_Metrics, ReadGlyphMetrics);
        baseField["m_GlyphRect"].Read(ref data.m_GlyphRect, ReadGlyphRect);
        baseField["m_Scale"].Read(ref data.m_Scale);
        baseField["m_AtlasIndex"].Read(ref data.m_AtlasIndex);
        baseField["m_ClassDefinitionType"].Read(ref data.m_ClassDefinitionType);
        return data;
    }

    private static GlyphMetrics ReadGlyphMetrics(AssetTypeValueField baseField)
    {
        var data = new GlyphMetrics();
        baseField["m_Width"].Read(ref data.m_Width);
        baseField["m_Height"].Read(ref data.m_Height);
        baseField["m_HorizontalBearingX"].Read(ref data.m_HorizontalBearingX);
        baseField["m_HorizontalBearingY"].Read(ref data.m_HorizontalBearingY);
        baseField["m_HorizontalAdvance"].Read(ref data.m_HorizontalAdvance);
        return data;
    }

    private static GlyphRect ReadGlyphRect(AssetTypeValueField baseField)
    {
        var data = new GlyphRect();
        baseField["m_X"].Read(ref data.m_X);
        baseField["m_Y"].Read(ref data.m_Y);
        baseField["m_Width"].Read(ref data.m_Width);
        baseField["m_Height"].Read(ref data.m_Height);
        return data;
    }

    private static Character ReadCharacter(AssetTypeValueField baseField)
    {
        var data = new Character();
        baseField["m_ElementType"].Read(ref data.m_ElementType);
        baseField["m_Unicode"].Read(ref data.m_Unicode);
        baseField["m_GlyphIndex"].Read(ref data.m_GlyphIndex);
        baseField["m_Scale"].Read(ref data.m_Scale);
        return data;
    }

    void IWriteTo.WriteTo(AssetFieldWriter baseField) => WriteTo(baseField, this);

    private static void WriteTo(AssetFieldWriter baseField, FontAssetData data)
    {
        baseField["m_FaceInfo"].Write(data.m_FaceInfo, WriteTo);
        baseField["m_GlyphTable"].Write(data.m_GlyphTable, WriteTo);
        baseField["m_CharacterTable"].Write(data.m_CharacterTable, WriteTo);
        baseField["m_UsedGlyphRects"].Write(data.m_UsedGlyphRects, WriteTo);
        baseField["m_FreeGlyphRects"].Write(data.m_FreeGlyphRects, WriteTo);
        baseField["m_AtlasWidth"].Write(data.m_AtlasWidth);
        baseField["m_AtlasHeight"].Write(data.m_AtlasHeight);
        baseField["m_AtlasPadding"].Write(data.m_AtlasPadding);
        baseField["m_AtlasRenderMode"].Write(data.m_AtlasRenderMode);
        baseField["normalStyle"].Write(data.normalStyle);
        baseField["normalSpacingOffset"].Write(data.normalSpacingOffset);
        baseField["boldStyle"].Write(data.boldStyle);
        baseField["boldSpacing"].Write(data.boldSpacing);
        baseField["italicStyle"].Write(data.italicStyle);
        baseField["tabSize"].Write(data.tabSize);
    }

    private static void WriteTo(AssetFieldWriter baseField, FaceInfo data)
    {
        baseField["m_FaceIndex"].Write(data.m_FaceIndex);
        baseField["m_FamilyName"].Write(data.m_FamilyName);
        baseField["m_StyleName"].Write(data.m_StyleName);
        baseField["m_PointSize"].Write(data.m_PointSize);
        baseField["m_Scale"].Write(data.m_Scale);
        baseField["m_LineHeight"].Write(data.m_LineHeight);
        baseField["m_AscentLine"].Write(data.m_AscentLine);
        baseField["m_CapLine"].Write(data.m_CapLine);
        baseField["m_MeanLine"].Write(data.m_MeanLine);
        baseField["m_Baseline"].Write(data.m_Baseline);
        baseField["m_DescentLine"].Write(data.m_DescentLine);
        baseField["m_SuperscriptOffset"].Write(data.m_SuperscriptOffset);
        baseField["m_SuperscriptSize"].Write(data.m_SuperscriptSize);
        baseField["m_SubscriptOffset"].Write(data.m_SubscriptOffset);
        baseField["m_SubscriptSize"].Write(data.m_SubscriptSize);
        baseField["m_UnderlineOffset"].Write(data.m_UnderlineOffset);
        baseField["m_UnderlineThickness"].Write(data.m_UnderlineThickness);
        baseField["m_StrikethroughOffset"].Write(data.m_StrikethroughOffset);
        baseField["m_StrikethroughThickness"].Write(data.m_StrikethroughThickness);
        baseField["m_TabWidth"].Write(data.m_TabWidth);
    }

    private static void WriteTo(AssetFieldWriter baseField, Glyph data)
    {
        baseField["m_Index"].Write(data.m_Index);
        baseField["m_Metrics"].Write(data.m_Metrics, WriteTo);
        baseField["m_GlyphRect"].Write(data.m_GlyphRect, WriteTo);
        baseField["m_Scale"].Write(data.m_Scale);
        baseField["m_AtlasIndex"].Write(data.m_AtlasIndex);
        baseField["m_ClassDefinitionType"].Write(data.m_ClassDefinitionType);
    }

    private static void WriteTo(AssetFieldWriter baseField, GlyphMetrics data)
    {
        baseField["m_Width"].Write(data.m_Width);
        baseField["m_Height"].Write(data.m_Height);
        baseField["m_HorizontalBearingX"].Write(data.m_HorizontalBearingX);
        baseField["m_HorizontalBearingY"].Write(data.m_HorizontalBearingY);
        baseField["m_HorizontalAdvance"].Write(data.m_HorizontalAdvance);
    }

    private static void WriteTo(AssetFieldWriter baseField, GlyphRect data)
    {
        baseField["m_X"].Write(data.m_X);
        baseField["m_Y"].Write(data.m_Y);
        baseField["m_Width"].Write(data.m_Width);
        baseField["m_Height"].Write(data.m_Height);
    }

    private static void WriteTo(AssetFieldWriter baseField, Character data)
    {
        baseField["m_ElementType"].Write(data.m_ElementType);
        baseField["m_Unicode"].Write(data.m_Unicode);
        baseField["m_GlyphIndex"].Write(data.m_GlyphIndex);
        baseField["m_Scale"].Write(data.m_Scale);
    }
}

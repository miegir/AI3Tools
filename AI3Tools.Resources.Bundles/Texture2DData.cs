using AssetsTools.NET.Texture;
using MessagePack;

namespace AI3Tools;

[MessagePackObject]
public record Texture2DData(
    [property: Key(0)] TextureFormat Format,
    [property: Key(1)] int Width,
    [property: Key(2)] int Height,
    [property: Key(3)] int MipCount,
    [property: Key(4)] byte[] EncodedData);

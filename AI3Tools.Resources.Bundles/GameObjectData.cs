using MessagePack;

namespace AI3Tools;

[MessagePackObject]
public record GameObjectData(
    [property: Key(0)] bool? Active);

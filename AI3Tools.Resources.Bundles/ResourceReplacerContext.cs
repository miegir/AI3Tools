using AssetsTools.NET;

namespace AI3Tools;

internal record ResourceReplacerContext(
    AssetTypeValueField BaseField, long Offset, long Size);

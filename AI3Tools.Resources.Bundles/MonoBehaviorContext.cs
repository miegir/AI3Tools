using AssetsTools.NET;

namespace AI3Tools;

internal record MonoBehaviorContext(
    AssetFileInfo Asset, ScriptName ScriptName, AssetTypeValueField BaseField);

using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Diagnostics;

namespace AI3Tools;

[DebuggerDisplay("{Name}")]
internal class GameObject
{
    public GameObject(AssetFileInfo asset, AssetTypeValueField field)
    {
        Asset = asset;
        Field = field;

        var nameField = field["m_Name"];
        if (!nameField.IsDummy)
        {
            Name = nameField.AsString;
        }
        else
        {
            Name = string.Empty;
        }
    }

    public AssetFileInfo Asset { get; }
    public AssetTypeValueField Field { get; }
    public string Name { get; set; }
    public GameObjectCollection Children { get; } = new();
    public GameObject? Parent { get; set; }
    public AssetClassID TypeId => (AssetClassID)Asset.TypeId;

    public string GetPath()
    {
        var segments = new Stack<string>();

        for (var obj = this; obj != null; obj = obj.Parent)
        {
            segments.Push(obj.Name);
        }

        return string.Join("/", segments);
    }
}

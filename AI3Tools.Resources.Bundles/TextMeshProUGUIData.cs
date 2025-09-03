using AssetsTools.NET;
using MessagePack;

namespace AI3Tools;

[MessagePackObject]
public class TextMeshProUGUIData(string text) : IWriteTo
{
    [Key(0)] public string m_text = text;

    public void WriteTo(AssetTypeValueField baseField)
    {
        baseField["m_text"].Write(m_text);
    }
}

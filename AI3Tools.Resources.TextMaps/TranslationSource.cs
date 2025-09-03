namespace AI3Tools;

internal class TranslationSource(string path) : IObjectSource<Dictionary<string, TextMapTranslation>>
{
    public Dictionary<string, TextMapTranslation> Deserialize()
    {
        using var stream = File.OpenRead(path);
        return TranslationSerializer.Deserialize(stream);
    }
}

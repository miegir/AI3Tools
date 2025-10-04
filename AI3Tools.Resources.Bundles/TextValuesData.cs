using System.Collections.Immutable;
using AssetsTools.NET;
using MessagePack;

namespace AI3Tools;

[MessagePackObject]
public class TextValuesData(IEnumerable<KeyValuePair<string, string>> values) : IWriteTo
{
    [Key(0)] public ImmutableDictionary<string, string> values = values
        .Where(e => !string.IsNullOrEmpty(e.Value))
        .ToImmutableDictionary(e => e.Key, e => TextCompressor.Compress(e.Value));

    void IWriteTo.WriteTo(AssetFieldWriter baseField)
    {
        foreach (var (key, value) in values)
        {
            baseField[key].Write(value);
        }
    }
}

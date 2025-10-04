using System.Text.Encodings.Web;
using System.Text.Json;

namespace AI3Tools;

internal class ScenesManager
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

    private readonly Dictionary<string, Dictionary<int, Dictionary<string, string>>> textMap = [];

    public bool IsEmpty => textMap.Count == 0;

    public void AddText(string path, int index, string fieldName, string text)
    {
        if (!textMap.TryGetValue(path, out var indexMap))
        {
            textMap.Add(path, indexMap = []);
        }

        if (!indexMap.TryGetValue(index, out var fields))
        {
            indexMap.Add(index, fields = []);
        }

        fields[fieldName] = text;
    }

    public void Export(Stream stream)
    {
        var items = textMap
            .Select(e => new Item { Path = e.Key, Text = e.Value })
            .ToArray();

        JsonSerializer.Serialize(stream, items, JsonOptions);
    }

    private class Item
    {
        public string? Path { get; set; }
        public Dictionary<int, Dictionary<string, string>>? Text { get; set; }
    }
}

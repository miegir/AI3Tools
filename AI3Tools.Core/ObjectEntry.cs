using System.IO.Compression;

namespace AI3Tools;

public class ObjectEntry(ZipArchiveEntry entry)
{
    public DateTime LastWriteTimeUtc => entry.LastWriteTime.UtcDateTime;

    public IObjectSource<T> AsObjectSource<T>() => new ObjectSource<T>(entry);

    public IStreamSource AsStreamSource() => new StreamSource(entry);

    private class ObjectSource<T>(ZipArchiveEntry entry) : IObjectSource<T>
    {
        public T Deserialize()
        {
            using var stream = entry.Open();
            return ObjectSerializer.Deserialize<T>(stream);
        }
    }

    private class StreamSource(ZipArchiveEntry entry) : IStreamSource
    {
        public Stream OpenRead() => entry.Open();
    }
}

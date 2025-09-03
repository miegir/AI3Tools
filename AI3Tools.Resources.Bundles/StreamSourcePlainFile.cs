namespace AI3Tools;

internal class StreamSourcePlainFile(string path) : ITrackableStreamSource
{
    private readonly FileInfo info = new(path);

    public bool Exists => info.Exists;

    public DateTime LastWriteTimeUtc => info.LastWriteTime;

    public Stream OpenRead() => info.OpenRead();

    public void Register(SourceChangeTracker tracker) => tracker.RegisterSource(info.FullName);
}

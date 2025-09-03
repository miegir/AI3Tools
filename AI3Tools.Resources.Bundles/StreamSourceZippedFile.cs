namespace AI3Tools;

internal class StreamSourceZippedFile(string path) : ITrackableStreamSource
{
    private readonly FileInfo info = new(path);

    public bool Exists => info.Exists;

    public DateTime LastWriteTimeUtc => info.LastWriteTime;

    public Stream OpenRead() => new ArchiveEntryStream(info.OpenRead());

    public void Register(SourceChangeTracker tracker) => tracker.RegisterSource(info.FullName);
}

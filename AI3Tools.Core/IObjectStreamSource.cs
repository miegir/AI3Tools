namespace AI3Tools;

public interface IObjectStreamSource : IStreamSource
{
    bool Exists { get; }
    DateTime LastWriteTimeUtc { get; }
}

namespace AI3Tools;

internal interface ITrackableStreamSource : IObjectStreamSource
{
    void Register(SourceChangeTracker tracker);
}

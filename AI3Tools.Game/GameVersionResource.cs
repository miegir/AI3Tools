using Microsoft.Extensions.Logging;

namespace AI3Tools;

internal class GameVersionResource(ILogger logger, Game game) : IResource
{
    public IEnumerable<Action> BeginExport(ExportArguments arguments) => [];

    public IEnumerable<Action> BeginImport(ImportArguments arguments) => [];

    public IEnumerable<Action> BeginMuster(MusterArguments arguments)
    {
        yield return () =>
        {
            logger.LogInformation("mustering game version...");

            var versionInfo = game.FindVersionInfo();
            if (versionInfo is null)
            {
                logger.LogError("game version info not found.");
                return;
            }

            arguments.Sink.ReportObject(
                GameVersionStatics.Path,
                new GameVersionStreamSource(versionInfo));
        };
    }

    public IEnumerable<Action> BeginUnpack(UnpackArguments arguments) => [];

    public IEnumerable<Action> BeginUnroll() => [];

    private class GameVersionStreamSource(GameVersionInfo info) : IObjectStreamSource
    {
        public bool Exists => true;
        public DateTime LastWriteTimeUtc => info.LastWriteTimeUtc;
        public Stream OpenRead() => ObjectSerializer.SerializeToStream(info.GameVersion);
    }
}

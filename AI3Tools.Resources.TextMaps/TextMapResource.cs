using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI3Tools;

public class TextMapResource : IResource
{
    private readonly ILogger logger;
    private readonly FileSource source;
    private readonly string languageName;
    private readonly string name;
    private readonly string fullName;

    public TextMapResource(ILogger logger, FileSource source, string languageName)
    {
        this.logger = logger;
        this.source = source;
        this.languageName = languageName;
        name = source.FileNameWithoutExtension;
        fullName = languageName + "/" + name;
    }

    public IEnumerable<Action> BeginExport(ExportArguments arguments)
    {
        var manager = new TextMapManager(logger, source);
        if (manager.IsEmpty)
        {
            yield break;
        }

        var path = Path.Combine(arguments.ExportDirectory, "Text", languageName, name + ".txt");
        if (!arguments.Force && File.Exists(path))
        {
            yield break;
        }

        yield return () =>
        {
            logger.LogInformation("Exporting text map {name}...", fullName);

            using var target = new FileTarget(path);
            manager.Export(target.Stream);
            target.Commit();
        };
    }

    public IEnumerable<Action> BeginImport(ImportArguments arguments)
    {
        var directoryPath = Path.Combine(arguments.SourceDirectory, "Text", languageName);
        if (!Directory.Exists(directoryPath))
        {
            return BeginUnroll();
        }

        var sourcePath = Path.Combine(directoryPath, name + ".txt");
        var sourceExists = File.Exists(sourcePath);
        if (!arguments.Debug && !sourceExists)
        {
            return BeginUnroll();
        }

        var manager = new TextMapManager(logger, source);
        if (manager.IsEmpty)
        {
            return BeginUnroll();
        }

        return Enumerate();

        IEnumerable<Action> Enumerate()
        {
            var statePath = Path.Combine(arguments.ObjectDirectory, "Text", languageName, name + ".import-state");
            var stateChangeTracker = new SourceChangeTracker(
                source.Destination, statePath, JsonSerializer.Serialize(arguments.Debug));

            if (!arguments.ForceTargets && !stateChangeTracker.HasChanges())
            {
                yield break;
            }

            yield return () =>
            {
                stateChangeTracker.RegisterSource(sourcePath);

                logger.LogInformation("importing text map {name}...", fullName);
                using (logger.BeginScope("text map {name}", fullName))
                {
                    var objectSource = sourceExists
                        ? new TranslationSource(sourcePath)
                        : ObjectSource.Create<Dictionary<string, TextMapTranslation>>();

                    manager.Import(objectSource, arguments.Debug ? name : null);
                }

                logger.LogInformation("saving text map {name}...", fullName);
                using var target = source.CreateTarget();
                manager.Save(target.Stream);
                target.Commit();

                if (!manager.HasWarnings)
                {
                    stateChangeTracker.Commit();
                }
            };
        }
    }

    public IEnumerable<Action> BeginMuster(MusterArguments arguments)
    {
        var directoryPath = Path.Combine(arguments.SourceDirectory, "Text", languageName);
        if (!Directory.Exists(directoryPath))
        {
            yield break;
        }

        var sourcePath = Path.Combine(directoryPath, name + ".txt");
        if (!File.Exists(sourcePath))
        {
            yield break;
        }

        yield return () =>
        {
            logger.LogInformation("mustering text map {name}...", fullName);

            var directory = ObjectPath.Root.Append("Text", languageName);
            arguments.Sink.ReportDirectory(directory);

            var objectPath = Path.Combine(arguments.ObjectDirectory, "Text", languageName, name + ".pak");
            var builder = new ObjectBuilder(sourcePath, objectPath, arguments.ForceObjects);

            TextMapManager.BuildObject(builder);

            var musterPath = directory.Append(name + ".pak");
            arguments.Sink.ReportObject(musterPath, objectPath);
        };
    }

    public IEnumerable<Action> BeginUnpack(UnpackArguments arguments)
    {
        var directory = ObjectPath.Root.Append("Text", languageName);
        if (!arguments.Container.HasDirectory(directory)) return BeginUnroll();
        var path = directory.Append(name + ".pak");
        if (!arguments.Container.TryGetEntry(path, out var entry) && !arguments.Debug) return BeginUnroll();
        if (!arguments.Debug && entry is null) return BeginUnroll();
        var manager = new TextMapManager(logger, source);
        if (manager.IsEmpty) return BeginUnroll();
        return Enumerate();

        IEnumerable<Action> Enumerate()
        {
            yield return () =>
            {
                logger.LogInformation("unpacking text map {name}...", fullName);
                using (logger.BeginScope("text map {name}", fullName))
                {
                    var objectSource = entry is not null
                        ? entry.AsObjectSource<Dictionary<string, TextMapTranslation>>()
                        : ObjectSource.Create<Dictionary<string, TextMapTranslation>>();
                    manager.Import(objectSource, arguments.Debug ? name : null);
                }

                logger.LogInformation("saving text map {name}...", fullName);
                using var target = source.CreateTarget();
                manager.Save(target.Stream);
                target.Commit();
            };
        }
    }

    public IEnumerable<Action> BeginUnroll()
    {
        if (source.CanUnroll())
        {
            yield return () =>
            {
                logger.LogInformation("unrolling text map {name}...", fullName);
                source.Unroll();
            };
        }
    }
}

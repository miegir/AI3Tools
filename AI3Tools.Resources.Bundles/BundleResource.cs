using Microsoft.Extensions.Logging;

namespace AI3Tools;

public class BundleResource(ILogger logger, FileSource source) : IResource
{
    public static readonly string[] SearchPatterns = ["*.bundle"];

    private readonly string name = source.FileNameWithoutExtension;

    public IEnumerable<Action> BeginExport(ExportArguments arguments)
    {
        var path = Path.Combine(arguments.ExportDirectory, "aa", name);

        yield return () =>
        {
            logger.LogInformation("exporting bundle {name}...", name);
            using (logger.BeginScope("bundle {name}", name))
            {
                var manager = new BundleManager(logger, source);
                var scenesPath = Path.Combine(arguments.ExportDirectory, "scenes", name + ".txt");
                manager.Export(arguments with { ExportDirectory = path }, scenesPath);
            }
        };
    }

    public IEnumerable<Action> BeginImport(ImportArguments arguments)
    {
        var sourceDirectory = Path.Combine(arguments.SourceDirectory, "aa", name);

        var bundleFileSource = new BundleFileSource(
            logger, Path.Combine(arguments.SourceDirectory, "bundles", name));

        var gameObjectSource = new GameObjectSource(
            Path.Combine(arguments.SourceDirectory, "scenes", name + ".txt"));

        if (!Directory.Exists(sourceDirectory)
            && !bundleFileSource.Exists
            && !gameObjectSource.Exists)
        {
            if (source.CanUnroll())
            {
                foreach (var action in BeginUnroll())
                {
                    yield return action;
                }
            }

            yield break;
        }

        yield return () =>
        {
            var objectDirectory = Path.Combine(arguments.ObjectDirectory, "aa", name);
            using (logger.BeginScope("bundle {name}", name))
            {
                var statePath = objectDirectory + ".import-state";
                var sourceChangeTracker = new SourceChangeTracker(source.Destination, statePath);
                var bundleResolverFactory = CreateBundleResolverFactory(arguments.ObjectDirectory);

                var manager = new BundleManager(logger, source);

                var shouldCommit = manager.Import(
                    arguments with
                    {
                        SourceDirectory = sourceDirectory,
                        ObjectDirectory = objectDirectory,
                    },
                    bundleResolverFactory,
                    bundleFileSource,
                    gameObjectSource,
                    sourceChangeTracker);

                if (shouldCommit)
                {
                    sourceChangeTracker.Commit();
                }
            }
        };
    }

    public IEnumerable<Action> BeginMuster(MusterArguments arguments)
    {
        var sourceDirectory = Path.Combine(arguments.SourceDirectory, "aa", name);

        var bundleFileSource = new BundleFileSource(
            logger, Path.Combine(arguments.SourceDirectory, "bundles", name));

        var gameObjectSource = new GameObjectSource(
            Path.Combine(arguments.SourceDirectory, "scenes", name + ".txt"));

        if (!Directory.Exists(sourceDirectory)
            && !bundleFileSource.Exists
            && !gameObjectSource.Exists)
        {
            yield break;
        }

        yield return () =>
        {
            var objectDirectory = Path.Combine(arguments.ObjectDirectory, "aa", name);
            logger.LogInformation("mustering bundle {name}...", name);
            using (logger.BeginScope("bundle {name}", name))
            {
                var directoryName = ObjectPath.Root.Append("aa", name);
                var manager = new BundleManager(logger, source);
                var bundleResolverFactory = CreateBundleResolverFactory(arguments.ObjectDirectory);

                if (manager.Muster(
                    directoryName,
                    arguments with
                    {
                        SourceDirectory = sourceDirectory,
                        ObjectDirectory = objectDirectory,
                    },
                    bundleResolverFactory,
                    bundleFileSource,
                    gameObjectSource))
                {
                    arguments.Sink.ReportDirectory(directoryName);
                }
            }
        };
    }

    public IEnumerable<Action> BeginUnpack(UnpackArguments arguments)
    {
        var directory = ObjectPath.Root.Append("aa", name);
        if (!arguments.Container.HasDirectory(directory)) return BeginUnroll();
        return Enumerate();
        IEnumerable<Action> Enumerate()
        {
            yield return () =>
            {
                logger.LogInformation("unpacking bundle {name}...", name);
                using (logger.BeginScope("bundle {name}", name))
                {
                    var manager = new BundleManager(logger, source);
                    manager.Unpack(arguments, directory);
                }
            };
        }
    }

    public IEnumerable<Action> BeginUnroll()
    {
        if (source.CanUnroll())
        {
            yield return () =>
            {
                logger.LogInformation("unrolling bundle {name}...", name);
                source.Unroll();
            };
        }
    }

    private BundleResolverFactory CreateBundleResolverFactory(string objectDirectory)
    {
        var bundleResolverStatePath = Path.Combine(objectDirectory, "aa", "bundle-resolver");
        return new BundleResolverFactory(logger, bundleResolverStatePath);
    }
}

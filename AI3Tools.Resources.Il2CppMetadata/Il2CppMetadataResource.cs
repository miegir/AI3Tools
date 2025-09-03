using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AI3Tools;

public class Il2CppMetadataResource(ILogger logger, FileSource source) : IResource
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

    private readonly string name = source.FileNameWithoutExtension;

    public IEnumerable<Action> BeginExport(ExportArguments arguments)
    {
        var path = Path.Combine(arguments.ExportDirectory, "metadata", name + ".txt");
        if (File.Exists(path))
        {
            yield break;
        }

        var manager = new Il2CppMetadataManager(logger, source);
        yield return () =>
        {
            logger.LogInformation("exporting il2cpp metadata file {name}...", name);

            var translations = manager.StringLiterals
                .ToDictionary(src => src, src => default(string));

            using var target = new FileTarget(path);
            JsonSerializer.Serialize(target.Stream, translations, JsonOptions);
            target.Commit();
        };
    }

    public IEnumerable<Action> BeginImport(ImportArguments arguments)
    {
        var sourcePath = Path.Combine(arguments.SourceDirectory, "metadata", name + ".txt");
        if (!File.Exists(sourcePath)) return BeginUnroll();
        return Enumerate();

        IEnumerable<Action> Enumerate()
        {
            var objectPath = Path.Combine(arguments.ObjectDirectory, "metadata", name + ".pak");
            var sourceChangeTracker = new SourceChangeTracker(source.Destination, objectPath + ".state");

            sourceChangeTracker.RegisterSource(sourcePath);

            if (!sourceChangeTracker.HasChanges())
            {
                yield break;
            }

            yield return () =>
            {
                var manager = new Il2CppMetadataManager(logger, source);

                using var stream = File.OpenRead(sourcePath);

                var translations = JsonSerializer.Deserialize<Dictionary<string, string?>>(stream, JsonOptions);
                var hasWarnings = Translate(manager.StringLiterals, translations);

                Save(manager);

                if (!hasWarnings)
                {
                    sourceChangeTracker.Commit();
                }
            };
        }
    }

    public IEnumerable<Action> BeginMuster(MusterArguments arguments)
    {
        var sourcePath = Path.Combine(arguments.SourceDirectory, "metadata", name + ".txt");
        if (!File.Exists(sourcePath)) yield break;

        yield return () =>
        {
            logger.LogInformation("mustering il2cpp metadata file {name}...", name);

            var objectPath = Path.Combine(arguments.ObjectDirectory, "metadata", name + ".pak");
            var builder = new ObjectBuilder(sourcePath, objectPath, arguments.ForceObjects);

            builder.Build(stream =>
            {
                logger.LogInformation("building il2cpp metadata file {name}...", name);

                var translations =
                    JsonSerializer.Deserialize<Dictionary<string, string?>>(stream, JsonOptions)
                        ?? [];

                // compress
                var untranslated = new HashSet<string>();
                foreach (var entry in translations)
                {
                    if (entry.Value == null || entry.Value == entry.Key)
                    {
                        untranslated.Add(entry.Key);
                    }
                }

                foreach (var item in untranslated)
                {
                    translations.Remove(item);
                }

                return translations;
            });

            var musterPath = ObjectPath.Root.Append("metadata", name + ".pak");
            arguments.Sink.ReportObject(musterPath, objectPath);
        };
    }

    public IEnumerable<Action> BeginUnpack(UnpackArguments arguments)
    {
        var path = ObjectPath.Root.Append("metadata", name + ".pak");
        if (!arguments.Container.TryGetEntry(path, out var entry)) return BeginUnroll();
        return Enumerate();

        IEnumerable<Action> Enumerate()
        {
            yield return () =>
            {
                logger.LogInformation("unpacking il2cpp metadata {name}...", name);

                var manager = new Il2CppMetadataManager(logger, source);
                var translations = entry.AsObjectSource<Dictionary<string, string?>>().Deserialize();

                Translate(manager.StringLiterals, translations);
                Save(manager);
            };
        }
    }

    public IEnumerable<Action> BeginUnroll()
    {
        if (source.CanUnroll())
        {
            yield return () =>
            {
                logger.LogInformation("unrolling il2cpp metadata {name}...", name);
                source.Unroll();
            };
        }
    }

    private bool Translate(string[] strings, Dictionary<string, string?>? translations)
    {
        logger.LogInformation("importing il2cpp metadata file {name}...", name);
        using (logger.BeginScope("importing metadata file {name}", name))
        {
            var hasWarnings = false;

            if (translations == null || translations.Count == 0)
            {
                return hasWarnings;
            }

            var unapplied = translations.Keys.ToHashSet();
            for (var i = 0; i < strings.Length; i++)
            {
                var s = strings[i];
                if (translations.TryGetValue(s, out var translated) && translated != null)
                {
                    strings[i] = translated;
                    unapplied.Remove(s);
                }
            }

            foreach (var s in unapplied)
            {
                hasWarnings = true;
                logger.LogWarning("Unapplied translation: {expected}.", s);
            }

            return hasWarnings;
        }
    }

    private void Save(Il2CppMetadataManager manager)
    {
        logger.LogInformation("saving il2cpp metadata {name}...", name);
        using var target = source.CreateTarget();
        manager.Save(target.Stream);
        target.Commit();
    }
}

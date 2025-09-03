using AI3Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;

var gamePathOption =
    new Option<FileInfo>("--game-path", "-g")
    {
        Description = "Game executable path",
        Required = true,
    }
    .AcceptExistingOnly();

var exportDirectoryOption =
    new Option<DirectoryInfo>("--export-directory", "-e")
    {
        Description = "Directory to export assets into",
        Required = true,
    }
    .AcceptLegalFilePathsOnly();

var sourceDirectoryOption =
    new Option<DirectoryInfo>("--source-directory", "-s")
    {
        Description = "Source files location",
        Required = true,
    }
    .AcceptExistingOnly();

var objectDirectoryOption =
    new Option<DirectoryInfo>("--object-directory", "-j")
    {
        Description = "Object files location",
        Required = true,
    }
    .AcceptLegalFilePathsOnly();

var bc7CompressionOption =
    new Option<BC7CompressionType>("--bc7-compression", "-7")
    {
        Description = "BC7 texture compression type"
    };

var bundleCompressionOption =
    new Option<BundleCompressionType>("--bundle-compression", "-c")
    {
        Description = "Bundle compression type"
    };

var archivePathCreateOption =
    new Option<FileInfo>("--archive-path", "-a")
    {
        Description = "Archive file path",
        Required = true,
    }
    .AcceptLegalFilePathsOnly();

var archivePathOpenOption =
    new Option<FileInfo>("--archive-path", "-a")
    {
        Description = "Archive file path",
        Required = true,
    }
    .AcceptExistingOnly();

var forceOption =
    new Option<bool>("--force", "-f")
    {
        Description = "Enables all force options",
    };

var forceExportOption =
    new Option<bool>("--force-export")
    {
        Description = "Overwrites asset files",
    };

var forceObjectsOption =
    new Option<bool>("--force-objects")
    {
        Description = "Always overwrites object files",
    };

var forceTargetsOption =
    new Option<bool>("--force-targets")
    {
        Description = "Always overwrites target files",
    };

var forcePackOption =
    new Option<bool>("--force-pack")
    {
        Description = "Always overwrites archive file",
    };

var debugOption =
    new Option<bool>("--debug", "-d")
    {
        Description = "Output debug information",
    };

var launchOption =
    new Option<bool>("--launch", "-l")
    {
        Description = "Launches game on completion",
    };

var exportCommand = new Command(
    name: "export")
{
    gamePathOption,
    exportDirectoryOption,
    forceOption,
    forceExportOption,
};

SetInvokeAction(exportCommand, context =>
{
    var exportDirectory = context.ParseResult.GetRequiredValue(exportDirectoryOption);
    var force = context.ParseResult.GetValue(forceOption);
    var forceExport = force || context.ParseResult.GetValue(forceExportOption);

    context.Pipeline.Export(
        arguments: new ExportArguments(
            ExportDirectory: exportDirectory.FullName,
            Force: forceExport));
});

var importCommand = new Command(
    name: "import")
{
    gamePathOption,
    sourceDirectoryOption,
    objectDirectoryOption,
    bc7CompressionOption,
    bundleCompressionOption,
    forceOption,
    forceObjectsOption,
    forceTargetsOption,
    debugOption,
    launchOption,
};

SetInvokeAction(importCommand, context =>
{
    var sourceDirectory = context.ParseResult.GetRequiredValue(sourceDirectoryOption);
    var objectDirectory = context.ParseResult.GetRequiredValue(objectDirectoryOption);
    var force = context.ParseResult.GetValue(forceOption);
    var forceObjects = force || context.ParseResult.GetValue(forceObjectsOption);
    var forceTargets = force || context.ParseResult.GetValue(forceTargetsOption);
    var bc7Compression = context.ParseResult.GetValue(bc7CompressionOption);
    var bundleCompression = context.ParseResult.GetValue(bundleCompressionOption);

    context.Pipeline.Import(new ImportArguments(
        SourceDirectory: sourceDirectory.FullName,
        ObjectDirectory: objectDirectory.FullName,
        ForceObjects: forceObjects,
        ForceTargets: forceTargets,
        BC7Compression: bc7Compression,
        BundleCompression: bundleCompression));
});

var createCommand = new Command(
    name: "create")
{
    gamePathOption,
    sourceDirectoryOption,
    objectDirectoryOption,
    bc7CompressionOption,
    archivePathCreateOption,
    forceOption,
    forceObjectsOption,
    forcePackOption,
};

SetInvokeAction(createCommand, context =>
{
    var sourceDirectory = context.ParseResult.GetRequiredValue(sourceDirectoryOption);
    var objectDirectory = context.ParseResult.GetRequiredValue(objectDirectoryOption);
    var force = context.ParseResult.GetValue(forceOption);
    var forceObjects = force || context.ParseResult.GetValue(forceObjectsOption);
    var forcePack = force || context.ParseResult.GetValue(forcePackOption);
    var bc7Compression = context.ParseResult.GetValue(bc7CompressionOption);
    var archivePath = context.ParseResult.GetRequiredValue(archivePathCreateOption);

    var sink = context.ServiceProvider.GetRequiredService<MusterSink>();

    context.Pipeline.Muster(new MusterArguments(
        Sink: sink,
        SourceDirectory: sourceDirectory.FullName,
        ObjectDirectory: objectDirectory.FullName,
        ForceObjects: forceObjects,
        BC7Compression: bc7Compression));

    sink.Pack(new PackArguments(
        ArchivePath: archivePath.FullName,
        Force: forcePack));
});

var unpackCommand = new Command(
    name: "unpack")
{
    gamePathOption,
    archivePathOpenOption,
    bundleCompressionOption,
    debugOption,
    launchOption,
};

SetInvokeAction(unpackCommand, context =>
{
    var archivePath = context.ParseResult.GetRequiredValue(archivePathOpenOption);
    var bundleCompression = context.ParseResult.GetValue(bundleCompressionOption);
    var debug = context.ParseResult.GetValue(debugOption);

    using var stream = archivePath.OpenRead();
    using var container = new ObjectContainer(stream);

    context.Pipeline.Unpack(new UnpackArguments(
        Container: container,
        BundleCompression: bundleCompression,
        Debug: debug));
});

var unrollCommand = new Command(
    name: "unroll")
{
    gamePathOption,
    launchOption,
};

SetInvokeAction(unrollCommand, context =>
{
    context.Pipeline.Unroll();
});

var rootCommand = new RootCommand
{
    exportCommand,
    importCommand,
    createCommand,
    unpackCommand,
    unrollCommand,
};

var parseResult = rootCommand.Parse(args);

return parseResult.Invoke();

void SetInvokeAction(Command command, Action<InvokeContext> action)
{
    command.SetAction(parseResult =>
    {
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<MusterSink>()
            .AddLogging(builder =>
            {
                builder.AddFilter(nameof(AI3Tools), LogLevel.Debug);
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                });
            })
            .BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Game>>();

        var game = new Game(logger, parseResult.GetRequiredValue(gamePathOption).FullName);
        var pipeline = game.CreatePipeline();

        action(new InvokeContext(
            ServiceProvider: serviceProvider,
            ParseResult: parseResult,
            Pipeline: pipeline));

        if (parseResult.GetValue(launchOption))
        {
            game.Launch();
        }
    });
}

record InvokeContext(
    IServiceProvider ServiceProvider,
    ParseResult ParseResult,
    GamePipeline Pipeline);

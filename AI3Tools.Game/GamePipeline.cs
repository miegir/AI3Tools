using Microsoft.Extensions.Logging;

namespace AI3Tools;

public class GamePipeline(ILogger logger, IEnumerable<IResource> resources)
{
    public void Export(ExportArguments arguments) => resources
        .SelectMany(r => r.BeginExport(arguments))
        .Scoped(logger, "resource")
        .Run();

    public void Import(ImportArguments arguments) => resources
        .SelectMany(r => r.BeginImport(arguments))
        .Scoped(logger, "resource")
        .Run();

    public void Muster(MusterArguments arguments) => resources
        .SelectMany(r => r.BeginMuster(arguments))
        .Scoped(logger, "resource")
        .Run();

    public void Unpack(UnpackArguments arguments) => resources
        .SelectMany(r => r.BeginUnpack(arguments))
        .Scoped(logger, "resource")
        .Run();

    public void Unroll() => resources
        .SelectMany(r => r.BeginUnroll())
        .Scoped(logger, "resource")
        .Run();
}

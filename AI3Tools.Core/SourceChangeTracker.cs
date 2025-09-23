using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;

namespace AI3Tools;

public class SourceChangeTracker
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly Dictionary<string, FileState?> updatedStates = [];
    private readonly Dictionary<string, FileState?> originalStates;
    private readonly FileDestination destination;
    private readonly string statePath;
    private readonly string? callerState;
    private readonly ImmutableSortedDictionary<string, Guid> mvids;
    private readonly bool accepted;

    public SourceChangeTracker(FileDestination destination, string statePath, string? callerState = null)
    {
        this.destination = destination;
        this.statePath = statePath;
        this.callerState = callerState;
        mvids = GetMvids(Assembly.GetCallingAssembly());
        var destinationState = destination.FileState;
        var stateInfo = new FileInfo(statePath);

        if (stateInfo.Exists)
        {
            using var stream = stateInfo.OpenRead();
            try
            {
                var state = JsonSerializer.Deserialize<State>(stream, JsonOptions);

                accepted = state != null
                    && (state.Mvids?.SequenceEqual(mvids) ?? false)
                    && state.CallerState == callerState
                    && state.DestinationState == destinationState;

                originalStates = state?.SourceStates ?? [];

                return;
            }
            catch (JsonException)
            {
            }
        }

        originalStates = [];
    }

    public bool HasChanges()
    {
        if (!accepted)
        {
            return true;
        }

        foreach (var (sourcePath, originalState) in originalStates)
        {
            if (GetCurrentState(sourcePath) != originalState)
            {
                return true;
            }
        }

        return false;
    }

    public void RegisterSource(string sourcePath)
    {
        updatedStates[sourcePath] = GetCurrentState(sourcePath);
    }

    public void Commit()
    {
        using var target = new FileTarget(statePath);

        JsonSerializer.Serialize(
            target.Stream, new State
            {
                Mvids = mvids,
                DestinationState = destination.FileState,
                CallerState = callerState,
                SourceStates = updatedStates,
            },
            JsonOptions);

        target.Commit();
    }

    private static FileState? GetCurrentState(string path) => File.Exists(path) ? FileState.FromPath(path) : null;

    private static ImmutableSortedDictionary<string, Guid> GetMvids(Assembly assembly)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<string, Guid>();

        AppendMvid(assembly);

        foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
        {
            try
            {
                var referencedAssembly = Assembly.Load(referencedAssemblyName);
                AppendMvid(referencedAssembly);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        return builder.ToImmutable();

        void AppendMvid(Assembly assembly)
        {
            builder[assembly.ManifestModule.Name] = assembly.ManifestModule.GetModuleVersionId();
        }
    }

    private class State
    {
        public ImmutableSortedDictionary<string, Guid>? Mvids { get; set; }
        public FileState? DestinationState { get; set; }
        public string? CallerState { get; set; }
        public Dictionary<string, FileState?>? SourceStates { get; set; }
    }
}

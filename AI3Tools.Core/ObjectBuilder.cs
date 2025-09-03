using MessagePack;
using System.Reflection;

namespace AI3Tools;

public class ObjectBuilder(FileDestination source, string objectPath, bool forceObjects)
{
    private readonly Guid mvid = Assembly.GetCallingAssembly().ManifestModule.ModuleVersionId;

    public T Build<T>(Func<Stream, T> factory)
    {
        var statePath = objectPath + ".state";
        var sourceState = source.FileState;

        if (!forceObjects && !IsChanged())
        {
            try
            {
                using var existingStream = File.OpenRead(objectPath);
                return ObjectSerializer.Deserialize<T>(existingStream);
            }
            catch (MessagePackSerializationException)
            {
            }
        }

        using var stream = source.OpenRead();
        var result = factory(stream);
        using var target = new FileTarget(objectPath);
        ObjectSerializer.Serialize(target.Stream, result);
        target.Commit();
        return result;

        bool IsChanged()
        {
            if (File.Exists(statePath))
            {
                using var stream = File.OpenRead(statePath);
                using var reader = new BinaryReader(stream);

                if (new Guid(reader.ReadBytes(16)) == mvid &&
                    reader.ReadInt64() == sourceState.Length &&
                    reader.ReadInt64() == sourceState.LastWriteTimeUtc.Ticks)
                {
                    var objectInfo = new FileInfo(objectPath);
                    return !objectInfo.Exists || objectInfo.LastWriteTimeUtc < sourceState.LastWriteTimeUtc;
                }
            }

            using var target = new FileTarget(statePath);
            using var writer = new BinaryWriter(target.Stream);

            writer.Write(mvid.ToByteArray());
            writer.Write(sourceState.Length);
            writer.Write(sourceState.LastWriteTimeUtc.Ticks);

            target.Commit();

            return true;
        }
    }
}

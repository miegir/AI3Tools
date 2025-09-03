namespace AI3Tools;

internal class PhysicalObjectSource<T>(string path) : IObjectSource<T>
{
    public T Deserialize()
    {
        using var stream = File.OpenRead(path);
        return ObjectSerializer.Deserialize<T>(stream);
    }
}

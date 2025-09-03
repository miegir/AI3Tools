namespace AI3Tools;

public static class ObjectSource
{
    public static IObjectSource<T> Create<T>() where T : new() => Create(() => new T());
    public static IObjectSource<T> Create<T>(T value) => new ConstObjectSource<T>(value);
    public static IObjectSource<T> Create<T>(Func<T> factory) => new DelegateObjectSource<T>(factory);

    private class ConstObjectSource<T>(T value) : IObjectSource<T>
    {
        public T Deserialize() => value;
    }

    private class DelegateObjectSource<T>(Func<T> factory) : IObjectSource<T>
    {
        public T Deserialize() => factory();
    }
}

namespace AI3Tools;

public record UnpackArguments(
    ObjectContainer Container,
    BundleCompressionType BundleCompression = 0,
    bool Debug = false);

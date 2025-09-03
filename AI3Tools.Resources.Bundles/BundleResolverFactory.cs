using AssetsTools.NET.Extra;
using Microsoft.Extensions.Logging;

namespace AI3Tools;

internal class BundleResolverFactory(ILogger logger, string objectPath)
{
    public BundleResolver CreateBundleResolver(BundleFileInstance bundleFileInstance)
    {
        var directory = Path.GetDirectoryName(bundleFileInstance.path) ?? string.Empty;
        return new BundleResolver(logger, directory, objectPath);
    }
}

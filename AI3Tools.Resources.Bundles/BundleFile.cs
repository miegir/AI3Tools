using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using Microsoft.Extensions.Logging;

namespace AI3Tools;

internal class BundleFile : IDisposable
{
    private readonly Dictionary<long, string> paths = [];
    private readonly Dictionary<long, GameObject> gameObjectMap = [];
    private readonly Dictionary<string, BundleResourceBlob> resourceBlobs = [];
    private readonly Dictionary<AssetFileInfo, AssetsFileInstance> assetFileInstanceByAssetLookup = [];
    private readonly Dictionary<AssetTypeValueField, AssetsFileInstance> assetFileInstanceByFieldLookup = [];
    private readonly GameObjectCollection gameObjects = new();
    private readonly FileTargetCollector fileTargetCollector = new();
    private readonly List<FileSource> unrollSources = [];
    private readonly ILogger logger;
    private readonly AssetsManager assetsManager;
    private readonly BundleFileInstance bundleFileInstance;
    private readonly Dictionary<int, AssetsFileInstance> assetsFileInstances;
    private readonly ILookup<string, long> pathLookup;
    private AssetBundleFile? unpackedBundleFile;

    public BundleFile(ILogger logger, FileStream stream)
    {
        this.logger = logger;
        assetsManager = new AssetsManager();
        using var classPackageStream = new MemoryStream(Properties.Resources.ClassData);
        assetsManager.LoadClassPackage(classPackageStream);
        bundleFileInstance = assetsManager.LoadBundleFile(stream);
        assetsFileInstances = LoadAssetsFileInstances();
        LoadPaths();
        LoadGameObjects();
        pathLookup = paths.ToLookup(e => AssetNameHelper.Unpack(e.Value), e => e.Key, StringComparer.OrdinalIgnoreCase);
    }

    public GameObjectCollection GameObjects => gameObjects;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            assetsManager.UnloadAll();
        }

        foreach (var source in unrollSources)
        {
            logger.LogInformation("unrolling bundle...");

            source.Unroll();
        }

        fileTargetCollector.Commit();
        unrollSources.Clear();
    }

    public BundleResolver CreateBundleResolver(BundleResolverFactory factory)
    {
        return factory.CreateBundleResolver(bundleFileInstance);
    }

    public IEnumerable<AssetFileInfo> GetAssets(AssetClassID typeId)
    {
        foreach (var assetsFileInstance in assetsFileInstances.Values)
        {
            foreach (var asset in assetsFileInstance.file.GetAssetsOfType(typeId))
            {
                assetFileInstanceByAssetLookup[asset] = assetsFileInstance;
                yield return asset;
            }
        }
    }

    public AssetTypeValueField GetBaseField(AssetFileInfo asset)
    {
        var assetsFileInstance = LookupAssetsFileInstance(asset);
        var field = assetsManager.GetBaseField(assetsFileInstance, asset);
        assetFileInstanceByFieldLookup[field] = assetsFileInstance;
        return field;
    }

    public string ReadAssetName(AssetFileInfo asset, string defaultExtension)
    {
        var assetsFileInstance = LookupAssetsFileInstance(asset);

        var name = AssetHelper.GetAssetNameFast(
            assetsFileInstance.file, assetsManager.ClassDatabase, asset);

        paths.TryGetValue(asset.PathId, out var pathName);

        return AssetNameHelper.Pack(name, defaultExtension, pathName);
    }

    public IEnumerable<GameObject> GetComponents(GameObject parent)
    {
        foreach (var data in parent.Field["m_Component.Array"].Children)
        {
            var component = ResolveGameObject(parent.Asset, data[0]);
            if (component != null)
            {
                yield return component;
            }
        }
    }

    public int GetMonoBehaviorIndex(GameObject parent, AssetFileInfo asset)
    {
        var index = 0;

        foreach (var component in GetComponents(parent))
        {
            if (component.TypeId != AssetClassID.MonoBehaviour)
            {
                continue;
            }

            if (component.Asset == asset)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public GameObject GetMonoBehaviorAtIndex(GameObject parent, int index)
    {
        return GetComponents(parent)
            .Where(c => c.TypeId == AssetClassID.MonoBehaviour)
            .Skip(index)
            .First();
    }

    public GameObject? ResolveGameObject(AssetFileInfo relativeTo, AssetTypeValueField pointer)
    {
        if (pointer.IsDummy) return default;
        if (pointer["m_FileID"].AsInt != 0) return default;
        var pathId = pointer["m_PathID"].AsLong;
        if (gameObjectMap.TryGetValue(pathId, out var gameObject)) return gameObject;
        var asset = assetsManager.GetExtAsset(LookupAssetsFileInstance(relativeTo), 0, pathId);
        if (asset.file == null || asset.baseField == null) return default;
        assetFileInstanceByAssetLookup[asset.info] = asset.file;
        assetFileInstanceByFieldLookup[asset.baseField] = asset.file;
        gameObject = new GameObject(asset.info, asset.baseField);
        gameObjectMap.Add(pathId, gameObject);
        return gameObject;
    }

    public GameObject? ResolveGameObject(AssetFileInfo asset)
    {
        if (asset.TypeId != (int)AssetClassID.GameObject) return null;
        var pathId = asset.PathId;
        if (gameObjectMap.TryGetValue(pathId, out var gameObject)) return gameObject;
        var baseField = GetBaseField(asset);
        gameObject = new GameObject(asset, baseField);
        gameObjectMap.Add(pathId, gameObject);
        return gameObject;
    }

    public byte[] GetTextureData(TextureFile textureFile)
    {
        // AssetsTools.NET.Texture implementation of
        // textureFile.GetTextureData(string rootPath, AssetBundleFile bundle)
        // is buggy. We therefore use its fixed copy for now.
        var bundle = GetUnpackedBundle();
        var m_StreamData = textureFile.m_StreamData;

        // TODO: unify with GetResourceData
        if (textureFile.pictureData.Length == 0
            && m_StreamData.path.StartsWith("archive:/"))
        {
            var name = m_StreamData.path.Split('/').Last();
            var fileIndex = bundle.GetFileIndex(name);
            if (fileIndex >= 0)
            {
                bundle.GetFileRange(fileIndex, out var offset, out _);
                var pictureData = new byte[m_StreamData.size];
                bundle.DataReader.Position = offset + (long)m_StreamData.offset;
                _ = bundle.DataReader.Read(pictureData, 0, pictureData.Length);
                return TextureFile.DecodeManaged(
                    data: pictureData,
                    format: (TextureFormat)textureFile.m_TextureFormat,
                    width: textureFile.m_Width,
                    height: textureFile.m_Height);
            }
        }

        return textureFile.GetTextureData(rootPath: null);
    }

    public IEnumerable<AssetFileInfo> FindAssets(AssetClassID typeId, string name, string defaultExtension)
    {
        name = AssetNameHelper.Unpack(name, out _);

        foreach (var assetsFileInstance in assetsFileInstances.Values)
        {
            foreach (var pathId in pathLookup[name])
            {
                var asset = assetsFileInstance.file.GetAssetInfo(pathId);
                if (asset is null)
                {
                    continue;
                }

                if (asset.TypeId != (int)typeId)
                {
                    continue;
                }

                assetFileInstanceByAssetLookup[asset] = assetsFileInstance;
                var assetName = ReadAssetName(asset, defaultExtension);
                assetName = AssetNameHelper.Unpack(assetName, out _);
                if (!string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return asset;
            }
        }
    }

    public ScriptName? ReadScriptName(BundleResolver resolver, AssetTypeValueField baseField)
    {
        var scriptField = baseField["m_Script"];
        if (scriptField.IsDummy)
        {
            return null;
        }

        var assetsFileInstance = LookupAssetsFileInstance(baseField);

        var fileId = scriptField["m_FileID"].AsInt;
        var pathId = scriptField["m_PathID"].AsLong;

        if (fileId == 0)
        {
            return ReadScriptName(assetsFileInstance);
        }
        else
        {
            fileId--;

            var dependency = assetsFileInstance.GetDependency(assetsManager, fileId);
            if (dependency != null)
            {
                return ReadScriptName(dependency);
            }

            var path = assetsFileInstance.file.Metadata.Externals[fileId].PathName;

            var dependentFileInstance = Resolve(path);
            if (dependentFileInstance == null)
            {
                return null;
            }

            return ReadScriptName(dependentFileInstance);
        }

        ScriptName? ReadScriptName(AssetsFileInstance assetsFileInstance)
        {
            var asset = assetsFileInstance.file.GetAssetInfo(pathId);
            if (asset == null) return null;
            var baseField = assetsManager.GetBaseField(assetsFileInstance, asset);
            return ScriptName.Read(baseField);
        }

        AssetsFileInstance? Resolve(string path)
        {
            using var bundleStream = resolver.OpenBundle(path);
            if (bundleStream == null) return null;
            var bundleFileInstance = assetsManager.LoadBundleFile(bundleStream);
            return assetsManager.LoadAssetsFileFromBundle(bundleFileInstance, 0);
        }
    }

    public byte[]? ReadResource(string? name, long offset, long length)
    {
        if (string.IsNullOrEmpty(name) || length == 0)
        {
            return null;
        }

        if (name.StartsWith("archive:/"))
        {
            var index = name.LastIndexOf('/');
            name = name[(index + 1)..];
        }

        var data = BundleHelper.LoadAssetDataFromBundle(bundleFileInstance.file, name);
        if (data == null)
        {
            return null;
        }

        if (offset == 0 && length == data.Length)
        {
            return data;
        }

        using var stream = new MemoryStream(data);
        stream.Position = offset;
        var buffer = new byte[length];

        var read = stream.Read(buffer);
        if (read == buffer.Length)
        {
            return buffer;
        }

        return buffer[0..read];
    }

    public void Replace(AssetFileInfo asset, IObjectSource<Texture2DData> source)
    {
        var baseField = GetBaseField(asset);
        var textureData = source.Deserialize();
        var textureFile = TextureFile.ReadTextureFile(baseField);
        textureFile.SetTextureDataRaw(textureData.EncodedData, textureData.Width, textureData.Height);
        textureFile.m_TextureFormat = (int)textureData.Format;
        textureFile.m_MipCount = textureData.MipCount;
        textureFile.WriteTo(baseField);
        asset.SetNewData(baseField);
    }

    public void Replace(AssetFileInfo asset, IObjectSource<string> source)
    {
        var baseField = GetBaseField(asset);
        var script = source.Deserialize();
        baseField["m_Script"].AsString = TextCompressor.Compress(script);
        asset.SetNewData(baseField);
    }

    public void Replace(AssetFileInfo asset, IObjectSource<GameObjectData> source)
    {
        var baseField = GetBaseField(asset);
        var data = source.Deserialize();
        if (data.Active is bool active)
        {
            baseField["m_isActive"].AsBool = active;
            asset.SetNewData(baseField);
        }
    }

    public void Replace<TData>(AssetFileInfo asset, IObjectSource<TData> source) where TData : IWriteTo
    {
        var baseField = GetBaseField(asset);
        var writer = new AssetFieldWriter(logger, baseField);
        source.Deserialize().WriteTo(writer);
        asset.SetNewData(baseField);
    }

    public void Replace<TData>(AssetFileInfo asset, TData data) where TData : IWriteTo
    {
        Replace(asset, ObjectSource.Create(() => data));
    }

    public void ReplaceResource(
        AssetFileInfo asset,
        string resourceName,
        IStreamSource source,
        Action<ResourceReplacerContext> replacerAction)
    {
        if (!resourceBlobs.TryGetValue(resourceName, out var blob))
        {
            resourceBlobs[resourceName] = blob = new();

            var data = BundleHelper.LoadAssetDataFromBundle(bundleFileInstance.file, resourceName);
            if (data == null)
            {
                logger.LogWarning("Cannot find resource named '{resourceName}'.", resourceName);
            }
            else
            {
                blob.AddChunk(data);
            }
        }

        using var sourceStream = source.OpenRead();
        using var targetStream = new MemoryStream();

        sourceStream.CopyTo(targetStream);

        var offset = blob.Length;
        var size = targetStream.Length;

        blob.AddChunk(targetStream.ToArray());

        var baseField = GetBaseField(asset);
        replacerAction(new ResourceReplacerContext(baseField, offset, size));
        asset.SetNewData(baseField);
    }

    public void Write(FileSource source, AssetBundleCompressionType compressionType)
    {
        if (assetsFileInstances.Values.All(assetsFileInstance => assetsFileInstance.file.AssetInfos.All(info => info.Replacer is null)))
        {
            if (source.CanUnroll())
            {
                logger.LogInformation("unrolling bundle...");

                unrollSources.Add(source);
            }

            return;
        }

        var bundleFile = bundleFileInstance.file;
        var directoryInfo = bundleFile.BlockAndDirInfo.DirectoryInfos;

        foreach (var (assetsFileIndex, assetsFileInstance) in assetsFileInstances)
        {
            directoryInfo[assetsFileIndex].SetNewData(assetsFileInstance.file);
        }

        foreach (var (key, value) in resourceBlobs)
        {
            var index = bundleFile.GetFileIndex(key);

            if (index < 0)
            {
                logger.LogError("cannot find bundle file '{name}'.", key);
                continue;
            }

            directoryInfo[index].SetNewData(value.ToArray());
        }

        if (compressionType == AssetBundleCompressionType.None)
        {
            var target = source.CreateTarget();
            try
            {
                WriteUncompressed(target.Stream);
                fileTargetCollector.AddTarget(target);
                target = null;
            }
            finally
            {
                target?.Dispose();
            }
        }
        else
        {
            WriteCompressed(source, compressionType);
        }
    }

    private AssetsFileInstance LookupAssetsFileInstance(AssetFileInfo asset)
    {
        if (!assetFileInstanceByAssetLookup.TryGetValue(asset, out var assetsFileInstance))
        {
            throw new NotSupportedException("Unknown asset.");
        }

        return assetsFileInstance;
    }

    private AssetsFileInstance LookupAssetsFileInstance(AssetTypeValueField field)
    {
        if (!assetFileInstanceByFieldLookup.TryGetValue(field, out var assetsFileInstance))
        {
            throw new NotSupportedException("Unknown field.");
        }

        return assetsFileInstance;
    }

    private void WriteUncompressed(Stream stream)
    {
        using var writer = new AssetsFileWriter(stream);
        logger.LogInformation("writing bundle...");
        bundleFileInstance.file.Write(writer);
        logger.LogInformation("writing bundle completed.");
    }

    private MemoryStream GetUncompressedStream()
    {
        using var ms = new MemoryStream();
        WriteUncompressed(ms);
        return new MemoryStream(ms.ToArray());
    }

    private void WriteCompressed(FileSource source, AssetBundleCompressionType compressionType)
    {
        var uncompressedBundle = new AssetBundleFile();

        using var uncompressedStream = GetUncompressedStream();
        using var uncompressedReader = new AssetsFileReader(uncompressedStream);

        uncompressedBundle.Read(uncompressedReader);

        var target = source.CreateTarget();
        try
        {
            using (var writer = new AssetsFileWriter(target.Stream))
            {
                logger.LogInformation("compressing bundle [{compression}]...", compressionType);
                uncompressedBundle.Pack(writer, compressionType);
            }

            uncompressedBundle.Close();

            fileTargetCollector.AddTarget(target);
            target = null;
        }
        finally
        {
            target?.Dispose();
        }
    }

    private Dictionary<int, AssetsFileInstance> LoadAssetsFileInstances()
    {
        var bundleFile = bundleFileInstance.file;
        var inf = bundleFile.BlockAndDirInfo.DirectoryInfos;
        var map = new Dictionary<int, AssetsFileInstance>();

        for (var i = 0; i < inf.Count; i++)
        {
            if (inf[i].Flags == 0)
            {
                continue;
            }

            if (!bundleFile.IsAssetsFile(i))
            {
                continue;
            }

            var assetsFileInstance = assetsManager.LoadAssetsFileFromBundle(bundleFileInstance, i);
            assetsManager.LoadClassDatabaseFromPackage(assetsFileInstance.file.Metadata.UnityVersion);
            map[i] = assetsFileInstance;
        }

        return map;
    }

    private void LoadPaths()
    {
        foreach (var inf in GetAssets(AssetClassID.AssetBundle))
        {
            var baseField = GetBaseField(inf);

            foreach (var data in baseField["m_Container.Array"].Children)
            {
                var path = data[0].AsString;
                var pathId = data[1]["asset.m_PathID"].AsLong;
                paths[pathId] = path;
            }
        }
    }

    private void LoadGameObjects()
    {
        gameObjectMap.Clear();
        gameObjects.Clear();

        var parents = new Dictionary<long, long>();

        foreach (var inf in GetAssets(AssetClassID.GameObject))
        {
            var baseField = GetBaseField(inf);

            gameObjectMap.TryAdd(inf.PathId, new GameObject(inf, baseField));

            foreach (var data in baseField["m_Component.Array"].Children)
            {
                var component = ResolveGameObject(inf, data[0]);
                if (component == null || !IsTransform(component.TypeId))
                {
                    continue;
                }

                var father = ResolveGameObject(component.Asset, component.Field["m_Father"]);
                if (father == null)
                {
                    continue;
                }

                var gameObject = ResolveGameObject(father.Asset, father.Field["m_GameObject"]);
                if (gameObject == null)
                {
                    continue;
                }

                parents[inf.PathId] = gameObject.Asset.PathId;
            }
        }

        foreach (var (childId, parentId) in parents)
        {
            var child = gameObjectMap[childId];
            var parent = gameObjectMap[parentId];

            child.Parent = parent;
            
            if (child.Name.Length == 0)
            {
                child.Name = $"[{parent.Children.Count}]";
            }

            parent.Children.Add(child);
        }

        foreach (var value in gameObjectMap.Values)
        {
            if (value.Parent == null)
            {
                gameObjects.Add(value);
            }

            value.Children.BuildLookupTable();
        }

        static bool IsTransform(AssetClassID typeId) => typeId switch
        {
            AssetClassID.Transform or
            AssetClassID.RectTransform => true,
            _ => false,
        };
    }

    private AssetBundleFile GetUnpackedBundle()
    {
        if (unpackedBundleFile == null)
        {
            unpackedBundleFile = bundleFileInstance.file;

            if (unpackedBundleFile.DataIsCompressed)
            {
                unpackedBundleFile = BundleHelper.UnpackBundle(unpackedBundleFile);
            }
        }

        return unpackedBundleFile;
    }
}

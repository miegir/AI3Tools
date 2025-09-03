using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace AI3Tools;

internal class BundleManager(ILogger logger, FileSource source)
{
    private const string DefaultTextExtension = ".txt";
    private const string DefaultTexture2DExtension = ".png";
    private const string DefaultAssetExtension = ".asset";

    internal void Export(ExportArguments arguments, string scenesPath)
    {
        using var bundleFile = new BundleFile(logger, source.OpenRead());

        Enumerate()
            .Scoped(logger, "asset")
            .Run();

        IEnumerable<Action> Enumerate()
        {
            if (arguments.Force || !File.Exists(scenesPath))
            {
                var scenesManager = new ScenesManager();

                foreach (var asset in bundleFile.GetAssets(AssetClassID.MonoBehaviour))
                {
                    var baseField = bundleFile.GetBaseField(asset);

                    var textField = baseField["m_text"];
                    if (textField.IsDummy)
                    {
                        continue;
                    }

                    var text = textField.AsString;
                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    var gameObjectPPtr = baseField["m_GameObject"];
                    if (gameObjectPPtr.IsDummy)
                    {
                        continue;
                    }

                    var gameObject = bundleFile.ResolveGameObject(gameObjectPPtr);
                    if (gameObject == null)
                    {
                        continue;
                    }

                    var path = gameObject.GetPath();

                    scenesManager.AddText(path, text);
                }

                if (!scenesManager.IsEmpty)
                {
                    yield return () =>
                    {
                        logger.LogInformation("exporting scenes...");

                        using var target = new FileTarget(scenesPath);
                        scenesManager.Export(target.Stream);
                        target.Commit();
                    };
                }
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.Texture2D))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultTexture2DExtension);
                var path = Path.Combine(arguments.ExportDirectory, name);
                if (!arguments.Force && File.Exists(path)) continue;
                var baseField = bundleFile.GetBaseField(asset);
                var textureFile = TextureFile.ReadTextureFile(baseField);
                if (textureFile.m_Width == 0 || textureFile.m_Height == 0) continue;
                yield return () =>
                {
                    logger.LogInformation("exporting texture {name}...", name);

                    try
                    {
                        var textureData = bundleFile.GetTextureData(textureFile);

                        using var image = Image.LoadPixelData<Bgra32>(
                            textureData, textureFile.m_Width, textureFile.m_Height);

                        image.Mutate(m => m.Flip(FlipMode.Vertical));

                        using var target = new FileTarget(path);
                        image.Save(target, PngFormat.Instance);
                        target.Commit();
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "error exporting texture {name}...", name);
                    }
                };
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.TextAsset))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultTextExtension);
                var path = Path.Combine(arguments.ExportDirectory, name);
                if (!arguments.Force && File.Exists(path)) continue;
                yield return () =>
                {
                    logger.LogInformation("exporting text {name}...", name);

                    var baseField = bundleFile.GetBaseField(asset);
                    var script = baseField["m_Script"].AsString;

                    using var target = new FileTarget(path);
                    using (var writer = new StreamWriter(target.Stream, Encoding.UTF8))
                    {
                        writer.Write(script);
                    }

                    target.Commit();
                };
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.VideoClip))
            {
                var baseField = bundleFile.GetBaseField(asset);
                var fileData = new VideoClipFileData(baseField);
                if (!fileData.IsValid) continue;
                var name = fileData.OriginalPath;
                var path = Path.Combine(arguments.ExportDirectory, name);
                if (!arguments.Force && File.Exists(path))
                {
                    continue;
                }

                var data = bundleFile.ReadResource(
                    fileData.ResourceName, fileData.Offset, fileData.Size);

                if (data == null)
                {
                    continue;
                }

                yield return () =>
                {
                    logger.LogInformation("exporting video clip {name}...", name);

                    using var target = new FileTarget(path);
                    target.Stream.Write(data, 0, data.Length);
                    target.Commit();
                };
            }
        }
    }

    public bool Import(
        ImportArguments arguments,
        BundleResolverFactory bundleResolverFactory,
        BundleFileSource bundleFileSource,
        GameObjectSource gameObjectSource,
        SourceChangeTracker sourceChangeTracker)
    {
        var hasChanges = arguments.ForceTargets || sourceChangeTracker.HasChanges();

        var compression = arguments.BundleCompression switch
        {
            BundleCompressionType.Default => GetCompressionType(source),
            BundleCompressionType.LZ4 => AssetBundleCompressionType.LZ4,
            BundleCompressionType.LZMA => AssetBundleCompressionType.LZMA,
            _ => AssetBundleCompressionType.None,
        };

        if (!hasChanges)
        {
            var targetCompression = GetCompressionType(source.Destination);
            if (targetCompression == compression)
            {
                return false;
            }
        }

        using var bundleFile = new BundleFile(logger, source.OpenRead());

        Enumerate()
            .Scoped(logger, "asset")
            .Run();

        bundleFile.Write(source, compression);

        return true;

        IEnumerable<Action> Enumerate()
        {
            var bundleResolver = bundleFile.CreateBundleResolver(bundleResolverFactory);

            bundleFileSource.Register(sourceChangeTracker);
            gameObjectSource.Register(sourceChangeTracker);

            foreach (var entry in gameObjectSource.Entries)
            {
                var gameObject = bundleFile.GameObjects.Find(entry.Path);
                if (gameObject == null)
                {
                    logger.LogWarning("game object not found: {path}", entry.Path);
                    continue;
                }

                if (entry.Data != null)
                {
                    yield return () =>
                    {
                        logger.LogInformation("importing game object {name}...", entry.Path);
                        var objectSource = ObjectSource.Create(entry.Data);
                        bundleFile.Replace(gameObject.Asset, objectSource);
                    };
                }

                if (entry.Text is string text)
                {
                    foreach (var component in bundleFile.GetComponents(gameObject))
                    {
                        if (component.TypeId != AssetClassID.MonoBehaviour)
                        {
                            continue;
                        }

                        var scriptName = bundleFile.ReadScriptName(bundleResolver, component.Field);
                        if (scriptName?.FullName != "TMPro.TextMeshProUGUI")
                        {
                            continue;
                        }

                        yield return () =>
                        {
                            logger.LogInformation("importing text mesh pro {name}...", entry.Path);
                            bundleFile.Replace(component.Asset, BuildTextMeshProUGUIData(text));
                        };
                    }
                }
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.Texture2D))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultTexture2DExtension);

                if (bundleFileSource.Exists)
                {
                    var assetSource = bundleFileSource.FindTexture2D(FixTextureName(name), DefaultTexture2DExtension);
                    if (assetSource is not null)
                    {
                        yield return () =>
                        {
                            logger.LogInformation("importing bundled texture {name}...", name);
                            bundleFile.Replace(asset, assetSource);
                        };

                        continue;
                    }
                }

                var sourcePath = Path.Combine(arguments.SourceDirectory, name);
                sourceChangeTracker.RegisterSource(sourcePath);

                if (!File.Exists(sourcePath)) continue;

                yield return () =>
                {
                    logger.LogInformation("importing texture {name}...", name);

                    var baseField = bundleFile.GetBaseField(asset);
                    var textureFile = TextureFile.ReadTextureFile(baseField);
                    var textureArguments = Texture2DArguments.Create(textureFile, arguments.BC7Compression);
                    var objectPath = Path.Combine(arguments.ObjectDirectory, name, textureArguments.Name);
                    var builder = new ObjectBuilder(sourcePath, objectPath, arguments.ForceObjects);

                    BuildTexture2DObject(builder, textureArguments, name);

                    var objectSource = new PhysicalObjectSource<Texture2DData>(objectPath);
                    bundleFile.Replace(asset, objectSource);
                };
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.TextAsset))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultTextExtension);
                var sourcePath = Path.Combine(arguments.SourceDirectory, name);
                sourceChangeTracker.RegisterSource(sourcePath);

                if (!File.Exists(sourcePath)) continue;

                yield return () =>
                {
                    logger.LogInformation("importing text asset {name}...", name);

                    var objectPath = Path.Combine(arguments.ObjectDirectory, name + ".pak");
                    var builder = new ObjectBuilder(sourcePath, objectPath, arguments.ForceObjects);

                    BuildTextObject(builder, name);

                    var objectSource = new PhysicalObjectSource<string>(objectPath);
                    bundleFile.Replace(asset, objectSource);
                };
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.VideoClip))
            {
                var baseField = bundleFile.GetBaseField(asset);
                var fileData = new VideoClipFileData(baseField);
                if (!fileData.IsValid) continue;
                var name = fileData.OriginalPath;
                var streamSource = VideoClipHelper.GetStreamSource(Path.Combine(arguments.SourceDirectory, name));
                streamSource.Register(sourceChangeTracker);
                if (!streamSource.Exists) continue;
                yield return () =>
                {
                    logger.LogInformation("importing video clip {name}...", name);

                    bundleFile.ReplaceResource(
                        asset,
                        fileData.ResourceName,
                        streamSource,
                        VideoClipFileData.Write);
                };
            }

            if (bundleFileSource.Exists)
            {
                foreach (var asset in bundleFile.GetAssets(AssetClassID.MonoBehaviour))
                {
                    var assetSource = FindFontAsset(bundleFile, bundleResolver, bundleFileSource, asset);
                    if (assetSource == null) continue;
                    yield return () =>
                    {
                        var name = bundleFile.ReadAssetName(asset, DefaultAssetExtension);
                        logger.LogInformation("import font {name}...", name);
                        //asset.SetNewData(assetSource.Deserialize());
                        bundleFile.Replace(asset, assetSource);
                    };
                }
            }
        }
    }

    public bool Muster(
        ObjectPath root,
        MusterArguments arguments,
        BundleResolverFactory bundleResolverFactory,
        BundleFileSource bundleFileSource,
        GameObjectSource gameObjectSource)
    {
        using var bundleFile = new BundleFile(logger, source.OpenRead());

        return Enumerate()
            .Scoped(logger, "asset")
            .Run();

        IEnumerable<Action> Enumerate()
        {
            var bundleResolver = bundleFile.CreateBundleResolver(bundleResolverFactory);

            foreach (var entry in gameObjectSource.Entries)
            {
                var gameObject = bundleFile.GameObjects.Find(entry.Path);
                if (gameObject == null) continue;

                if (entry.Data != null)
                {
                    yield return () =>
                    {
                        var name = entry.Path + ".god";
                        var objectPath = Path.Combine(arguments.ObjectDirectory, name);
                        var builder = new ObjectBuilder(
                            gameObjectSource.Destination, objectPath, arguments.ForceObjects);

                        builder.Build(_ => entry.Data);

                        arguments.Sink.ReportObject(root.Append(name), objectPath);
                    };
                }

                if (entry.Text is string text)
                {
                    foreach (var component in bundleFile.GetComponents(gameObject))
                    {
                        if (component.TypeId != AssetClassID.MonoBehaviour)
                        {
                            continue;
                        }

                        var scriptName = bundleFile.ReadScriptName(bundleResolver, component.Field);
                        if (scriptName?.FullName != "TMPro.TextMeshProUGUI")
                        {
                            continue;
                        }

                        yield return () =>
                        {
                            var name = bundleFile.ReadAssetName(component.Asset, DefaultAssetExtension) + ".TMProUGUI.pak";
                            var objectPath = Path.Combine(arguments.ObjectDirectory, name);
                            var builder = new ObjectBuilder(
                                gameObjectSource.Destination, objectPath, arguments.ForceObjects);

                            builder.Build(_ => BuildTextMeshProUGUIData(text));

                            arguments.Sink.ReportObject(root.Append(name), objectPath);
                        };
                    }
                }
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.Texture2D))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultTexture2DExtension);

                var assetSource = bundleFileSource.FindTexture2D(FixTextureName(name), DefaultTexture2DExtension);
                if (assetSource is not null)
                {
                    yield return () =>
                    {
                        var objectPath = Path.Combine(arguments.ObjectDirectory, name + ".texture2d");

                        var builder = new ObjectBuilder(
                            bundleFileSource.Destination, objectPath, arguments.ForceObjects);

                        builder.Build(_ => assetSource.Deserialize());

                        arguments.Sink.ReportObject(root.Append(name + ".pak"), objectPath);
                    };

                    continue;
                }

                var sourcePath = Path.Combine(arguments.SourceDirectory, name);

                if (!File.Exists(sourcePath)) continue;

                yield return () =>
                {
                    var baseField = bundleFile.GetBaseField(asset);
                    var textureFile = TextureFile.ReadTextureFile(baseField);
                    var textureArguments = Texture2DArguments.Create(textureFile, arguments.BC7Compression);
                    var objectPath = Path.Combine(arguments.ObjectDirectory, name, textureArguments.Name);
                    var builder = new ObjectBuilder(sourcePath, objectPath, arguments.ForceObjects);

                    BuildTexture2DObject(builder, textureArguments, name);

                    arguments.Sink.ReportObject(root.Append(name + ".pak"), objectPath);
                };
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.TextAsset))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultTextExtension);
                var sourcePath = Path.Combine(arguments.SourceDirectory, name);

                if (!File.Exists(sourcePath)) continue;

                yield return () =>
                {
                    var objectName = name + ".pak";
                    var objectPath = Path.Combine(arguments.ObjectDirectory, objectName);
                    var builder = new ObjectBuilder(sourcePath, objectPath, arguments.ForceObjects);

                    BuildTextObject(builder, name);

                    arguments.Sink.ReportObject(root.Append(objectName), objectPath);
                };
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.VideoClip))
            {
                var baseField = bundleFile.GetBaseField(asset);
                var fileData = new VideoClipFileData(baseField);
                if (!fileData.IsValid) continue;
                var name = fileData.OriginalPath;
                var streamSource = VideoClipHelper.GetStreamSource(Path.Combine(arguments.SourceDirectory, name));
                if (!streamSource.Exists) continue;
                yield return () =>
                {
                    arguments.Sink.ReportObject(root.Append(name), streamSource);
                };
            }

            if (bundleFileSource.Exists)
            {
                foreach (var asset in bundleFile.GetAssets(AssetClassID.MonoBehaviour))
                {
                    var assetSource = FindFontAsset(bundleFile, bundleResolver, bundleFileSource, asset);
                    if (assetSource == null) continue;
                    yield return () =>
                    {
                        var name = bundleFile.ReadAssetName(asset, DefaultAssetExtension);
                        var objectPath = Path.Combine(arguments.ObjectDirectory, name + ".fnt");

                        var builder = new ObjectBuilder(
                            bundleFileSource.Destination, objectPath, arguments.ForceObjects);

                        builder.Build(_ => assetSource.Deserialize());

                        arguments.Sink.ReportObject(root.Append(name + ".fnt"), objectPath);
                    };
                }
            }
        }
    }

    public void Unpack(UnpackArguments arguments, ObjectPath root)
    {
        using var bundleFile = new BundleFile(logger, source.OpenRead());

        Enumerate()
            .Scoped(logger, "asset")
            .Run();

        var compression = arguments.BundleCompression switch
        {
            BundleCompressionType.Default => GetCompressionType(source),
            BundleCompressionType.LZ4 => AssetBundleCompressionType.LZ4,
            BundleCompressionType.LZMA => AssetBundleCompressionType.LZMA,
            _ => AssetBundleCompressionType.None,
        };

        bundleFile.Write(source, compression);

        IEnumerable<Action> Enumerate()
        {
            foreach (var asset in bundleFile.GetAssets(AssetClassID.GameObject))
            {
                var gameObject = bundleFile.ResolveGameObject(asset);
                if (gameObject == null)
                {
                    continue;
                }

                var name = gameObject.GetPath();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (arguments.Container.TryGetEntry(root.Append(name + ".god"), out var entry))
                {
                    yield return () =>
                    {
                        logger.LogInformation("importing game object {name}...", name);
                        bundleFile.Replace(gameObject.Asset, entry.AsObjectSource<GameObjectData>());
                    };
                }
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.Texture2D))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultTexture2DExtension);
                if (arguments.Container.TryGetEntry(root.Append(name + ".pak"), out var entry))
                {
                    yield return () =>
                    {
                        logger.LogInformation("importing texture {name}...", name);
                        bundleFile.Replace(asset, entry.AsObjectSource<Texture2DData>());
                    };
                }
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.TextAsset))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultTextExtension);
                if (arguments.Container.TryGetEntry(root.Append(name + ".pak"), out var entry))
                {
                    yield return () =>
                    {
                        logger.LogInformation("importing text {name}...", name);
                        bundleFile.Replace(asset, entry.AsObjectSource<string>());
                    };
                }
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.MonoBehaviour))
            {
                var name = bundleFile.ReadAssetName(asset, DefaultAssetExtension);

                if (arguments.Container.TryGetEntry(root.Append(name + ".fnt"), out var fntEntry))
                {
                    yield return () =>
                    {
                        logger.LogInformation("importing font {name}...", name);
                        bundleFile.Replace(asset, fntEntry.AsObjectSource<FontAssetData>());
                    };
                }

                if (arguments.Container.TryGetEntry(root.Append(name + ".TMProUGUI.pak"), out var tmpEntry))
                {
                    yield return () =>
                    {
                        logger.LogInformation("importing text mesh pro {name}...", name);
                        bundleFile.Replace(asset, tmpEntry.AsObjectSource<TextMeshProUGUIData>());
                    };
                }
            }

            foreach (var asset in bundleFile.GetAssets(AssetClassID.VideoClip))
            {
                var baseField = bundleFile.GetBaseField(asset);
                var fileData = new VideoClipFileData(baseField);
                if (!fileData.IsValid) continue;
                var name = fileData.OriginalPath;
                if (arguments.Container.TryGetEntry(root.Append(name), out var entry))
                {
                    yield return () =>
                    {
                        logger.LogInformation("importing video clip {name}...", name);
                        bundleFile.ReplaceResource(
                            asset,
                            fileData.ResourceName,
                            entry.AsStreamSource(),
                            VideoClipFileData.Write);
                    };
                }
            }
        }
    }

    private static IObjectSource<FontAssetData>? FindFontAsset(
        BundleFile bundleFile,
        BundleResolver bundleResolver,
        BundleFileSource bundleFileSource,
        AssetFileInfo asset)
    {
        var baseField = bundleFile.GetBaseField(asset);
        var scriptName = bundleFile.ReadScriptName(bundleResolver, baseField);
        if (scriptName?.FullName != "TMPro.TMP_FontAsset")
        {
            return null;
        }

        var name = bundleFile.ReadAssetName(asset, DefaultAssetExtension);
        return bundleFileSource.FindMonoBehavior(
            bundleResolver,
            name,
            scriptName,
            DefaultAssetExtension,
            ReadFontAsset,
            data => data.m_UsedGlyphRects?.Length > 0);
    }

    private static FontAssetData ReadFontAsset(MonoBehaviorContext context)
    {
        var data = FontAssetData.ReadFontAsset(context.BaseField);

        if (data.m_FaceInfo is not null and var info)
        {
            info.m_LineHeight = info.m_PointSize * 2;
        }

        return data;
    }

    private void BuildTexture2DObject(ObjectBuilder builder, Texture2DArguments arguments, string name)
    {
        builder.Build(stream =>
        {
            logger.LogInformation("building texture {name}...", name);

            return new Texture2DEncoder(logger, arguments, stream, name).Encode();
        });
    }

    private void BuildTextObject(ObjectBuilder builder, string name)
    {
        builder.Build(stream =>
        {
            logger.LogInformation("building text {name}...", name);

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        });
    }

    private static TextMeshProUGUIData BuildTextMeshProUGUIData(string text)
    {
        return new TextMeshProUGUIData(TextCompressor.Compress(text));
    }

    private static AssetBundleCompressionType GetCompressionType(IFileStreamSource source)
    {
        using var stream = source.OpenRead();

        var targetAssetsManager = new AssetsManager();
        var targetBundle = targetAssetsManager.LoadBundleFile(stream, unpackIfPacked: false);

        return (targetBundle.file.Header.GetCompressionType()) switch
        {
            1 => AssetBundleCompressionType.LZMA,
            2 or 3 => AssetBundleCompressionType.LZ4,
            _ => AssetBundleCompressionType.None,
        };
    }

    private static string FixTextureName(string name)
    {
        if (name.Contains("-font-texture+"))
        {
            name = name.Replace("-font-texture+", "-font+");
            name = Path.ChangeExtension(name, ".asset");
        }

        return name;
    }
}

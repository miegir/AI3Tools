namespace AI3Tools;

internal static class AssetNameHelper
{
    public static string Pack(string name, string defaultExtension, string? pathName = null)
    {
        var extension = Path.GetExtension(name);

        if (string.IsNullOrEmpty(extension))
        {
            extension = defaultExtension;
        }

        name = Path.GetFileNameWithoutExtension(name);
        if (!string.IsNullOrEmpty(pathName))
        {
            var pathExtension = Path.GetExtension(pathName);
            if (!string.IsNullOrEmpty(pathExtension))
            {
                return pathName;
            }

            if (pathName.Contains('/'))
            {
                return $"{pathName}{extension}";
            }

            var pos = pathName.IndexOf('+');
            if (pos >= 0)
            {
                pathName = pathName[..pos];
            }

            name = $"{name}+{pathName}";
        }
        else if (name.Contains('+'))
        {
            name += '+';
        }

        return $"{name}{extension}";
    }

    public static string Unpack(string name) => Unpack(name, out _);

    public static string Unpack(string name, out string? pathName)
    {
        var extension = Path.GetExtension(name);

        name = Path.GetFileNameWithoutExtension(name);

        var pos = name.LastIndexOf('+');
        if (pos >= 0)
        {
            pathName = name[(pos+1)..];

            name = name[..pos];
        }
        else
        {
            pathName = null;
        }

        return $"{name}{extension}";
    }
}

using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Extensions.Logging;

namespace AI3Tools;

internal class AssetFieldWriter(ILogger logger, AssetTypeValueField field, string? prefix = null)
{
    public string Path => $"{prefix}{@field.FieldName}";

    public AssetFieldWriter this[string name]
    {
        get
        {
            var child = field[name];

            if (!field.IsDummy && child.IsDummy && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Missing child '{path}.{name}'.", Path, name);
            }

            return Child(child);
        }
    }

    public void Write(byte value)
    {
        if (!field.IsDummy)
        {
            field.AsByte = value;
        }
    }

    public void Write(int value)
    {
        if (!field.IsDummy)
        {
            field.AsInt = value;
        }
    }

    public void Write(uint value)
    {
        if (!field.IsDummy)
        {
            field.AsUInt = value;
        }
    }

    public void Write(long value)
    {
        if (!field.IsDummy)
        {
            field.AsLong = value;
        }
    }

    public void Write(ulong value)
    {
        if (!field.IsDummy)
        {
            field.AsULong = value;
        }
    }

    public void Write(float value)
    {
        if (!field.IsDummy)
        {
            field.AsFloat = value;
        }
    }

    public void Write(string? value)
    {
        if (!field.IsDummy)
        {
            field.AsString = value;
        }
    }

    public void Write<T>(T? value) where T: IWriteTo
    {
        Write(value, (f, v) => v.WriteTo(f));
    }

    public void Write<T>(T? value, Action<AssetFieldWriter, T> writer)
    {
        if (value is not null && !field.IsDummy)
        {
            writer(this, value);
        }
    }

    public void Write<T>(T[]? value) where T : IWriteTo
    {
        Write(value, (f, v) => v.WriteTo(f));
    }

    public void Write<T>(T[]? value, Action<AssetFieldWriter, T> writer)
    {
        if (value is not null && !field.IsDummy)
        {
            var array = field["Array"];
            if (!array.IsDummy)
            {
                var oldChildren = array.Children;
                var newChildren = new List<AssetTypeValueField>(value.Length);
                var prototype = array.TemplateField.Children[1];

                for (var i = 0; i < value.Length; i++)
                {
                    var child = i < oldChildren.Count
                        ? oldChildren[i]
                        : ValueBuilder.DefaultValueFieldFromTemplate(prototype);

                    newChildren.Add(child);
                    writer(Child(child, $"[{i}]"), value[i]);
                }

                array.Children = newChildren;
            }
        }
    }

    private AssetFieldWriter Child(AssetTypeValueField child, string? name = null)
    {
        return new AssetFieldWriter(logger, child, $"{Path}{name}.");
    }
}

using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace AI3Tools;

internal static partial class AssetTypeValueFieldExtensions
{
    public static void Read(this AssetTypeValueField field, ref byte value)
    {
        if (!field.IsDummy)
        {
            value = (byte)field.AsUInt;
        }
    }

    public static void Read(this AssetTypeValueField field, ref int value)
    {
        if (!field.IsDummy)
        {
            value = field.AsInt;
        }
    }

    public static void Read(this AssetTypeValueField field, ref long value)
    {
        if (!field.IsDummy)
        {
            value = field.AsLong;
        }
    }

    public static void Read(this AssetTypeValueField field, ref ulong value)
    {
        if (!field.IsDummy)
        {
            value = field.AsULong;
        }
    }

    public static void Read(this AssetTypeValueField field, ref uint value)
    {
        if (!field.IsDummy)
        {
            value = field.AsUInt;
        }
    }

    public static void Read(this AssetTypeValueField field, ref float value)
    {
        if (!field.IsDummy)
        {
            value = field.AsFloat;
        }
    }

    public static void Read(this AssetTypeValueField field, ref string? value)
    {
        if (!field.IsDummy)
        {
            value = field.AsString;
        }
    }

    public static void Read<T>(this AssetTypeValueField field, ref T? value, Func<AssetTypeValueField, T> reader)
    {
        if (!field.IsDummy)
        {
            value = reader(field);
        }
    }

    public static void Read<T>(this AssetTypeValueField field, ref T[]? value, Func<AssetTypeValueField, T> reader)
    {
        if (!field.IsDummy)
        {
            var array = field["Array"];

            if (!array.IsDummy)
            {
                value = [.. array.Children.Select(reader)];
            }
        }
    }

    public static void Write(this AssetTypeValueField field, byte value)
    {
        if (!field.IsDummy)
        {
            field.AsByte = value;
        }
    }

    public static void Write(this AssetTypeValueField field, int value)
    {
        if (!field.IsDummy)
        {
            field.AsInt = value;
        }
    }

    public static void Write(this AssetTypeValueField field, uint value)
    {
        if (!field.IsDummy)
        {
            field.AsUInt = value;
        }
    }

    public static void Write(this AssetTypeValueField field, long value)
    {
        if (!field.IsDummy)
        {
            field.AsLong = value;
        }
    }

    public static void Write(this AssetTypeValueField field, ulong value)
    {
        if (!field.IsDummy)
        {
            field.AsULong = value;
        }
    }

    public static void Write(this AssetTypeValueField field, float value)
    {
        if (!field.IsDummy)
        {
            field.AsFloat = value;
        }
    }

    public static void Write(this AssetTypeValueField field, string? value)
    {
        if (!field.IsDummy)
        {
            field.AsString = value;
        }
    }

    public static void Write<T>(this AssetTypeValueField field, T? value) where T : IWriteTo
    {
        Write(field, value, (f, v) => v.WriteTo(f));
    }

    public static void Write<T>(this AssetTypeValueField field, T? value, Action<AssetTypeValueField, T> writer)
    {
        if (value is not null && !field.IsDummy)
        {
            writer(field, value);
        }
    }

    public static void Write<T>(this AssetTypeValueField field, T[]? value) where T : IWriteTo
    {
        Write(field, value, (f, v) => v.WriteTo(f));
    }

    public static void Write<T>(this AssetTypeValueField field, T[]? value, Action<AssetTypeValueField, T> writer)
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
                    writer(child, value[i]);
                }

                array.Children = newChildren;
            }
        }
    }
}

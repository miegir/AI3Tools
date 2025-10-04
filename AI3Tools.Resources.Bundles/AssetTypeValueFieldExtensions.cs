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
}

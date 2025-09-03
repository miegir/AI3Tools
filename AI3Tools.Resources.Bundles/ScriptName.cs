using AssetsTools.NET;
using System.Diagnostics;

namespace AI3Tools;

[DebuggerDisplay("{FullName,nq}")]
internal record ScriptName(string Name, string ClassName, string Namespace, string AssemblyName)
{
    public static ScriptName Read(AssetTypeValueField baseField)
    {
        var name = baseField["m_Name"].AsString;
        var className = baseField["m_ClassName"].AsString;
        var namespaceName = baseField["m_Namespace"].AsString;
        var assemblyName = baseField["m_AssemblyName"].AsString;
        return new ScriptName(name, className, namespaceName, assemblyName);
    }

    public string FullName => $"{Namespace}.{ClassName}";
}

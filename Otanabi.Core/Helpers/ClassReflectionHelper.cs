using System.Reflection;
using Otanabi.Core.Models;

namespace Otanabi.Core.Helpers;

public sealed class ClassReflectionHelper
{
    private readonly string AssemblyName = "Otanabi.Extensions";
    private string ExNameSpace => $"{AssemblyName}.Extractors";
    private string VidNameSpace => $"{AssemblyName}.VideoExtractors";

    public Provider GetProviderPropsByType(Type type)
    {
        var c = Activator.CreateInstance(type);
        var sourceName = (Provider)
            type.InvokeMember("GenProvider", BindingFlags.InvokeMethod, null, c, Array.Empty<object>());
        return sourceName;
    }

    public Type[] ExtractAssembliesOnlyClass(string namesp)
    {
        var lTypes = new List<Type>();

        var data = LoadExtensionAssembly().GetTypes().Where(t => t.Namespace == namesp).Select(t => t).ToList();
        foreach (var type in data)
        {
            if (!type.Name.Contains("<"))
            {
                lTypes.Add(type);
            }
        }
        return lTypes.ToArray();
    }

    public Assembly LoadExtensionAssembly()
    {
        var currDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Assembly.LoadFile(Path.Join(currDir, $"{AssemblyName}.dll"));
    }

    public string GetAssemblyPath()
    {
        var currDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Join(currDir, $"{AssemblyName}.dll");
    }

    private Type GetExtensionType(string className)
    {
        var types = LoadExtensionAssembly().GetTypes();
        return LoadExtensionAssembly().GetTypes().Where(t => t.FullName == className).ToList().First();
    }

    public (MethodInfo, object) GetMethodFromProvider(string methodName, Provider provider)
    {
        var provCl = provider.Name.Substring(0, 1).ToUpper() + provider.Name.Substring(1).ToLower();
        var extractorType = GetExtensionType($"{ExNameSpace}.{provCl}Extractor");
        var extractorInstance = Activator.CreateInstance(extractorType);
        var method = extractorType.GetMethod(methodName);
        return (method, extractorInstance);
    }

    public (MethodInfo, object) GetMethodFromVideoSource(VideoSource source)
    {
        var extractorType = GetExtensionType($"{VidNameSpace}.{source.Server}Extractor");
        var extractorInstance = Activator.CreateInstance(extractorType);
        var method = extractorType.GetMethod("GetStreamAsync");
        return (method, extractorInstance);
    }

    public Provider[] GetProviders()
    {
        var providers = new List<Provider>();
        var data = ExtractAssembliesOnlyClass(ExNameSpace);
        foreach (var cls in data)
        {
            var provider = GetProviderPropsByType(cls);
            if (provider.Active)
            {
                providers.Add(provider);
            }
        }
        return providers.ToArray();
    }
}

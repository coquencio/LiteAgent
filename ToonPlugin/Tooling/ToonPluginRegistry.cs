using System.Reflection;

namespace ToonPlugin.Tooling;
internal class ToonPluginRegistry
{
    private readonly Dictionary<string, ToonPluginDefinition> _plugins = new();

    public void RegisterPlugins<T>(T instance) where T : class
    {
        var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                               .Where(m => m.GetCustomAttribute<ToonPluginAttribute>() != null);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<ToonPluginAttribute>();
            var definition = new ToonPluginDefinition
            {
                Name = method.Name.ToLower(),
                Description = attr?.Description,
                Method = method,
                TargetInstance = instance,
                Parameters = method.GetParameters()
            };

            _plugins[definition.Name] = definition;
        }
    }
    public ToonPluginDefinition? GetDefinition(string functionName)
    {
        if (_plugins.TryGetValue(functionName, out var definition))
            return definition;
        
        return null;
    }
    public string GetPluginCatalog() =>
        string.Join("\n", _plugins.Values.Select(v => v.ToSignature()));
    
}


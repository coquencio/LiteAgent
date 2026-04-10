using System.Reflection;

namespace LiteAgent.Tooling;
internal class LitePluginRegistry
{
    private readonly Dictionary<string, LitePluginDefinition> _plugins = new();

    public void RegisterPlugins<T>(T instance) where T : class
    {
        var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                               .Where(m => m.GetCustomAttribute<LitePlugin>() != null);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<LitePlugin>();
            var definition = new LitePluginDefinition
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
    public LitePluginDefinition? GetDefinition(string functionName)
    {
        if (_plugins.TryGetValue(functionName, out var definition))
            return definition;
        
        return null;
    }
    public string GetPluginCatalog() =>
        string.Join("\n", _plugins.Values.Select(v => v.ToSignature()));
    
}


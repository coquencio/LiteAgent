using System.Reflection;
using System.Text.RegularExpressions;

namespace LiteAgent.Tooling;
internal class LitePluginRegistry
{
    private readonly Dictionary<string, LitePluginDefinition> _plugins = new();

    public void RegisterPlugins(object instance)
    {
        var methods = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<LitePlugin>() != null);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<LitePlugin>();
            var definition = new LitePluginDefinition
            {
                Name = Regex.Replace(method.Name, @"(?<!^)([A-Z])", "_$1").ToLower(),
                Description = attr?.Description,
                Method = method,
                TargetInstance = instance,
                Parameters = method.GetParameters(),
                MaxRetries = attr?.MaxRetries ?? 0
            };
            if (_plugins.ContainsKey(definition.Name))
                throw new InvalidOperationException($"A plugin with the name '{definition.Name}' is already registered.");
            
            if (_plugins.ContainsKey("execute_sequence"))
                throw new InvalidOperationException("Conflict detected: The plugin name 'ExecuteSequence' is reserved for the LiteAgent internal orchestration engine. Please use a different name for your custom plugin to avoid conflicts with the agentic chaining system.");

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


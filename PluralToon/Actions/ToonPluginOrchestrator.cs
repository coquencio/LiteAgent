using ToonPlugin.Prompting;
using ToonPlugin.Tooling;
using System.Collections;
using System.Reflection;

namespace ToonPlugin.Actions;
public class ToonPluginOrchestrator
{
    private readonly ToonPluginRegistry _registry = new();
    private readonly PluginParser _parser = new();
    private readonly ToonPromptGenerator _generator;
    public ToonPluginOrchestrator()
    {
        _generator = new(_registry);
    }
    public void RegisterKit<T>(T instance) where T : class =>
        _registry.RegisterPlugins(instance);

    public string GetSystemInstructions() => _generator.GetSystemPrompt();
    public async Task<string> ExecuteMatchAsync(string llmResponse)
    {
        if (!_parser.IsToonCall(llmResponse)) 
            return llmResponse;

        var call = _parser.Parse(llmResponse);
        if (call == null)
        {
            return "Error: Invalid TOON syntax";
        }

        var definition = _registry.GetDefinition(call.FunctionName);
        if (definition == null)
        {
            return $"Error: Claw '{call.FunctionName}' not found";
        }

        try
        {
            var parameters = _parser.MapArguments(definition.Parameters, call.Arguments);

            var result = definition.Method.Invoke(definition.TargetInstance, parameters);

            if (result is Task task)
            {
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task)?.ToString() ?? "Task completed";
            }

            return Serialize(result);
        }
        catch (Exception ex)
        {
            return $"Execution Error: {ex.InnerException?.Message ?? ex.Message}";
        }
    }

    private static string Serialize(object? obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();

        // 1. Primitives and Simple Types
        if (type.IsPrimitive || obj is string || obj is decimal || obj is DateTime)
        {
            return obj.ToString() ?? string.Empty;
        }

        // 2. Handle Collections (Arrays, Lists, etc.)
        if (obj is IEnumerable enumerable)
        {
            var items = new List<string>();
            foreach (var item in enumerable)
            {
                items.Add(Serialize(item));
            }
            return $"[{string.Join("|", items)}]";
        }

        // 3. Handle Complex Objects (Recursive)
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Select(p => {
                                 var value = p.GetValue(obj);
                                 return $"{p.Name.ToLower()}:{Serialize(value)}";
                             });

        return $"({string.Join(",", properties)})";
    }
}
using LiteAgent.Prompting;
using LiteAgent.Tooling;
using System.Collections;
using System.Reflection;

namespace LiteAgent.Actions;

public class LiteActions
{
    private readonly LitePluginRegistry _registry = new();
    private readonly PluginParser _parser = new();
    private readonly PromptGenerator _generator;

    public LiteActions()
    {
        _generator = new(_registry);
    }

    internal void RegisterKit(object instance) =>
        _registry.RegisterPlugins(instance);

    internal string GetSystemInstructions() => _generator.GetSystemPrompt();

    internal async Task<string> ExecuteMatchAsync(string llmResponse)
    {
        if (!_parser.IsToonCall(llmResponse))
            return llmResponse;

        var call = _parser.Parse(llmResponse);
        if (call == null)
        {
            return "Error: Invalid TOON syntax";
        }

        // --- SPECIAL HANDLING FOR ORCHESTRATION ---
        if (call.FunctionName.Equals("executesequence", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // We instantiate the orchestrator passing the current dictionary of plugins
                var orchestrator = new SequencePlugin(_registry);

                // We extract the "sequence" argument from the call
                var sequenceArg = call.Arguments.FirstOrDefault() ?? string.Empty;

                return await orchestrator.ExecuteSequence(sequenceArg);
            }
            catch (Exception ex)
            {
                return $"Orchestration Error: {ex.Message}";
            }
        }
        // ------------------------------------------

        var definition = _registry.GetDefinition(call.FunctionName);
        if (definition == null)
        {
            return $"Error: plugin '{call.FunctionName}' not found";
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

        if (type.IsPrimitive || obj is string || obj is decimal || obj is DateTime)
        {
            return obj.ToString() ?? string.Empty;
        }

        if (obj is IEnumerable enumerable)
        {
            var items = new List<string>();
            foreach (var item in enumerable)
            {
                items.Add(Serialize(item));
            }
            return $"[{string.Join("|", items)}]";
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Select(p => {
                                 var value = p.GetValue(obj);
                                 return $"{p.Name.ToLower()}:{Serialize(value)}";
                             });

        return $"({string.Join(",", properties)})";
    }
}
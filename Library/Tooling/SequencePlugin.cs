using LiteAgent.Tooling;
namespace LiteAgent.Actions;

internal class SequencePlugin(LitePluginRegistry registry)
{
    private readonly LitePluginRegistry _registry = registry;
    private readonly PluginParser _parser = new();

    public async Task<string> ExecuteSequence(string sequence)
    {
        var steps = sequence.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var stepResults = new List<string>();

        foreach (var step in steps)
        {
            var currentInstruction = step.Trim();

            // 1. Resolve indexed variables and property access
            for (int i = 0; i < stepResults.Count; i++)
            {
                string placeholder = $"${i + 1}";

                if (currentInstruction.Contains(placeholder))
                {
                    // Handle property access: $1.property_name
                    currentInstruction = ResolveDataReferences(currentInstruction, placeholder, stepResults[i]);
                }
            }

            // 2. Fallback for $LAST
            if (stepResults.Any())
            {
                currentInstruction = currentInstruction.Replace("$LAST", stepResults.Last());
            }

            var call = _parser.Parse(currentInstruction);
            if (call == null) return $"Error: Invalid TOON syntax in step -> {step}";

            var definition = _registry.GetDefinition(call.FunctionName);
            if (definition == null) return $"Error: Plugin '{call.FunctionName}' not found.";

            try
            {
                var parameters = _parser.MapArguments(definition.Parameters, call.Arguments);
                var result = definition.Method.Invoke(definition.TargetInstance, parameters);

                string output = result is Task task
                    ? (await ResolveTaskResult(task))
                    : result?.ToString() ?? "success";

                stepResults.Add(output);
            }
            catch (Exception ex)
            {
                return $"Sequence failed at {call.FunctionName}: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        return stepResults.Last();
    }

    private string ResolveDataReferences(string instruction, string placeholder, string rawValue)
    {
        // If the value is a complex object serialized as (key:value,key2:value2)
        if (rawValue.StartsWith("(") && rawValue.EndsWith(")"))
        {
            var cleanValue = rawValue.Trim('(', ')');
            // Split by comma to get key:value pairs
            var pairs = cleanValue.Split(',')
                                  .Select(p => p.Split(':', 2))
                                  .Where(parts => parts.Length == 2)
                                  .ToDictionary(kv => kv[0].Trim(), kv => kv[1].Trim());

            // Look for $1.property patterns and replace them
            foreach (var key in pairs.Keys)
            {
                string propertyPattern = $"{placeholder}.{key}";
                if (instruction.Contains(propertyPattern))
                {
                    instruction = instruction.Replace(propertyPattern, pairs[key]);
                }
            }
        }

        // Standard replacement for the full value
        return instruction.Replace(placeholder, rawValue);
    }

    private async Task<string> ResolveTaskResult(Task task)
    {
        await task;
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task)?.ToString() ?? "done";
    }
}
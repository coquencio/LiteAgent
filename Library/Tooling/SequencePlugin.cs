using LiteAgent.Tooling;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace LiteAgent.Actions;

internal class SequencePlugin(LitePluginRegistry registry)
{
    private readonly LitePluginRegistry _registry = registry;
    private readonly PluginParser _parser = new();

    public async Task<string> ExecuteSequence(string sequence)
    {
        // Split by '|' only if it is NOT inside curly braces { }
        var steps = Regex.Split(sequence, @"\|(?![^{]*\})")
                         .Where(s => !string.IsNullOrWhiteSpace(s))
                         .ToList();

        var stepResults = new List<string>();
        var sequenceTrace = new StringBuilder();

        for (int i = 0; i < steps.Count; i++)
        {
            var currentInstruction = steps[i].Trim();
            int stepIndex = i + 1;

            // 1. Resolve indexed variables ($1, $1.prop, $LAST)
            for (int j = 0; j < stepResults.Count; j++)
            {
                string placeholder = $"${j + 1}";
                if (currentInstruction.Contains(placeholder))
                {
                    currentInstruction = ResolveDataReferences(currentInstruction, placeholder, stepResults[j]);
                }
            }

            if (stepResults.Any())
            {
                currentInstruction = currentInstruction.Replace("$LAST", stepResults.Last());
            }

            // 2. Parse TOON call
            var call = _parser.Parse(currentInstruction);
            if (call == null) return $"Error: Invalid TOON syntax -> {currentInstruction}";

            var definition = _registry.GetDefinition(call.FunctionName);
            if (definition == null) return $"Error: Plugin '{call.FunctionName}' not found.";

            try
            {
                // 3. Execution via Reflection
                var parameters = _parser.MapArguments(definition.Parameters, call.Arguments);
                var result = definition.Method.Invoke(definition.TargetInstance, parameters);

                // 4. Resolve async Task and Normalize output to TOON format
                object? rawOutput = result is Task task
                    ? await ResolveTaskResult(task)
                    : result;

                string output = NormalizeToToon(rawOutput);

                stepResults.Add(output);

                // 5. Append to trace: [#1: plugin_name -> (id:1,email:j@m.com)]
                sequenceTrace.Append($"[#{stepIndex}: {call.FunctionName} -> {output}] ");
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException?.Message ?? ex.Message;
                return $"Sequence failed at step #{stepIndex} ({call.FunctionName}): {errorMsg}";
            }
        }

        return sequenceTrace.ToString().Trim();
    }

    /// <summary>
    /// Converts any object into a TOON compliant string (key:value,key:value)
    /// </summary>
    private string NormalizeToToon(object? obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();

        // Return simple string for primitives, strings, or decimals
        if (type.IsPrimitive || obj is string || obj is decimal)
            return obj.ToString() ?? "success";

        // Reflect properties for complex objects to ensure $1.prop resolution works
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Select(p => {
                            var val = p.GetValue(obj)?.ToString() ?? "null";
                            return $"{p.Name.ToLower()}:{val}";
                        });

        return $"({string.Join(",", props)})";
    }

    private string ResolveDataReferences(string instruction, string placeholder, string rawValue)
    {
        // Now rawValue is guaranteed to be in (k:v,k:v) format if it was a complex object
        if (rawValue.StartsWith("(") && rawValue.EndsWith(")"))
        {
            var cleanValue = rawValue.Trim('(', ')');
            var pairs = cleanValue.Split(',')
                                  .Select(p => p.Split(':', 2))
                                  .Where(parts => parts.Length == 2)
                                  .ToDictionary(kv => kv[0].Trim(), kv => kv[1].Trim(), StringComparer.OrdinalIgnoreCase);

            foreach (var key in pairs.Keys)
            {
                string propertyPattern = $"{placeholder}.{key}";
                if (instruction.Contains(propertyPattern, StringComparison.OrdinalIgnoreCase))
                {
                    instruction = instruction.Replace(propertyPattern, pairs[key], StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // Final fallback for the full value replacement
        return instruction.Replace(placeholder, rawValue);
    }

    private async Task<object?> ResolveTaskResult(Task task)
    {
        await task;
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }
}
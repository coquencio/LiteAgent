using LiteAgent.Tooling;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace LiteAgent.Actions;

internal class SequencePlugin(ILogger<SequencePlugin>? logger)
{
    private readonly ILogger<SequencePlugin>? _logger = logger;
    private readonly PluginParser _parser = new();

    public async Task<string> ExecuteSequence(string sequence, LitePluginRegistry registry)
    {
        _logger?.LogInformation("Starting sequence execution: {Sequence}", sequence);

        // Split by '|' only if it is NOT inside curly braces { }
        var steps = Regex.Split(sequence, @"\|(?![^{]*\})")
                         .Where(s => !string.IsNullOrWhiteSpace(s))
                         .ToList();

        _logger?.LogDebug("Sequence split into {StepCount} discrete steps.", steps.Count);

        var stepResults = new List<string>();
        var sequenceTrace = new StringBuilder();

        for (int i = 0; i < steps.Count; i++)
        {
            var currentInstruction = steps[i].Trim();
            int stepIndex = i + 1;

            _logger?.LogTrace("Processing Step #{Index}: {Instruction}", stepIndex, currentInstruction);

            // 1. Resolve indexed variables ($1, $1.prop, $LAST)
            for (int j = 0; j < stepResults.Count; j++)
            {
                string placeholder = $"${j + 1}";
                if (currentInstruction.Contains(placeholder))
                {
                    _logger?.LogTrace("Resolving placeholder {Placeholder} using results from step {StepRef}", placeholder, j + 1);
                    currentInstruction = ResolveDataReferences(currentInstruction, placeholder, stepResults[j]);
                }
            }

            if (stepResults.Any() && currentInstruction.Contains("$LAST"))
            {
                _logger?.LogTrace("Resolving $LAST placeholder.");
                currentInstruction = currentInstruction.Replace("$LAST", stepResults.Last());
            }

            // 2. Parse TOON call
            var call = _parser.Parse(currentInstruction);
            if (call == null)
            {
                _logger?.LogError("Sequence aborted. Invalid TOON syntax at Step #{Index}: {Instruction}", stepIndex, currentInstruction);
                return $"Error: Invalid TOON syntax -> {currentInstruction}";
            }

            var definition = registry.GetDefinition(call.FunctionName);
            if (definition == null)
            {
                _logger?.LogError("Sequence aborted. Plugin '{FunctionName}' not found for Step #{Index}.", call.FunctionName, stepIndex);
                return $"Error: Plugin '{call.FunctionName}' not found.";
            }

            try
            {
                // 3. Execution via Compiled Lambda
                _logger?.LogDebug("Executing Step #{Index} ({FunctionName})...", stepIndex, call.FunctionName);
                var parameters = _parser.MapArguments(definition.Parameters, call.Arguments); var result = definition.Method.Invoke(definition.TargetInstance, parameters);

                // 4. Execute handler
                object? rawOutput = await definition.Handler!(parameters!);
                
                string output = NormalizeToToon(rawOutput);
                stepResults.Add(output);

                _logger?.LogTrace("Step #{Index} result: {Result}", stepIndex, output);

                // 5. Append to trace
                sequenceTrace.Append($"[#{stepIndex}: {call.FunctionName} -> {output}] ");
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException?.Message ?? ex.Message;
                _logger?.LogError(ex, "Sequence failed at Step #{Index} ({FunctionName}): {Error}", stepIndex, call.FunctionName, errorMsg);
                return $"Sequence failed at step #{stepIndex} ({call.FunctionName}): {errorMsg}";
            }
        }

        var finalTrace = sequenceTrace.ToString().Trim();
        _logger?.LogInformation("Sequence execution completed successfully.");
        return finalTrace;
    }

    private string NormalizeToToon(object? obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();

        if (type.IsPrimitive || obj is string || obj is decimal)
            return obj.ToString() ?? "success";

        _logger?.LogTrace("Normalizing complex object of type {Type} for sequence chaining.", type.Name);

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Select(p => {
                            var val = p.GetValue(obj)?.ToString() ?? "null";
                            return $"{p.Name.ToLower()}:{val}";
                        });

        return $"({string.Join(",", props)})";
    }

    private string ResolveDataReferences(string instruction, string placeholder, string rawValue)
    {
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
                    _logger?.LogTrace("Substituting property reference: {Pattern} -> {Value}", propertyPattern, pairs[key]);
                    instruction = instruction.Replace(propertyPattern, pairs[key], StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        return instruction.Replace(placeholder, rawValue);
    }

    private async Task<object?> ResolveTaskResult(Task task)
    {
        _logger?.LogTrace("Awaiting async task result for sequence step.");
        await task;
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }
}
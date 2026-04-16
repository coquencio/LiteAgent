using LiteAgent.Prompting;
using LiteAgent.Tooling;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Reflection;

namespace LiteAgent.Actions;

internal class LiteActions
{
    private readonly LitePluginRegistry _registry = new();
    private readonly PluginParser _parser = new();
    private readonly PromptGenerator _generator;
    private readonly SequencePlugin _orchestrator;
    private readonly ILogger<LiteActions>? _logger;

    // Tracks retry attempts per function name in the current context
    private readonly Dictionary<string, int> _retryTracker = new(StringComparer.OrdinalIgnoreCase);

    public LiteActions(ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<LiteActions>();
        _orchestrator = new SequencePlugin(loggerFactory?.CreateLogger<SequencePlugin>());
        _generator = new(_registry);

        _logger?.LogDebug("LiteActions initialized with TOON parser and retry tracking.");
    }

    internal void RegisterKit(object instance)
    {
        _logger?.LogTrace("Registering plugin kit for instance type: {Type}", instance.GetType().Name);
        _registry.RegisterPlugins(instance);
    }

    internal string GetSystemInstructions() => _generator.GetSystemPrompt();

    internal async Task<string> ExecuteMatchAsync(string llmResponse)
    {
        if (!_parser.IsToonCall(llmResponse))
        {
            _logger?.LogTrace("Response does not contain a TOON call. Skipping execution.");
            return llmResponse;
        }

        _logger?.LogInformation("TOON call detected. Attempting to parse: {RawResponse}", llmResponse);

        var call = _parser.Parse(llmResponse);
        if (call == null)
        {
            _logger?.LogWarning("Failed to parse TOON call syntax.");
            return "Error: Invalid TOON syntax";
        }

        // --- SPECIAL HANDLING FOR ORCHESTRATION ---
        if (call.FunctionName.Equals("execute_sequence", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var sequenceArg = call.Arguments.FirstOrDefault() ?? string.Empty;
                return await _orchestrator.ExecuteSequence(sequenceArg, _registry);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Sequence execution encountered an unhandled exception.");
                return $"Sequence execution error: {ex.Message}";
            }
        }

        var definition = _registry.GetDefinition(call.FunctionName);
        if (definition == null)
        {
            _logger?.LogWarning("Plugin '{FunctionName}' not found in registry.", call.FunctionName);
            return $"Error: plugin '{call.FunctionName}' not found";
        }

        try
        {
            _logger?.LogTrace("Mapping arguments for method: {MethodName}", definition.Method.Name);
            var parameters = _parser.MapArguments(definition.Parameters, call.Arguments);

            _logger?.LogInformation("Invoking plugin method: {MethodName}", definition.Method.Name);
            var result = definition.Method.Invoke(definition.TargetInstance, parameters);

            // If successful, reset retry counter for this function
            _retryTracker[call.FunctionName] = 0;

            if (result is Task task)
            {
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                var taskValue = resultProperty?.GetValue(task);
                return Serialize(taskValue) ?? "Task completed";
            }

            return Serialize(result);
        }
        catch (Exception ex)
        {
            var innerMsg = ex is TargetInvocationException tie ? tie.InnerException?.Message : ex.Message;
            _logger?.LogError("Execution failed for {MethodName}: {Error}", definition.Method.Name, innerMsg);

            // RETRY LOGIC
            _retryTracker.TryGetValue(call.FunctionName, out int currentAttempts);

            if (currentAttempts < definition.MaxRetries)
            {
                _retryTracker[call.FunctionName] = currentAttempts + 1;

                _logger?.LogWarning("Retry attempt {Count}/{Max} for {MethodName}. Notifying LLM.",
                    _retryTracker[call.FunctionName], definition.MaxRetries, call.FunctionName);

                // We return a prompt that encourages the LLM to fix the parameters
                return $"FIX_ATTEMPT: The tool '{call.FunctionName}' failed with error: '{innerMsg}'. " +
                       "Check your arguments (types, count, or format) and try one more time.";
            }

            _logger?.LogError("Max retries ({Max}) exceeded for {MethodName}. Returning final error.", definition.MaxRetries, call.FunctionName);
            return $"{definition.Method.Name} Execution Error: {innerMsg}";
        }
    }
    internal void ResetRetryCounters()
    {
        _logger?.LogTrace("Resetting retry counters for a new message cycle.");
        _retryTracker.Clear();
    }
    private string Serialize(object? obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();

        if (type.IsPrimitive || obj is string || obj is decimal || obj is DateTime)
        {
            return obj.ToString() ?? string.Empty;
        }

        if (obj is IEnumerable enumerable && obj is not string)
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
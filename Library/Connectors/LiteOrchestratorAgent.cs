using LiteAgent.Actions;
using LiteAgent.Constants;
using Microsoft.Extensions.Logging;

namespace LiteAgent.Connectors;

public class LiteOrchestratorAgent
{
    private readonly LiteActions _liteActions;
    private readonly ILiteClient _aiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<LiteMessage> _history = new();
    private string _customContext = string.Empty;
    private int _maxContextTokens = 128000;
    private int _maxTurns = 10; // Default fallback
    private readonly ILogger<LiteOrchestratorAgent>? _logger;

    public LiteOrchestratorAgent(ILiteClient aiClient, IServiceProvider serviceProvider, ILoggerFactory? loggerFactory)
    {
        _aiClient = aiClient;
        _serviceProvider = serviceProvider;
        _logger = loggerFactory?.CreateLogger<LiteOrchestratorAgent>();
        _liteActions = new LiteActions(loggerFactory);

        _logger?.LogInformation("LiteOrchestratorAgent initialized.");
    }

    internal LiteOrchestratorAgent WithConfiguration(int? maxTokens = default, float? temperature = default, int? maxContextTokens = default, int? maxTurns = default, params Type[] pluginTypes)
    {
        _logger?.LogDebug("Applying agent configuration...");
        try
        {
            if (maxTokens != null)
                _aiClient.SetMaxTokens(maxTokens.Value);

            if (temperature != null)
                _aiClient.SetTemperature(temperature.Value);

            if (pluginTypes != null && pluginTypes.Length > 0)
                RegisterTools(pluginTypes);

            if (maxContextTokens.HasValue)
            {
                _maxContextTokens = maxContextTokens.Value;
                _logger?.LogDebug("Max context tokens set to {MaxTokens}", _maxContextTokens);
            }

            if (maxTurns.HasValue)
            {
                _maxTurns = maxTurns.Value;
                _logger?.LogDebug("Max turns set to {MaxTurns}", _maxTurns);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying agent configuration.");
            throw;
        }

        return this;
    }

    internal void RegisterTools(params Type[] pluginTypes)
    {
        foreach (var type in pluginTypes)
        {
            _logger?.LogTrace("Resolving tool type: {TypeName}", type.Name);
            var instance = _serviceProvider.GetService(type);

            if (instance == null)
            {
                _logger?.LogError("Failed to resolve tool: {TypeName}", type.Name);
                throw new InvalidOperationException(
                    $"The tool '{type.Name}' could not be resolved from the Service Collection. " +
                    $"Make sure to register '{type.Name}' in your DI container (e.g., services.AddSingleton<{type.Name}>()) " +
                    $"before registering it in the agent.");
            }

            _liteActions.RegisterKit(instance);
            _logger?.LogDebug("Tool registered successfully: {TypeName}", type.Name);
        }
    }

    public void RegisterToolInstances(params object[] instances)
    {
        try
        {
            foreach (var instance in instances)
            {
                var typeName = instance.GetType().Name;
                _liteActions.RegisterKit(instance);
                _logger?.LogDebug("Manual tool instance registered: {TypeName}", typeName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error registering manual tool instances.");
            throw;
        }
    }

    public void Configure(float temperature = 0.7f, int maxTokens = 1000, int maxTurns = 10)
    {
        _logger?.LogDebug("Updating Agent config: Temp={Temp}, MaxTokens={Tokens}, MaxTurns={Turns}",
            temperature, maxTokens, maxTurns);

        _aiClient.SetTemperature(temperature);
        _aiClient.SetMaxTokens(maxTokens);
        _maxTurns = maxTurns;
    }

    public void AddContext(string context)
    {
        _logger?.LogTrace("Custom context appended.");
        _customContext += context + "\n";
    }

    public async Task<string> SendMessageAsync(string userMessage, bool stateless = true)
    {
        _liteActions.ResetRetryCounters();
        _logger?.LogInformation("Starting SendMessageAsync (Stateless: {Stateless})", stateless);

        var history = stateless ? new List<LiteMessage>() : _history;

        if (history.Count == 0)
        {
            _logger?.LogDebug("Initializing history with system instructions.");
            history.Add(new LiteMessage(Roles.System, _liteActions.GetSystemInstructions()));

            if (!string.IsNullOrWhiteSpace(_customContext))
                history.Add(new LiteMessage(Roles.System, "Without ignoring the previous instructions, " + _customContext));
        }

        history.Add(new LiteMessage(Roles.User, userMessage));
        PruneHistory(history);

        int turnCount = 0;

        while (turnCount < _maxTurns)
        {
            turnCount++;
            _logger?.LogDebug("Agentic Loop Turn #{Turn}", turnCount);

            string rawResponse = await _aiClient.GetCompletionAsync(history);
            _logger?.LogTrace("LLM Raw Response: {Response}", rawResponse);

            string executionResult = await _liteActions.ExecuteMatchAsync(rawResponse);

            if (executionResult == rawResponse)
            {
                _logger?.LogInformation("Final response reached after {Turn} turns.", turnCount);
                history.Add(new LiteMessage(Roles.Assistant, rawResponse));
                return rawResponse;
            }

            if (executionResult.StartsWith("FIX_ATTEMPT:"))
            {
                _logger?.LogWarning("Tool execution failed. Retrying turn to allow LLM to self-correct. Error: {Result}", executionResult);
            }
            else
            {
                _logger?.LogInformation("Tool executed. Result: {Result}", executionResult);
            }

            history.Add(new LiteMessage(Roles.Assistant, rawResponse));
            history.Add(new LiteMessage(Roles.User, $"TOOL_RESULT: {executionResult}"));

            PruneHistory(history);
        }

        _logger?.LogError("Max turns ({MaxTurns}) reached without a final response.", _maxTurns);
        return "Error: Maximum agentic turns reached without a conclusion.";
    }

    public async Task<string> SendMessageAsync(string userMessage, List<LiteMessage> externalHistory)
    {
        _liteActions.ResetRetryCounters();
        _logger?.LogInformation("Starting SendMessageAsync with External History.");

        EnsureSystemContext(externalHistory);
        externalHistory.Add(new LiteMessage(Roles.User, userMessage));
        PruneHistory(externalHistory);

        int turnCount = 0;

        while (turnCount < _maxTurns)
        {
            turnCount++;
            _logger?.LogDebug("Agentic Loop Turn #{Turn} (External History)", turnCount);

            string rawResponse = await _aiClient.GetCompletionAsync(externalHistory);
            _logger?.LogTrace("LLM Raw Response (External History): {Response}", rawResponse);

            string executionResult = await _liteActions.ExecuteMatchAsync(rawResponse);

            if (executionResult == rawResponse)
            {
                _logger?.LogInformation("Final response reached after {Turn} turns (External History).", turnCount);
                externalHistory.Add(new LiteMessage(Roles.Assistant, rawResponse));
                return rawResponse;
            }

            if (executionResult.StartsWith("FIX_ATTEMPT:"))
            {
                _logger?.LogWarning("Tool execution failed. Retrying turn to allow LLM to self-correct. Error: {Result}", executionResult);
            }
            else
            {
                _logger?.LogInformation("Tool executed (External History). Result: {Result}", executionResult);
            }

            externalHistory.Add(new LiteMessage(Roles.Assistant, rawResponse));
            externalHistory.Add(new LiteMessage(Roles.User, $"TOOL_RESULT: {executionResult}"));

            PruneHistory(externalHistory);
        }

        _logger?.LogError("Max turns ({MaxTurns}) reached in External History loop without a final response.", _maxTurns);
        return "Error: Maximum agentic turns reached without a conclusion.";
    }

    private void EnsureSystemContext(List<LiteMessage> history)
    {
        string systemInstructions = _liteActions.GetSystemInstructions();

        if (!history.Any(m => m.Role == Roles.System && m.Content.Contains(systemInstructions)))
        {
            _logger?.LogDebug("Injecting missing base system instructions.");
            history.Insert(0, new LiteMessage(Roles.System, systemInstructions));
        }

        if (!string.IsNullOrWhiteSpace(_customContext) &&
            !history.Any(m => m.Role == Roles.System && m.Content.Contains(_customContext)))
        {
            _logger?.LogDebug("Injecting custom context into history.");
            history.Add(new LiteMessage(Roles.System, "Without ignoring the previous instructions, " + _customContext));
        }
    }

    private void PruneHistory(List<LiteMessage> history)
    {
        var systemMessages = history.Where(m => m.Role == Roles.System).ToList();
        var conversationalMessages = history.Where(m => m.Role != Roles.System).ToList();

        int currentEstimate = EstimateTokens(history);
        if (currentEstimate > _maxContextTokens)
        {
            _logger?.LogWarning("History exceeds token limit ({Current} > {Max}). Pruning oldest messages...", currentEstimate, _maxContextTokens);

            while (EstimateTokens(history) > _maxContextTokens && conversationalMessages.Count > 1)
            {
                conversationalMessages.RemoveAt(0);
                history.Clear();
                history.AddRange(systemMessages);
                history.AddRange(conversationalMessages);
            }

            _logger?.LogDebug("Pruning complete. New estimate: {NewEstimate}", EstimateTokens(history));
        }
    }

    private int EstimateTokens(List<LiteMessage> messages)
    {
        return messages.Sum(m => m.Content.Length / 4);
    }
}
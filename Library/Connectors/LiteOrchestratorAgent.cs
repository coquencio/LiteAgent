using LiteAgent.Actions;
using LiteAgent.Constants;

namespace LiteAgent.Connectors;
public class LiteOrchestratorAgent
{
    private readonly LiteActions _liteActions;
    private readonly ILiteClient _aiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<LiteMessage> _history = new();
    private string _customContext = string.Empty;
    private int _maxContextTokens = 128000;

    public LiteOrchestratorAgent(ILiteClient aiClient, IServiceProvider serviceProvider)
    {
        _liteActions = new LiteActions();
        _aiClient = aiClient;
        _serviceProvider = serviceProvider;
    }

    internal LiteOrchestratorAgent WithConfiguration(int? maxTokens = default, float? temperature = default, int? maxContextTokens = default, params Type[] pluginTypes)
    {
        if (maxTokens != null)
            _aiClient.SetMaxTokens(maxTokens.Value);

        if (temperature != null)
            _aiClient.SetTemperature(temperature.Value);
        
        if (pluginTypes != null && pluginTypes.Length > 0)
            RegisterTools(pluginTypes);

        if (maxContextTokens.HasValue)
            _maxContextTokens = maxContextTokens.Value;

        return this;
    }


    /// <summary>
    /// Registers one or more LitePluginBase tool instances with the orchestrator.
    /// </summary>
    /// <param name="instances">The plugin/tool instances to register.</param>
    /// <summary>
    /// Registers one or more LitePluginBase tool types by resolving them from the Service Collection.
    /// </summary>
    /// <param name="pluginTypes">The types of the plugins to register.</param>
    internal void RegisterTools(params Type[] pluginTypes)
    {
        foreach (var type in pluginTypes)
        {
            // Resolvemos la instancia desde el contenedor
            var instance = _serviceProvider.GetService(type);

            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"The tool '{type.Name}' could not be resolved from the Service Collection. " +
                    $"Make sure to register '{type.Name}' in your DI container (e.g., services.AddSingleton<{type.Name}>()) " +
                    $"before registering it in the agent.");
            }

            _liteActions.RegisterKit(instance);
        }
    }

    /// <summary>
    /// Registers one or more LitePluginBase tool instances directly. Use this method when the tool instances
    /// are not managed by the service provider (DI container) but are created manually or elsewhere.
    /// </summary>
    /// <param name="instances">The plugin/tool instances to register.</param>
    public void RegisterToolInstances(params object[] instances)
    {
        foreach (var instance in instances)
        {
            _liteActions.RegisterKit(instance);
        }
    }

    /// <summary>
    /// Configures the AI client with the specified temperature and maximum token count.
    /// </summary>
    /// <param name="temperature">The temperature value for the AI model (default is 0.7).</param>
    /// <param name="maxTokens">The maximum number of tokens for the AI model (default is 1000).</param>
    public void Configure(float temperature = 0.7f, int maxTokens = 1000)
    {
        _aiClient.SetTemperature(temperature);
        _aiClient.SetMaxTokens(maxTokens);
    }

    /// <summary>
    /// Adds custom context information for the agent to use in its responses. This method is optional and can be used to provide additional instructions or background for the agent.
    /// </summary>
    /// <param name="context">The custom context string to append for the agent.</param>
    public void AddContext(string context)
    {
        _customContext += context + "\n";
    }
    /// <summary>
    /// Sends a message to the agent and returns the response. Optionally controls whether the agent maintains conversation history.
    /// </summary>
    /// <param name="userMessage">The message from the user to send to the agent.</param>
    /// <param name="stateless">If true, the agent will not remember previous messages after responding; if false, conversation history from current instance is preserved for future interactions.</param>
    /// <returns>The agent's response as a string.</returns>
    public async Task<string> SendMessageAsync(string userMessage, bool stateless = true)
    {
        var history = stateless ? new List<LiteMessage>() : _history;

        // 1. Initialize history with System Instructions if empty
        if (history.Count == 0)
        {
            history.Add(new LiteMessage(Roles.System, _liteActions.GetSystemInstructions()));
            
            if (!string.IsNullOrWhiteSpace(_customContext))
                history.Add(new LiteMessage(Roles.System, "Without ignoring the previous instructions, " + _customContext));
        }

        history.Add(new LiteMessage(Roles.User, userMessage));
        PruneHistory(history);

        // 2. Start the Agentic Loop
        while (true)
        {
            string rawResponse = await _aiClient.GetCompletionAsync(history);

            // 3. Try to execute a TOON tool
            string executionResult = await _liteActions.ExecuteMatchAsync(rawResponse);

            // If the result is the same as rawResponse, it's just text for the user
            if (executionResult == rawResponse)
            {
                history.Add(new LiteMessage(Roles.Assistant, rawResponse));

                return rawResponse;
            }

            // If it's different, a tool was executed. Feed the result back to the LLM.
            history.Add(new LiteMessage(Roles.Assistant, rawResponse));
            history.Add(new LiteMessage(Roles.User, $"TOOL_RESULT: {executionResult}"));

            // Loop again so the LLM can process the tool result
        }
    }
    /// <summary>
    /// Sends a message using an external history. The agent will inject system instructions 
    /// and custom context into this history before processing.
    /// </summary>
    /// <param name="userMessage">The message from the user.</param>
    /// <param name="externalHistory">A list of messages representing the conversation state.</param>
    /// <returns>The agent's response.</returns>
    public async Task<string> SendMessageAsync(string userMessage, List<LiteMessage> externalHistory)
    {
        // 1. Ensure system instructions and custom context are present in the provided history
        EnsureSystemContext(externalHistory);

        // 2. Add the new user message
        externalHistory.Add(new LiteMessage(Roles.User, userMessage));

        // 3. Prune history based on max context window
        PruneHistory(externalHistory);

        // 4. Start the Agentic Loop
        while (true)
        {
            string rawResponse = await _aiClient.GetCompletionAsync(externalHistory);

            // 5. Try to execute a match
            string executionResult = await _liteActions.ExecuteMatchAsync(rawResponse);

            // If it's pure text, append to history and return
            if (executionResult == rawResponse)
            {
                externalHistory.Add(new LiteMessage(Roles.Assistant, rawResponse));
                return rawResponse;
            }

            // If a tool was triggered, record the tool call and the result
            externalHistory.Add(new LiteMessage(Roles.Assistant, rawResponse));
            externalHistory.Add(new LiteMessage(Roles.User, $"TOOL_RESULT: {executionResult}"));

            // Prune again if the tool result was too large
            PruneHistory(externalHistory);
        }
    }

    /// <summary>
    /// Helper to inject system instructions and custom context if they are missing.
    /// </summary>
    private void EnsureSystemContext(List<LiteMessage> history)
    {
        // Get base instructions from the registered tools
        string systemInstructions = _liteActions.GetSystemInstructions();

        // Check if instructions are already present to avoid duplicates
        if (!history.Any(m => m.Role == Roles.System && m.Content.Contains(systemInstructions)))
        {
            history.Insert(0, new LiteMessage(Roles.System, systemInstructions));
        }

        // Inject custom context if provided and not already in history
        if (!string.IsNullOrWhiteSpace(_customContext) &&
            !history.Any(m => m.Role == Roles.System && m.Content.Contains(_customContext)))
        {
            history.Add(new LiteMessage(Roles.System, "Without ignoring the previous instructions, " + _customContext));
        }
    }

    // Update PruneHistory to accept a specific list
    private void PruneHistory(List<LiteMessage> history)
    {
        var systemMessages = history.Where(m => m.Role == Roles.System).ToList();
        var conversationalMessages = history.Where(m => m.Role != Roles.System).ToList();

        while (EstimateTokens(history) > _maxContextTokens && conversationalMessages.Count > 1)
        {
            conversationalMessages.RemoveAt(0);

            history.Clear();
            history.AddRange(systemMessages);
            history.AddRange(conversationalMessages);
        }
    }

    private int EstimateTokens(List<LiteMessage> messages)
    {
        return messages.Sum(m => m.Content.Length / 4);
    }
}


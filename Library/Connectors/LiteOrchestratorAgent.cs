using LiteAgent.Actions;
using LiteAgent.Constants;

namespace LiteAgent.Connectors;
public class LiteOrchestratorAgent
{
    private readonly LiteActions _orchestrator;
    private readonly ILiteClient _aiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<LiteMessage> _history = new();
    private string _customContext = string.Empty;

    public LiteOrchestratorAgent(ILiteClient aiClient, IServiceProvider serviceProvider)
    {
        _orchestrator = new LiteActions();
        _aiClient = aiClient;
        _serviceProvider = serviceProvider;
    }

    internal LiteOrchestratorAgent WithConfiguration(int? maxTokens = default, float? temperature = default, params Type[] pluginTypes)
    {
        if (maxTokens != null)
            _aiClient.SetMaxTokens(maxTokens.Value);

        if (temperature != null)
            _aiClient.SetTemperature(temperature.Value);
        
        if (pluginTypes != null && pluginTypes.Length > 0)
            RegisterTools(pluginTypes);
        
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

            _orchestrator.RegisterKit(instance);
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
            _orchestrator.RegisterKit(instance);
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
    /// <param name="stateless">If true, the agent will not remember previous messages after responding; if false, conversation history is preserved for future interactions.</param>
    /// <returns>The agent's response as a string.</returns>
    public async Task<string> SendMessageAsync(string userMessage, bool stateless = true)
    {
        // 1. Initialize history with System Instructions if empty
        if (_history.Count == 0)
        {
            _history.Add(new LiteMessage(Roles.System, _orchestrator.GetSystemInstructions()));
            
            if (!string.IsNullOrWhiteSpace(_customContext))
                _history.Add(new LiteMessage(Roles.System, "Without ignoring the previous instructions, " + _customContext));
        }

        _history.Add(new LiteMessage(Roles.User, userMessage));

        // 2. Start the Agentic Loop
        while (true)
        {
            string rawResponse = await _aiClient.GetCompletionAsync(_history);

            // 3. Try to execute a TOON tool
            string executionResult = await _orchestrator.ExecuteMatchAsync(rawResponse);

            // If the result is the same as rawResponse, it's just text for the user
            if (executionResult == rawResponse)
            {
                _history.Add(new LiteMessage(Roles.Assistant, rawResponse));
                
                if (stateless)
                    _history.Clear();

                return rawResponse;
            }

            // If it's different, a tool was executed. Feed the result back to the LLM.
            _history.Add(new LiteMessage(Roles.Assistant, rawResponse));
            _history.Add(new LiteMessage(Roles.User, $"TOOL_RESULT: {executionResult}"));

            // Loop again so the LLM can process the tool result
        }
    }
}


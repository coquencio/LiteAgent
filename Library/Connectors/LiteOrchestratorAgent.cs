using LiteAgent.Actions;
using LiteAgent.Constants;
using LiteAgent.Tooling;

namespace LiteAgent.Connectors;
public class LiteOrchestratorAgent
{
    private readonly LiteActions _orchestrator;
    private readonly ILiteClient _aiClient;
    private readonly List<LiteMessage> _history = new();

    public LiteOrchestratorAgent(ILiteClient aiClient)
    {
        _orchestrator = new LiteActions();
        _aiClient = aiClient;
    }

    internal LiteOrchestratorAgent WithConfiguration(int? maxTokens = default, float? temperature = default, params LitePluginBase[] instances)
    {
        if (maxTokens != null)
            _aiClient.SetMaxTokens(maxTokens.Value);

        if (temperature != null)
            _aiClient.SetTemperature(temperature.Value);
        
        if (instances != null && instances.Length > 0)
            RegisterTools(instances);
        
        return this;
    }


    /// <summary>
    /// Registers one or more LitePluginBase tool instances with the orchestrator.
    /// </summary>
    /// <param name="instances">The plugin/tool instances to register.</param>
    public void RegisterTools(params LitePluginBase[] instances) =>
        _orchestrator.RegisterKit(instances);


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

    public async Task<string> SendMessageAsync(string userMessage)
    {
        // 1. Initialize history with System Instructions if empty
        if (_history.Count == 0)
        {
            _history.Add(new LiteMessage(Roles.System, _orchestrator.GetSystemInstructions()));
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
                return rawResponse;
            }

            // If it's different, a tool was executed. Feed the result back to the LLM.
            _history.Add(new LiteMessage(Roles.Assistant, rawResponse));
            _history.Add(new LiteMessage(Roles.User, $"TOOL_RESULT: {executionResult}"));

            // Loop again so the LLM can process the tool result
        }
    }
}


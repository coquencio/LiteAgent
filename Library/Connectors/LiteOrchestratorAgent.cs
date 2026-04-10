using LiteAgent.Actions;
using LiteAgent.Constants;

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

    public void RegisterTools<T>(T instance) where T : class =>
        _orchestrator.RegisterKit(instance);

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


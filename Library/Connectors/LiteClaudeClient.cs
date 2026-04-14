using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using LiteAgent.Constants;

namespace LiteAgent.Connectors;

public class LiteClaudeClient : ILiteClient
{
    private readonly AnthropicClient _client;
    private readonly string _modelName;
    private int _maxTokens = 1000;
    private float _temperature = 0.7f;

    public LiteClaudeClient(string apiKey, string modelName = "claude-3-5-sonnet-20240620")
    {
        _client = new AnthropicClient(apiKey);
        _modelName = modelName;
    }

    public async Task<string> GetCompletionAsync(List<LiteMessage> history)
    {
        var systemInstructions = history
            .Where(m => m.Role == Roles.System)
            .Select(m => m.Content)
            .ToList();

        string fullSystemPrompt = string.Join("\n", systemInstructions);

        // 2. Mapear mensajes (User y Assistant solamente)
        var messages = history
            .Where(m => m.Role != Roles.System)
            .Select(m => new Message
            {
                Role = m.Role == Roles.Assistant ? RoleType.Assistant : RoleType.User,
                Content = new List<ContentBase> { new Anthropic.SDK.Messaging.TextContent { Text = m.Content } }
            })
            .ToList();

        var request = new MessageParameters
        {
            Model = _modelName,
            Messages = messages,
            System = !string.IsNullOrEmpty(fullSystemPrompt)
                ? new List<SystemMessage> { new SystemMessage(fullSystemPrompt) }
                : null,
            MaxTokens = _maxTokens,
            Temperature = (decimal)_temperature
        };

        var response = await _client.Messages.GetClaudeMessageAsync(request);

        return response.Content.OfType<Anthropic.SDK.Messaging.TextContent>().FirstOrDefault()?.Text ?? string.Empty;
    }

    public void SetMaxTokens(int maxTokens) => _maxTokens = maxTokens;

    public void SetTemperature(float temperature) => _temperature = temperature;
}
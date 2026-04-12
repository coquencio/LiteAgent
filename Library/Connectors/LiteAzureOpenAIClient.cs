using LiteAgent.Constants;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace LiteAgent.Connectors;

public class LiteAzureOpenAIClient : ILiteClient
{
    private ChatClient _chatClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _deployment;
    private int _maxTokens = 1000;
    private float _temperature = 0.7f;
    public LiteAzureOpenAIClient(string apiKey, string deployment, string endpoint)
    {
        _apiKey = apiKey;
        _deployment = deployment;
        _endpoint = endpoint;
        _chatClient = GetChatClient();
    }
    public async Task<string> GetCompletionAsync(List<LiteMessage> history)
    {
        var messages = new List<ChatMessage>();
        foreach (var h in history)
        {
            if (h.Role == Roles.System) messages.Add(new SystemChatMessage(h.Content));
            else if (h.Role == Roles.Assistant) messages.Add(new AssistantChatMessage(h.Content));
            else messages.Add(new UserChatMessage(h.Content));
        }
        
        var options = new ChatCompletionOptions()
        {
            Temperature = _temperature,
            MaxOutputTokenCount = _maxTokens
        };

        var response = await _chatClient.CompleteChatAsync(messages.ToArray(), options);

        return response.Value.Content[0].Text;
    }

    public void SetMaxTokens(int maxTokens) =>
        _maxTokens = maxTokens;

    public void SetTemperature(float temperature) =>
        _temperature = temperature;

    private ChatClient GetChatClient() => 
        new(
            credential: new ApiKeyCredential(_apiKey),
            model: _deployment,
            options: new OpenAIClientOptions()
            {
                Endpoint = new($"{_endpoint}"),
            }
        );
}


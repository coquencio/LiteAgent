using Azure;
using Azure.AI.OpenAI;
using LiteAgent.Constants;
using OpenAI.Chat;

namespace LiteAgent.Connectors;

public class LiteAzureOpenAIClient : ILiteClient
{
    private AzureOpenAIClient _chatClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _model;
    private int _maxTokens;
    private float _temperature;
    public LiteAzureOpenAIClient(string apiKey, string model, string endpoint)
    {
        _apiKey = apiKey;
        _model = model;
        _endpoint = endpoint;
        Setup();
    }
    public async Task<string> GetCompletionAsync(List<LiteMessage> history)
    {
        var messages = new List<ChatMessage>();
        history.ForEach(h =>       
                messages.Add(h.Role.Equals(Roles.Assistant)?
                    new SystemChatMessage(h.Content) :
                    new UserChatMessage(h.Content))
                );

        var client = _chatClient.GetChatClient(_model);

        var options = new ChatCompletionOptions()
        {
            Temperature = _temperature,
            MaxOutputTokenCount = _maxTokens
        };

        var response = await client.CompleteChatAsync(messages.ToArray(), options);

        return response.Value.Content[0].Text;
    }

    public void SetMaxTokens(int maxTokens) =>
        _maxTokens = maxTokens;

    public void SetTemperature(float temperature) =>
        _temperature = temperature;

    public void Setup()
    {
        _chatClient = new AzureOpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
    }
}


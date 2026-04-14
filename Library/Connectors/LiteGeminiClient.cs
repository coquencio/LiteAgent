using Google.GenAI;
using Google.GenAI.Types;
using LiteAgent.Connectors;
using LiteAgent.Constants;

public class LiteGeminiClient : ILiteClient
{
    private readonly Client _client;
    private readonly string _modelName;
    private int _maxTokens = 1000;
    private float _temperature = 0.7f;

    public LiteGeminiClient(string apiKey, string modelName = "gemini-1.5-flash")
    {
        _client = new Client(apiKey: apiKey);
        _modelName = modelName;
    }

    public async Task<string> GetCompletionAsync(List<LiteMessage> history)
    {
        var systemInstruction = string.Join("\n", history
            .Where(m => m.Role == Roles.System)
            .Select(m => m.Content));

        var contents = history
            .Where(m => m.Role != Roles.System)
            .Select(m => new Content
            {
                Role = m.Role == Roles.Assistant ? "model" : "user",
                Parts = new List<Part> { new Part { Text = m.Content } }
            }).ToList();

        var config = new GenerateContentConfig
        {
            Temperature = _temperature,
            MaxOutputTokens = _maxTokens,
            SystemInstruction = new Content
            {
                Parts = new List<Part> { new Part { Text = systemInstruction } }
            }
        };

        var response = await _client.Models.GenerateContentAsync(_modelName, contents, config);

        return response.Text ?? string.Empty;
    }

    public void SetMaxTokens(int maxTokens) => _maxTokens = maxTokens;
    public void SetTemperature(float temperature) => _temperature = temperature;
}
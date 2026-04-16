namespace LiteAgent.Connectors;
public interface ILiteClient
{
    void SetMaxTokens(int maxTokens);
    void SetTemperature(float temperature);
    Task<LiteResponse> GetCompletionAsync(List<LiteMessage> history);
}

public record LiteMessage(string Role, string Content);
public record LiteUsage(int PromptTokens, int CompletionTokens, int TotalTokens);

public record LiteResponse(string Content, LiteUsage Usage);
namespace LiteAgent.Connectors;
public interface ILiteClient
{
    void Setup();
    void SetMaxTokens(int maxTokens);
    void SetTemperature(float temperature);
    Task<string> GetCompletionAsync(List<LiteMessage> history);
}

public record LiteMessage(string Role, string Content);
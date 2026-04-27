namespace LiteAgent.Connectors;
public interface ILiteClient
{
    /// <summary>
    /// Sets the maximum number of tokens for the AI model response.
    /// </summary>
    /// <param name="maxTokens">The maximum token count for generated responses.</param>
    void SetMaxTokens(int maxTokens);

    /// <summary>
    /// Sets the temperature parameter for the AI model, controlling response randomness.
    /// </summary>
    /// <param name="temperature">A value between 0 and 2 (typically), where lower values make output more focused and deterministic, and higher values make it more random.</param>
    void SetTemperature(float temperature);

    /// <summary>
    /// Gets a completion response from the AI model based on the conversation history.
    /// </summary>
    /// <param name="history">A list of messages representing the conversation history.</param>
    /// <returns>A LiteResponse containing the model's response content and token usage information.</returns>
    Task<LiteResponse> GetCompletionAsync(List<LiteMessage> history);
}

public record LiteMessage(string Role, string Content);
public record LiteUsage(int PromptTokens, int CompletionTokens, int TotalTokens);

public record LiteResponse(string Content, LiteUsage Usage);
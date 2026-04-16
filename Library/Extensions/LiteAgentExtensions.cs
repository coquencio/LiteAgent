using LiteAgent.Actions;
using LiteAgent.Connectors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LiteAgent.Extensions;

/// <summary>
/// Extension methods for setting up LiteAgent services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class LiteAgentExtensions
{
    /// <summary>
    /// Registers a LiteOrchestratorAgent and allows adding plugins via a generic fluent configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration delegate to set up plugins and agent parameters.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, Action<LiteAgentConfigurationBuilder>? configure = null)
    {
        var builder = new LiteAgentConfigurationBuilder();
        configure?.Invoke(builder);

        // Internal actions handler
        services.AddTransient<LiteActions>();

        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            var loggerFactory = sp.GetService<ILoggerFactory>();

            var agent = new LiteOrchestratorAgent(client, sp, loggerFactory);

            return agent.WithConfiguration(
                builder.MaxTokens,
                builder.Temperature,
                builder.MaxContextTokens,
                builder.MaxTurns,
                builder.PluginTypes.ToArray()
            );
        });
    }

    /// <summary>
    /// Helper class to provide a clean, generic-based configuration syntax for the LiteAgent.
    /// </summary>
    public class LiteAgentConfigurationBuilder
    {
        public List<Type> PluginTypes { get; } = new();
        public int MaxTokens { get; private set; } = 1000;
        public int MaxContextTokens { get; private set; } = 128000;
        public int MaxTurns { get; private set; } = 10;
        public float Temperature { get; private set; } = 0.7f;

        /// <summary>
        /// Adds a plugin type to the agent's registry using generic syntax. 
        /// The type will be resolved from the DI container at runtime.
        /// </summary>
        /// <typeparam name="T">The class containing methods decorated with [LitePlugin].</typeparam>
        public LiteAgentConfigurationBuilder AddPlugin<T>() where T : class
        {
            PluginTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Sets the maximum number of tokens for the LLM completion response.
        /// </summary>
        /// <param name="tokens">Token limit (default 1000).</param>
        public LiteAgentConfigurationBuilder SetMaxTokens(int tokens)
        {
            MaxTokens = tokens;
            return this;
        }

        /// <summary>
        /// Sets the sampling temperature for the LLM. 
        /// Higher values make the output more random, lower values more deterministic.
        /// </summary>
        /// <param name="temp">Temperature value between 0 and 2 (default 0.7).</param>
        public LiteAgentConfigurationBuilder SetTemperature(float temp)
        {
            Temperature = temp;
            return this;
        }

        /// <summary>
        /// Sets the maximum context window size allowed before the agent starts pruning history.
        /// </summary>
        /// <param name="tokens">Maximum tokens allowed in the conversation history.</param>
        public LiteAgentConfigurationBuilder SetMaxContextTokens(int tokens)
        {
            MaxContextTokens = tokens;
            return this;
        }

        /// <summary>
        /// Sets the safety limit for the agentic loop. 
        /// Prevents the agent from executing too many tool calls or turns in a single message cycle.
        /// </summary>
        /// <param name="turns">Maximum number of turns (default 10).</param>
        public LiteAgentConfigurationBuilder SetMaxTurns(int turns)
        {
            MaxTurns = turns;
            return this;
        }
    }

    /// <summary>
    /// Registers a LiteAzureOpenAIClient as a singleton ILiteClient use this for azure Open AI models.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The Azure OpenAI API Key.</param>
    /// <param name="deploymentName">The name of the model deployment (e.g., "gpt-4o").</param>
    /// <param name="endpoint">The Azure OpenAI resource endpoint URL.</param>
    public static IServiceCollection AddAzureOpenAILiteClient(this IServiceCollection services, string apiKey, string deploymentName, string endpoint)
    {
        return services.AddSingleton<ILiteClient>(sp =>
            new LiteAzureOpenAIClient(apiKey, deploymentName, endpoint));
    }
    /// <summary>
    /// Registers a LiteGeminiClient as a singleton ILiteClient using the official Google GenAI SDK.
    /// </summary>
    public static IServiceCollection AddGeminiLiteClient(this IServiceCollection services, string apiKey, string modelName = "gemini-1.5-flash")
    {
        return services.AddSingleton<ILiteClient>(sp =>
            new LiteGeminiClient(apiKey, modelName));
    }

    /// <summary>
    /// Registers a LiteClaudeClient as a singleton ILiteClient using the Anthropic SDK.
    /// </summary>
    public static IServiceCollection AddClaudeLiteClient(this IServiceCollection services, string apiKey, string modelName = "claude-3-5-sonnet-latest")
    {
        return services.AddSingleton<ILiteClient>(sp =>
            new LiteClaudeClient(apiKey, modelName));
    }

    /// <summary>
    /// Registers a LiteGenericOpenAIClient as a singleton ILiteClient. 
    /// Use this for Ollama, Groq, DeepSeek, or any OpenAI-compatible provider.
    /// </summary>
    public static IServiceCollection AddGenericOpenAILiteClient(this IServiceCollection services, string apiKey, string modelId, string endpoint = "https://api.openai.com/v1")
    {
        return services.AddSingleton<ILiteClient>(sp =>
            new LiteGenericOpenAIClient(apiKey, modelId, endpoint));
    }
}
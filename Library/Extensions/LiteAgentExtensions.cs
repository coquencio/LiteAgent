using LiteAgent.Connectors;
using Microsoft.Extensions.DependencyInjection;

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
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, Action<LiteAgentConfigurationBuilder>? configure = null)
    {
        var builder = new LiteAgentConfigurationBuilder();
        configure?.Invoke(builder);

        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            var agent = new LiteOrchestratorAgent(client, sp);

            return agent.WithConfiguration(
                builder.MaxTokens,
                builder.Temperature,
                builder.PluginTypes.ToArray()
            );
        });
    }

    /// <summary>
    /// Helper class to provide a clean, generic-based configuration syntax.
    /// </summary>
    public class LiteAgentConfigurationBuilder
    {
        public List<Type> PluginTypes { get; } = new();
        public int MaxTokens { get; private set; } = 1000;
        public float Temperature { get; private set; } = 0.7f;

        /// <summary>
        /// Adds a plugin type to the agent using generic syntax.
        /// </summary>
        /// <typeparam name="T">The plugin class inheriting from LitePluginBase.</typeparam>
        public LiteAgentConfigurationBuilder AddPlugin<T>() where T : class
        {
            PluginTypes.Add(typeof(T));
            return this;
        }

        public LiteAgentConfigurationBuilder SetMaxTokens(int tokens)
        {
            MaxTokens = tokens;
            return this;
        }

        public LiteAgentConfigurationBuilder SetTemperature(float temp)
        {
            Temperature = temp;
            return this;
        }
    }

    /// <summary>
    /// Registers a LiteAzureOpenAIClient as a singleton ILiteClient.
    /// </summary>
    public static IServiceCollection AddAzureOpenAILiteClient(this IServiceCollection services, string apiKey, string deploymentName, string endpoint)
    {
        return services.AddSingleton<ILiteClient>(sp =>
            new LiteAzureOpenAIClient(apiKey, deploymentName, endpoint));
    }
}
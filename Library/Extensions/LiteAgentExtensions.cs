using LiteAgent.Connectors;
using LiteAgent.Tooling;
using Microsoft.Extensions.DependencyInjection;

namespace LiteAgent.Extensions;
public static class LiteAgentExtensions
{
    /// <summary>
    /// Registers a LiteOrchestratorAgent with default configuration in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the agent to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services)
    {
        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            return new LiteOrchestratorAgent(client);
        });
    }

    /// <summary>
    /// Registers a LiteOrchestratorAgent with a specified temperature in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the agent to.</param>
    /// <param name="temp">The temperature value for the agent configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, float temp)
    {
        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            return new LiteOrchestratorAgent(client).WithConfiguration(temperature: temp, instances: []);
        });
    }

    /// <summary>
    /// Registers a LiteOrchestratorAgent with a specified maximum token count in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the agent to.</param>
    /// <param name="maxTokens">The maximum number of tokens for the agent configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, int maxTokens)
    {
        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            return new LiteOrchestratorAgent(client).WithConfiguration(maxTokens, instances: []);
        });
    }
    /// <summary>
    /// Registers a LiteOrchestratorAgent with specified temperature and maximum token count in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the agent to.</param>
    /// <param name="temp">The temperature value for the agent configuration.</param>
    /// <param name="maxTokens">The maximum number of tokens for the agent configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, float temp, int maxTokens)
    {
        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            return new LiteOrchestratorAgent(client).WithConfiguration(maxTokens, temp, []);
        });
    }
    /// <summary>
    /// Registers a LiteOrchestratorAgent with specified temperature, maximum token count, and plugin instances in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the agent to.</param>
    /// <param name="temp">The temperature value for the agent configuration.</param>
    /// <param name="maxTokens">The maximum number of tokens for the agent configuration.</param>
    /// <param name="pluginInstances">The plugin instances to use with the agent.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, float temp, int maxTokens, params LitePluginBase[] pluginInstances)
    {
        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            return new LiteOrchestratorAgent(client).WithConfiguration(maxTokens, temp, pluginInstances);
        });
    }

    /// <summary>
    /// Registers a LiteOrchestratorAgent with specified plugin instances in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the agent to.</param>
    /// <param name="pluginInstances">The plugin instances to use with the agent.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, params LitePluginBase[] pluginInstances)
    {
        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            return new LiteOrchestratorAgent(client).WithConfiguration(instances: pluginInstances);
        });
    }

    /// <summary>
    /// Registers a LiteOrchestratorAgent with specified maximum token count and plugin instances in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the agent to.</param>
    /// <param name="maxTokens">The maximum number of tokens for the agent configuration.</param>
    /// <param name="pluginInstances">The plugin instances to use with the agent.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, int maxTokens, params LitePluginBase[] pluginInstances)
    {
        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            return new LiteOrchestratorAgent(client).WithConfiguration(maxTokens, instances: pluginInstances);
        });
    }
    /// <summary>
    /// Registers a LiteOrchestratorAgent with specified temperature and plugin instances in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the agent to.</param>
    /// <param name="temp">The temperature value for the agent configuration.</param>
    /// <param name="pluginInstances">The plugin instances to use with the agent.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLiteAgent(this IServiceCollection services, float temp, params LitePluginBase[] pluginInstances)
    {
        return services.AddTransient(sp =>
        {
            var client = sp.GetRequiredService<ILiteClient>();
            return new LiteOrchestratorAgent(client).WithConfiguration(temperature: temp, instances: pluginInstances);
        });
    }

    /// <summary>
    /// Registers a LiteAzureOpenAIClient as a singleton ILiteClient in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="apiKey">The API key for Azure OpenAI.</param>
    /// <param name="deploymentName">The deployment name for Azure OpenAI.</param>
    /// <param name="endpoint">The endpoint URL for Azure OpenAI.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAzureOpenAILiteClient(this IServiceCollection services, string apiKey, string deploymentName, string endpoint)
    {
        return services.AddSingleton<ILiteClient>(sp =>
        new LiteAzureOpenAIClient(
            apiKey: apiKey,
            deployment: deploymentName,
            endpoint: endpoint
        ));
    }


}


using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LitePlugin : Attribute
{
    /// <summary>
    /// Gets the description of the method, which helps the LLM understand when and how to use this tool.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the maximum number of times the library will allow the agent to re-invoke this method if an error occurs.
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Decorates a method to be discoverable by LiteAgent as a tool.
    /// </summary>
    /// <param name="description">A clear explanation of what the method does for the AI's context.</param>
    /// <param name="maxRetries">The limit of retry attempts if the method execution fails during the agentic loop.</param>
    public LitePlugin(string? description = null, int maxRetries = 2)
    {
        Description = description;
        MaxRetries = maxRetries;
    }
}
namespace LiteAgent.Tooling;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LitePlugin(string? description = null) : Attribute
{
    public string? Description { get; } = description;
}

public abstract class LitePluginBase
{
    // This class can be extended in the future for shared plugin functionality
}
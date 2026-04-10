namespace LiteAgent.Tooling;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LitePlugin(string? description = null) : Attribute
{
    public string? Description { get; } = description;
}


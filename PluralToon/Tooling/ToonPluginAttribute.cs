namespace ToonPlugin.Tooling;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ToonPluginAttribute(string? description = null) : Attribute
{
    public string? Description { get; } = description;
}


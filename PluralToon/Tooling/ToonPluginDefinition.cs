using System.Reflection;


namespace ToonPlugin.Tooling;
internal class ToonPluginDefinition
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MethodInfo Method { get; set; } = null!;
    public object TargetInstance { get; set; } = null!;
    public ParameterInfo[] Parameters { get; set; } = Array.Empty<ParameterInfo>();

    public string ToSignature()
    {
        var paramsStr = string.Join(",", Parameters.Select(p => p.Name));
        var desc = string.IsNullOrEmpty(Description) ? "" : $" - {Description}";
        return $"{Name}{{{paramsStr}}}{desc}";
    }
}


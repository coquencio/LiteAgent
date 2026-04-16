using System.Reflection;

namespace LiteAgent.Tooling;

internal class LitePluginDefinition
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MethodInfo Method { get; set; } = null!;
    public object TargetInstance { get; set; } = null!;
    public ParameterInfo[] Parameters { get; set; } = Array.Empty<ParameterInfo>();
    public int MaxRetries { get; set; } = 0;
    public string ToSignature()
    {
        var returnType = Method.ReturnType;

        // Unwrapping Task<T>
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        // Getting the robust type representation
        var returnTypeDescriptor = GetTypeDescriptor(returnType);

        // Changed comma to pipe to match the new TOON argument delimiter
        var paramsStr = string.Join("|", Parameters.Select(p => $"<{p.Name}>"));
        var desc = string.IsNullOrEmpty(Description) ? "" : $" - {Description}";

        // We use Name as is (usually the Method name) to avoid injecting underscores
        return $"{returnTypeDescriptor}:{Name}{{{paramsStr}}}{desc}";
    }

    private string GetTypeDescriptor(Type type)
    {
        // 1. Basic Types
        if (type == typeof(void)) return "void";
        if (type == typeof(string)) return "string";
        if (type.IsPrimitive || type == typeof(decimal)) return type.Name.ToLower();

        // 2. Arrays or Collections: [type]
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var elementType = type.IsArray
                ? type.GetElementType()
                : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            return $"[{GetTypeDescriptor(elementType)}]";
        }

        // 3. Complex Objects: (prop1:type,prop2:type)
        var props = type.GetProperties(BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                        .Select(p => $"{p.Name.ToLower()}:{GetTypeDescriptor(p.PropertyType)}");

        return $"({string.Join(",", props)})";
    }
}
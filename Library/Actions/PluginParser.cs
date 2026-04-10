using System.Reflection;
using System.Text.RegularExpressions;

namespace LiteAgent.Actions;

internal record PluginCall(string FunctionName, string[] Arguments);
internal class PluginParser
{
    private static readonly Regex ToonRegex = new(@"^(\w+)\{(.*)\}$|(\w+)\{\}", RegexOptions.Compiled);
    public bool IsToonCall(string input) => ToonRegex.IsMatch(input);
    public PluginCall? Parse(string input)
    {
        var match = ToonRegex.Match(input.Trim());
        if (!match.Success) return null;

        string functionName = (match.Groups[1].Value + match.Groups[3].Value).ToLower();
        string argsRaw = match.Groups[2].Value;

        string[] args = string.IsNullOrWhiteSpace(argsRaw)
            ? Array.Empty<string>()
            : argsRaw.Split(',').Select(a => a.Trim()).ToArray();

        return new PluginCall(functionName, args);
    }
    public object?[] MapArguments(ParameterInfo[] targetParameters, string[] providedArgs)
    {
        var mapped = new object?[targetParameters.Length];

        for (int i = 0; i < targetParameters.Length; i++)
        {
            var param = targetParameters[i];

            if (i < providedArgs.Length)
            {
                mapped[i] = Convert.ChangeType(providedArgs[i], param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                mapped[i] = param.DefaultValue;
            }
            else
            {
                mapped[i] = null;
            }
        }
        return mapped;
    }
}


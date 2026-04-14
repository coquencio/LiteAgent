using System.Reflection;
using System.Text.RegularExpressions;

namespace LiteAgent.Actions;

internal record PluginCall(string FunctionName, string[] Arguments);

internal class PluginParser
{
    // Regex matches: name{args} or name{}
    private static readonly Regex ToonRegex = new(@"^(\w+)\{(.*)\}$|(\w+)\{\}", RegexOptions.Compiled);

    public bool IsToonCall(string input) => ToonRegex.IsMatch(input);

    public PluginCall? Parse(string input)
    {
        var match = ToonRegex.Match(input.Trim());
        if (!match.Success) return null;

        // We keep the name as is, or use the snake_case logic we discussed
        string functionName = (match.Groups[1].Value + match.Groups[3].Value);
        string argsRaw = match.Groups[2].Value;

        string[] args;

        // If the function is a sequence, we must NOT split the internal pipes yet.
        // We treat the entire content as a single 'sequence' argument.
        if (functionName.Equals("execute_sequence", StringComparison.OrdinalIgnoreCase) ||
            functionName.Equals("executesequence", StringComparison.OrdinalIgnoreCase))
        {
            args = new[] { argsRaw.Trim() };
        }
        else
        {
            // For standard plugins, split by '|' only if NOT preceded by '\'
            // AND (Crucial) only if it's not nested inside another set of braces
            // This Regex splits on pipes that are NOT followed by an unbalanced closing brace
            args = string.IsNullOrWhiteSpace(argsRaw)
                ? Array.Empty<string>()
                : Regex.Split(argsRaw, @"(?<!\\)\|(?=(?:[^{}]*\{[^{}]*\})*[^{}]*$)")
                       .Select(a => Unescape(a.Trim()))
                       .ToArray();
        }

        return new PluginCall(functionName, args);
    }

    private string Unescape(string input) => input.Replace(@"\|", "|");

    public object?[] MapArguments(ParameterInfo[] targetParameters, string[] providedArgs)
    {
        var mapped = new object?[targetParameters.Length];

        for (int i = 0; i < targetParameters.Length; i++)
        {
            var param = targetParameters[i];
            if (i < providedArgs.Length)
            {
                // Basic conversion for primitive types
                mapped[i] = Convert.ChangeType(providedArgs[i], param.ParameterType);
            }
            else
            {
                mapped[i] = param.HasDefaultValue ? param.DefaultValue : null;
            }
        }
        return mapped;
    }
}
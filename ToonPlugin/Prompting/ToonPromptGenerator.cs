
using ToonPlugin.Tooling;
using System.Text;

namespace ToonPlugin.Prompting;
internal class ToonPromptGenerator (ToonPluginRegistry registry)
{
    private readonly ToonPluginRegistry _registry = registry;
    private const string ProtocolHeader = "## TOON PROTOCOL (Token-Oriented Object Notation) ACTIVE";

    public string GetSystemPrompt()
    {
        var toolsCatalog = _registry.GetPluginCatalog();
        var sb = new StringBuilder();
        sb.AppendLine(ProtocolHeader);
        sb.AppendLine("You are an execution-oriented agent. Your communication with the system is performed via 'ToonPlugins'.");
        sb.AppendLine();
        sb.AppendLine("### RULES OF ENGAGEMENT:");
        sb.AppendLine("1. DO NOT USE JSON for tool calling.");
        sb.AppendLine("2. To trigger an action, use strictly this format: function_name{arg1,arg2}");
        sb.AppendLine("3. No spaces between arguments and braces unless they are part of the value.");
        sb.AppendLine("4. If no tool is needed, respond with standard natural language.");
        sb.AppendLine();
        sb.AppendLine("### AVAILABLE CLAWS (Catalog):");
        sb.AppendLine(toolsCatalog);
        sb.AppendLine();
        sb.AppendLine("### EXAMPLE:");
        sb.AppendLine("User: 'Check the weather in London'");
        sb.AppendLine("Assistant: get_weather{London}");

        return sb.ToString();
    }
}


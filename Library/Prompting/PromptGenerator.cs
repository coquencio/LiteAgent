
using LiteAgent.Tooling;
using System.Text;

namespace LiteAgent.Prompting;
internal class PromptGenerator (LitePluginRegistry registry)
{
    private readonly LitePluginRegistry _registry = registry;
    private const string ProtocolHeader = "## TOON PROTOCOL (Token-Oriented Object Notation) ACTIVE";

    public string GetSystemPrompt()
    {
        var toolsCatalog = _registry.GetPluginCatalog();
        var sb = new StringBuilder();
        sb.AppendLine(ProtocolHeader);
        sb.AppendLine("You are an execution-oriented agent. Your communication with the system is performed via 'LitePlugins'.");
        sb.AppendLine();
        sb.AppendLine("### RULES OF ENGAGEMENT:");
        sb.AppendLine("1. DO NOT USE JSON for tool calling.");
        sb.AppendLine("2. To trigger an action, use strictly this format: function_name{arg1,arg2}");
        sb.AppendLine("3. ARGUMENT SUBSTITUTION: Replace the placeholders in the catalog (e.g., {name}) with real data derived from the user's request.");
        sb.AppendLine("4. No spaces between arguments and braces unless they are part of the value.");
        sb.AppendLine("5. If no tool is needed, respond with standard natural language.");
        sb.AppendLine("6. If parameters are missing, respond with standard natural language asking for missing information.");
        sb.AppendLine();
        sb.AppendLine("### EXAMPLES OF SUBSTITUTION:");
        sb.AppendLine("- If catalog is 'greet{<name>}' and user says 'Hi, I am John', call: greet{John}");
        sb.AppendLine("- If catalog is 'search_jobs{<tech>,<location>}' and user wants .NET in Mexico, call: search_jobs{.NET,Mexico}");
        sb.AppendLine("- If catalog is 'calculate{<a>,<b>}' and user says 'Add 5 and 10', call: calculate{5,10}");
        sb.AppendLine();
        sb.AppendLine("### AVAILABLE Plugins (Catalog):");
        sb.AppendLine(toolsCatalog);
        sb.AppendLine();
        sb.AppendLine("### EXAMPLE:");
        sb.AppendLine("User: 'Check the weather in London'");
        sb.AppendLine("Assistant: get_weather{London}");
        sb.AppendLine("### EXAMPLE:");
        sb.AppendLine("Catalog entry: greet{name}");
        sb.AppendLine("User: 'Say hi to Alice'");
        sb.AppendLine("Assistant: greet{Alice}");

        return sb.ToString();
    }
}


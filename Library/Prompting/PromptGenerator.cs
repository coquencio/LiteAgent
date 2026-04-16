using LiteAgent.Tooling;
using System.Text;

namespace LiteAgent.Prompting;

internal class PromptGenerator(LitePluginRegistry registry)
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
        sb.AppendLine("1. DO NOT USE JSON. Use only the TOON format.");
        sb.AppendLine("2. FORMAT: use only plugin_name{arg1|arg2}. Use '|' to separate arguments.");
        sb.AppendLine("3. ESCAPING: If an argument contains a literal pipe, you MUST escape it as '\\|'.");
        sb.AppendLine("4. When calling a plugin, DO NOT include any additional text, ONLY the plugin call.");
        sb.AppendLine("5. TYPE INSPECTION: Use the return_type definitions to identify available properties.");
        sb.AppendLine("6. NO SPACES between arguments and braces unless they are part of the literal value.");
        sb.AppendLine("7. If no plugin is needed or info is missing, respond with standard natural language.");
        sb.AppendLine("8. DIRECT CALL: If the request requires only ONE tool, call it directly: plugin_name{args}.");
        sb.AppendLine("9. SEQUENCE: Use 'execute_sequence' ONLY if you need to pipe the output of one tool into another tool.");
        sb.AppendLine("10. NO WRAPPERS: Never wrap a single tool call inside 'execute_sequence'.");
        sb.AppendLine();

        sb.AppendLine("### DATA ACCESS & CHAINING:");
        sb.AppendLine("- PROPERTY ACCESS ($1.prop): Use this syntax ONLY inside 'execute_sequence' to pass a specific property to the NEXT step.");
        sb.AppendLine("- FINAL ANSWER: If you call a tool directly, you will receive the full object. Read its properties from the response to answer the user.");
        sb.AppendLine();

        sb.AppendLine("### EXAMPLES:");
        sb.AppendLine("- Simple Request: 'What is Jorge's email?'");
        sb.AppendLine("  Correct: get_user_details{Jorge}");
        sb.AppendLine("  Wrong: execute_sequence{get_user_details{Jorge}|$1.email}");
        sb.AppendLine();
        sb.AppendLine("- Chaining Request: 'Get Jorge's email and send him a reminder'");
        sb.AppendLine("  Correct: execute_sequence{get_user_details{Jorge}|send_email{$1.email|Reminder}}");
        sb.AppendLine();

        sb.AppendLine("### AVAILABLE PLUGINS (Catalog):");
        sb.AppendLine(toolsCatalog);
        sb.AppendLine();

        sb.AppendLine("### FINAL INSTRUCTION:");
        sb.AppendLine("Prioritize direct calls for information retrieval. Use sequences only for multi-step automated workflows.");

        return sb.ToString();
    }
}
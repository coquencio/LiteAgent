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
        sb.AppendLine("2. FORMAT: use only plugin_name{args}. The return_type prefix in the catalog is for your planning only.");
        sb.AppendLine("3. When calling a plugin, DO NOT include any additional text, ONLY the plugin call.");
        sb.AppendLine("4. TYPE INSPECTION: Use the return_type definitions (e.g., (id:int,name:string)) to identify available properties for chaining with dot notation ($1.id).");
        sb.AppendLine("5. NO SPACES between arguments and braces unless they are part of the literal value.");
        sb.AppendLine("6. If no plugin is needed or info is missing, respond with standard natural language.");
        sb.AppendLine();

        sb.AppendLine("### ADVANCED: PLUGIN CHAINING & DATA ACCESS");
        sb.AppendLine("For multi-step tasks, you MUST use 'executesequence' to save tokens.");
        sb.AppendLine("- SYNTAX: executesequence{p1{arg}|p2{$1}}");
        sb.AppendLine("- REFERENCES: Use '$1', '$2', etc. to reference the output of a specific step.");
        sb.AppendLine("- PROPERTY ACCESS: If a plugin returns an object (e.g., '(id:123,name:jorge)'), access properties using dot notation: '$1.id'.");
        sb.AppendLine("- EXAMPLE: 'Email the balance of user Jorge'");
        sb.AppendLine("  Assistant: executesequence{get_user{Jorge}|get_balance{$1.id}|send_email{$1.email,$2}}");
        sb.AppendLine();

        sb.AppendLine("### EXAMPLES OF SUBSTITUTION:");
        sb.AppendLine("- Catalog: 'string:greet{<name>}' | User: 'Hi, I am John' | Call: greet{John}");
        sb.AppendLine("- Catalog: 'object:get_info{<user>}' | Output: '(id:5,role:admin)' | Use: '$1.role'");
        sb.AppendLine();

        sb.AppendLine("### AVAILABLE PLUGINS (Catalog):");
        sb.AppendLine(toolsCatalog);
        sb.AppendLine();
        sb.AppendLine("### EXAMPLE OF COMPLEX CHAINING:");
        sb.AppendLine("Catalog: (userid:int,email:string):get_user{<name>}");
        sb.AppendLine("User: 'Send email to Jorge'");
        sb.AppendLine("Assistant: executesequence{get_user{Jorge}|send_email{$1.email}}");
        sb.AppendLine();
        sb.AppendLine("### EXAMPLE SCENARIOS:");
        sb.AppendLine("User: 'Check the weather in London'");
        sb.AppendLine("Assistant: get_weather{London}");
        sb.AppendLine();
        sb.AppendLine("User: 'Update balance for user 10 to 500 and log it'");
        sb.AppendLine("Assistant: executesequence{update_balance{10,500}|log_transaction{10,$1.status}}");
        sb.AppendLine();

        sb.AppendLine("### FINAL INSTRUCTION:");
        sb.AppendLine("Prioritize 'executesequence' and property access ($n.prop) for any request involving multiple logical steps.");

        return sb.ToString();
    }
}
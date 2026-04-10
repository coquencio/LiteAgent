# LiteAgent (Preview)

LiteAgent is a high-performance, token-efficient tool-calling library for .NET. It replaces verbose JSON payloads in LLM workflows with **TOON (Token-Oriented Object Notation)**, offering a lightweight agentic runtime that saves up to **80%** in input/output tokens.

> **Status:** Proof of Concept (POC). Built for developers who need agentic capabilities without the overhead of heavy frameworks like Semantic Kernel.

---

## The TOON Advantage

Standard LLM tool calling is "token-expensive" because it relies on massive JSON schemas. **TOON** flattens these structures into a dense, text-based format that LLMs understand natively.

- **Standard JSON:** `{"tool_calls":[{"id":"1","function":{"name":"greet","arguments":"{\"name\":\"Alice\"}"}}]}` (~60 tokens)
- **TOON (LiteAgent):** `greet{Alice}` (~6 tokens)

---

## Project Structure

Based on the current solution architecture:

- **Connectors:** Plug-and-play clients for Azure OpenAI using the official SDK (`ILiteClient`).
- **Actions:** The core engine that parses TOON strings and executes C# methods via reflection (`LiteActions`, `PluginParser`).
- **Tooling:** Simple attribute-based system (`[LitePlugin]`) to expose your code to the AI.
- **Prompting:** Dynamic generation of high-density system instructions with automated parameter substitution logic.

---

## Quick Start: Implementation Guide

### 1. Define your Tools (Plugins)
Create a class with methods decorated with `[LitePlugin]`. The system will automatically map arguments from the TOON text.

```csharp
using LiteAgent.Tooling;

public class BusinessTools
{
    [LitePlugin("Sends a greet asking for the name of the person")]
    public string Greet(string name)
    {
        return $"Hello, {name}! The tool was executed successfully.";
    }

    [LitePlugin("Retrieves a list of available items by category")]
    public List<string> GetInventory(string category)
    {
        return new List<string> { "Laptop", "Mouse", "Keyboard" };
    }
}
```

### 2. Configure `Program.cs` (Dependency Injection)
LiteAgent integrates seamlessly with the .NET Generic Host and Dependency Injection.

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using LiteAgent.Connectors;

var builder = Host.CreateApplicationBuilder(args);

// 1. Register the AI Connector (Azure OpenAI)
builder.Services.AddSingleton<ILiteClient>(sp => 
    new LiteAzureOpenAIClient(
        apiKey: "your-azure-api-key",
        deployment: "gpt-4o-mini-talent",
        endpoint: "https://talentassistant-resource.openai.azure.com"
    ));

// 2. Register the Orchestrator Agent
builder.Services.AddTransient<LiteOrchestratorAgent>();

using IHost host = builder.Build();
```

### 3. Run the Agent
The `LiteOrchestratorAgent` manages the autonomous cycle (Think-Act-Observe) until the task is finished.

```csharp
var agent = host.Services.GetRequiredService<LiteOrchestratorAgent>();

// Configure generation parameters (Optional)
agent.Configure(temperature: 0.5f, maxTokens: 1000);

// Register your plugins
agent.RegisterTools(new BusinessTools());

// Start the conversation
string response = await agent.SendMessageAsync("Say hi to Jorge and check the office inventory");

Console.WriteLine($"Agent: {response}");
```

---

## Technical Features

### Recursive TOON Serialization
The internal serialization engine handles complex nested structures by converting them into a compact notation optimized for LLM context:
- **Objects:** `(name:jorge,role:dev)`
- **Collections:** `[item1|item2|item3]`
- **Nesting:** `(id:1,tags:[c#|ai],meta:(ver:1.0))`

### Built-in Agentic Loop
When calling `SendMessageAsync`, the agent enters an autonomous cycle:
1. **Prompt:** Sends chat history + dynamic TOON system instructions to the LLM.
2. **Execute:** If the LLM responds with `plugin_name{args}`, the agent executes the C# method.
3. **Observe:** The execution result is fed back to the LLM as a `TOOL_RESULT`.
4. **Finalize:** The cycle continues until the LLM generates a final natural language response for the user.

---

## Roadmap
- [ ] **Advanced History Management:**
    - Configure Max Messages window.
    - Stateless execution support.
    - Automated history summarization for context optimization.
- [ ] **Complex Orchestration:**
    - Tool chaining (multi-step tool execution in a single turn).
- [ ] Support for `CancellationToken` in long-running loops.
- [ ] Tool output size capping to prevent context overflow.
- [ ] Source Generators for Reflection-free execution (AOT friendly).

## License
MIT
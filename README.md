# LiteAgent (Preview)

LiteAgent is a high-performance, token-efficient tool-calling library for .NET. It replaces verbose JSON payloads in LLM workflows with **TOON (Token-Oriented Object Notation)**, offering a lightweight agentic runtime that saves up to **80%** in input/output tokens.

> **Status:** Proof of Concept (POC). Built for developers who need agentic capabilities without the overhead of heavy frameworks like Semantic Kernel.

---

## The TOON Advantage

Standard LLM tool calling relies on massive JSON schemas. **TOON** flattens these structures into a dense, text-based format that LLMs understand natively.

- **Standard JSON:** `{"tool_calls":[{"id":"1","function":{"name":"greet","arguments":"{\"name\":\"Alice\"}"}}]}` (~60 tokens)
- **TOON (LiteAgent):** `greet{Alice}` (~6 tokens)

---

## Project Structure

- **Connectors:** Clients for Azure OpenAI using the official SDK (`ILiteClient`).
- **Actions:** The core engine that parses TOON and executes methods via reflection (`LiteActions`).
- **Tooling:** Base class system (`LitePluginBase`) and attributes (`[LitePlugin]`) to expose code efficiently.
- **Extensions:** Fluent API for seamless .NET Dependency Injection.

---

## Quick Start: Implementation Guide

### 1. Define your Tools
Plugins are created by inheriting from `LitePluginBase`. Decorate your methods with `[LitePlugin]` to make them discoverable.

```csharp
using LiteAgent.Tooling;

public class BusinessTools : LitePluginBase
{
    [LitePlugin("Sends a greet asking for the name of the person")]
    public string Greet(string name) => $"Hello, {name}!";

    [LitePlugin("Retrieves items by category")]
    public List<string> GetInventory(string category) => 
        new() { "Laptop", "Mouse", "Keyboard" };
}
```

### 2. Configuration & DI
LiteAgent offers two flexible ways to configure the agent: globally during dependency injection or dynamically at runtime.

#### Option A: Registration via Extension Methods
Use the `AddLiteAgent` extensions to configure behavior and register plugins directly in the `IServiceCollection`.

```csharp
using LiteAgent.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// 1. Register the AI Connector
builder.Services.AddAzureOpenAILiteClient(
    apiKey: "your-api-key",
    deploymentName: "gpt-4o-mini",
    endpoint: "https://your-resource.openai.azure.com"
);

// 2. Register Agent with full configuration (Temp, Tokens, and Plugins)
builder.Services.AddLiteAgent(
    temp: 0.2f, 
    maxTokens: 500, 
    new BusinessTools()
);

using IHost host = builder.Build();
```

#### Option B: Runtime Configuration
You can register a default agent using `builder.Services.AddLiteAgent()` without initial parameters. This allows you to modify the behavior, provide custom context, or register tools only when you obtain the instance at runtime.

```csharp
// 1. Simple registration in Program.cs
builder.Services.AddLiteAgent();

// 2. Configure after getting the instance
var agent = host.Services.GetRequiredService<LiteOrchestratorAgent>();

// Manual configuration as needed
agent.Configure(temperature: 0.8f, maxTokens: 2000);
agent.RegisterTools(new BusinessTools());

// Add custom instructions or background information
agent.AddContext("Always prioritize environmentally friendly items in inventory checks.");
```

### 3. Run the Agent
The `LiteOrchestratorAgent` manages the autonomous **Think-Act-Observe** cycle. You can also specify if the call should be **stateless**.

```csharp
// Start the conversation (stateless: true by default)
string response = await agent.SendMessageAsync("Say hi to Jorge and check the office inventory", stateless: false);

Console.WriteLine($"Agent: {response}");
```

---

## Technical Features

### Autonomous Agentic Loop
The agent manages a self-correcting cycle:
1. **System Prompt:** Injects dynamic TOON instructions and any `AddContext` data.
2. **Execute:** If the LLM generates a TOON string (e.g., `Greet{Jorge}`), the agent executes the C# method.
3. **Observe:** Results are fed back to the model as a `TOOL_RESULT` for further processing.
4. **Finalize:** The cycle repeats until a final answer is reached. If `stateless` is true, history is cleared after completion.

### Flexible Injection
- **`AddLiteAgent()`**: Basic registration.
- **`AddLiteAgent(float temp, int maxTokens, params LitePluginBase[] instances)`**: Full-stack registration in a single line.
- **`AddContext(string context)`**: Appends custom logic or background to the system instructions.
- **`RegisterTools(...)`**: Dynamic plugin registration.

---

## Roadmap
- [ ] **Advanced History Management:** Max message window and summarization.
- [ ] **Multi-Model Support:** Google Gemini, DeepSeek, and Anthropic connectors.
- [ ] **Complex Orchestration:** Multi-step tool chaining in a single turn.
- [ ] **Source Generators:** AOT-friendly execution for high-performance environments.

## License
MIT
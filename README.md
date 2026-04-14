# LiteAgent (Preview)

[![NuGet version](https://img.shields.io/nuget/v/LiteAgent.svg?style=flat-square)](https://www.nuget.org/packages/LiteAgent/)

LiteAgent is a high-performance, token-efficient tool-calling library for .NET. It replaces verbose JSON payloads in LLM workflows with **TOON (Token-Oriented Object Notation)**, offering a lightweight agentic runtime that saves up to **80%** in input/output tokens.

> **Status:** Proof of Concept (POC). Built for developers who need agentic capabilities without the overhead of heavy frameworks like Semantic Kernel.

-----

## The TOON Advantage

Standard LLM tool calling relies on massive JSON schemas. **TOON** flattens these structures into a dense, text-based format that LLMs understand natively.

  - **Standard JSON:** `{"tool_calls":[{"id":"1","function":{"name":"greet","arguments":"{\"name\":\"Jorge\"}"}}]}` (\~60 tokens)
  - **TOON (LiteAgent):** `greet{Jorge}` (\~6 tokens)
  - **TOON Multi-Arg:** `log{System|Error\\|Critical}` (Uses `|` as separator and `\\|` for escaping)
-----

## Multi-Model Support
LiteAgent supports the most capable models in the industry through official and high-performance SDKs:

* **Azure OpenAI:** Enterprise-grade integration.
* **Google Gemini:** Official `Google.GenAI` support (Flash & Pro).
* **Anthropic Claude:** High-reasoning agentic workflows via `Anthropic.SDK`.
* **Generic OpenAI:** Support for **Ollama**, **Groq**, **DeepSeek**.
-----

## Project Structure

  - **Connectors:** Pluggable AI clients (ILiteClient) for Azure, Gemini, Claude, and local models.
  - **Actions:** The core engine that parses TOON and executes methods via reflection (`LiteActions`).
  - **Tooling:** Plugin system and attributes (`[LitePlugin]`) to expose code efficiently.
  - **Extensions:** Fluent API for seamless .NET Dependency Injection.
  - **Constants:** Shared definitions like Roles for messaging.

-----

## Quick Start: Implementation Guide

### 1\. Define your Tools

Plugins are plain C\# classes to keep your code clean and decoupled. Simply decorate your methods with `[LitePlugin]` to make them discoverable.

```csharp
using LiteAgent.Tooling;

public class BusinessTools
{
    [LitePlugin("Sends a greet asking for the name of the person")]
    public string Greet(string name) => $"Hello, {name}!";

    [LitePlugin("Retrieves items by category")]
    public List<string> GetInventory(string category) => 
        new() { "Laptop", "Mouse", "Keyboard" };
}
```

### 2\. Configuration & DI

LiteAgent is designed to work with the .NET Dependency Injection container. **Crucially, all plugin classes must be registered in the Service Provider first.**

#### Option A: Fluent Registration (Recommended)

Use the `AddLiteAgent` extension to configure parameters and register plugins that are already in the DI container.

```csharp
using LiteAgent.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// 1. Register your Plugin classes in the DI container
builder.Services.AddSingleton<BusinessTools>();
builder.Services.AddSingleton<InventoryPlugins>();

// 2. Register the AI Connector
// --- Option A: Azure OpenAI (Official) ---
builder.Services.AddAzureOpenAILiteClient(
    apiKey: "your-api-key",
    deploymentName: "gpt-4o-mini",
    endpoint: "https://your-resource.openai.azure.com"
);

// --- Option B: Google Gemini (Official) ---
builder.Services.AddGeminiLiteClient(
    apiKey: "your-google-api-key",
    modelName: "gemini-1.5-flash"
);

// --- Option C: Anthropic Claude ---
builder.Services.AddClaudeLiteClient(
    apiKey: "your-anthropic-key",
    modelName: "claude-3-5-sonnet-latest"
);

// --- Option D: Local Models (Ollama) ---
builder.Services.AddGenericOpenAILiteClient(
    apiKey: "ollama",
    modelId: "llama3",
    endpoint: "http://localhost:11434/v1"
);

// 3. Configure the Agent with the registered plugins
builder.Services.AddLiteAgent(config => 
{
    config.AddPlugin<BusinessTools>();
    config.AddPlugin<InventoryPlugins>();
    config.SetTemperature(0.7f);
    config.SetMaxTokens(1000);
    // Sets the limit for the history pruning (Default: 128,000)
    config.SetMaxContextTokens(128000);
});

using IHost host = builder.Build();
```

#### Option B: Manual Instance Registration

If your tools are not managed by the DI container, you can still add direct instances to the agent at runtime.

```csharp
var agent = host.Services.GetRequiredService<LiteOrchestratorAgent>();
var manualTool = new BusinessTools();

agent.RegisterToolInstances(manualTool);
```

### 3\. Run the Agent

The `LiteOrchestratorAgent` manages the autonomous **Think-Act-Observe** cycle. You can provide specific context or instructions right before sending a message.
#### Option A: Managed History (Internal)

The agent maintains an internal `_history` list. You can choose to clear it after each call or keep it for multi-turn conversations.

```csharp
var agent = host.Services.GetRequiredService<LiteOrchestratorAgent>();

// Add custom instructions or personality at runtime
agent.AddContext("You love to crack some silly jokes when returning final answers.");

// Customize model settings at runtime
agent.Configure(temperature: 0.7f, maxTokens: 1000);


// Start the conversation (stateless: true clears history after the response, false preserves agent instance's history)
string response = await agent.SendMessageAsync("Greet Jorge and check the office inventory", stateless: true);

Console.WriteLine($"Agent: {response}");
```

#### Option B: External History (Full Control)

Pass your own `List<LiteMessage>`. The agent will automatically inject system instructions and custom context if they are missing, and apply pruning rules.

```csharp
var myHistory = new List<LiteMessage>(); // Could be loaded from a Database
string response = await agent.SendMessageAsync("Check inventory", myHistory);
```

-----

## Technical Features

### Autonomous Agentic Loop

The agent manages a self-correcting cycle:

1.  **System Prompt:** Injects dynamic TOON instructions and any `AddContext` data.
2.  **Execute:** If the LLM generates a TOON string (e.g., `greet{Jorge}`), the agent executes the C\# method.
3.  **Observe:** Results (including execution traces) are fed back to the model.
4.  **Finalize:** The cycle repeats until the LLM provides a final natural language response.

### Smart Dependency Resolution

When using `AddPlugin<T>()`, the agent resolves the instance directly from your `IServiceProvider`. This allows your plugins to use their own injected dependencies (like DB Contexts or specialized services) via standard constructor injection.


### **Sequence Orchestrator (Pipelines)**

LiteAgent supports **Autonomous Chaining**. Instead of multiple round-trips between the LLM and your server, the agent can plan and execute a complex sequence of plugins in a single turn using `executesequence`.

#### Features:

  * **Zero-Latency Chaining:** Execute `Plugin A | Plugin B | Plugin C` entirely in C\#.
  * **Indexed References:** Use `$1`, `$2`, etc., to pass results from previous steps to the next one.
  * **Dot Notation Access:** Access specific properties of complex objects (e.g., `$1.id` or `$1.email`).
  * **Type Discovery:** The system automatically exposes return types (like `(id:int,name:string)`) so the LLM knows exactly which properties are available for chaining.
  * **Execution Trace:** Returns a summarized trace of each step: `[#1: get_user -> success] [#2: get_balance -> 500]`.

**Example:**
`executesequence{get_user{Jorge}|get_balance{$1.id}|send_email{$1.email|$2}}`

### Smart Context Pruning

To avoid "Context Window Exceeded" errors, LiteAgent includes a Pruning Mechanism:

1. **System Preservation:** System instructions and custom context are always kept at the top of the stack.

2. **Sliding Window:** When EstimateTokens exceeds MaxContextTokens, the oldest conversational messages are removed first.

3. **Automatic Injection:** When using external history, EnsureSystemContext verifies that the agent's core instructions are present before processing.

-----

## Roadmap

  - [X] **Advanced History Management:** Max message window and summarization.
  - [X] **Multi-Model Support:** Google Gemini, DeepSeek, and Anthropic connectors.
  - [X] **Complex Orchestration:** Multi-step tool chaining in a single turn.

## Keywords

LLM, OpenAI, Azure OpenAI, token optimization, reduce tokens, function calling, tool calling, .NET AI, AI agents, Semantic Kernel alternative, prompt optimization, cost reduction, TOON format

## License
MIT

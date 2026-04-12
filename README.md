# LiteAgent (Preview)

[![NuGet version](https://img.shields.io/nuget/v/LiteAgent.svg?style=flat-square)](https://www.nuget.org/packages/LiteAgent/)

LiteAgent is a high-performance, token-efficient tool-calling library for .NET. It replaces verbose JSON payloads in LLM workflows with **TOON (Token-Oriented Object Notation)**, offering a lightweight agentic runtime that saves up to **80%** in input/output tokens.

> **Status:** Proof of Concept (POC). Built for developers who need agentic capabilities without the overhead of heavy frameworks like Semantic Kernel.

-----

## The TOON Advantage

Standard LLM tool calling relies on massive JSON schemas. **TOON** flattens these structures into a dense, text-based format that LLMs understand natively.

  - **Standard JSON:** `{"tool_calls":[{"id":"1","function":{"name":"greet","arguments":"{\"name\":\"Jorge\"}"}}]}` (\~60 tokens)
  - **TOON (LiteAgent):** `greet{Jorge}` (\~6 tokens)

-----

## Project Structure

  - **Connectors:** Clients for Azure OpenAI using the official SDK (`ILiteClient`).
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
builder.Services.AddAzureOpenAILiteClient(
    apiKey: "your-api-key",
    deploymentName: "gpt-4o-mini",
    endpoint: "https://your-resource.openai.azure.com"
);

// 3. Configure the Agent with the registered plugins
builder.Services.AddLiteAgent(config => 
{
    config.AddPlugin<BusinessTools>();
    config.AddPlugin<InventoryPlugins>();
    config.SetTemperature(0.7f);
    config.SetMaxTokens(1000);
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

```csharp
var agent = host.Services.GetRequiredService<LiteOrchestratorAgent>();

// Add custom instructions or personality at runtime
agent.AddContext("You love to crack some silly jokes when returning final answers.");

// Start the conversation (stateless: true clears history after the response)
string response = await agent.SendMessageAsync("Greet Jorge and check the office inventory", stateless: true);

Console.WriteLine($"Agent: {response}");
```

-----

## Technical Features

### Autonomous Agentic Loop

The agent manages a self-correcting cycle:

1.  **System Prompt:** Injects dynamic TOON instructions and any `AddContext` data.
2.  **Execute:** If the LLM generates a TOON string (e.g., `greet{Jorge}`), the agent executes the C\# method.
3.  **Observe:** Results are fed back to the model as a `TOOL_RESULT`.
4.  **Finalize:** The cycle repeats until the LLM provides a final natural language response.

### Smart Dependency Resolution

When using `AddPlugin<T>()`, the agent resolves the instance directly from your `IServiceProvider`. This allows your plugins to use their own injected dependencies (like DB Contexts or specialized services) via standard constructor injection.

-----

## Roadmap

  - [ ] **Advanced History Management:** Max message window and summarization.
  - [ ] **Multi-Model Support:** Google Gemini, DeepSeek, and Anthropic connectors.
  - [ ] **Complex Orchestration:** Multi-step tool chaining in a single turn.
  - [ ] **Source Generators:** AOT-friendly execution for high-performance environments.

## License

MIT

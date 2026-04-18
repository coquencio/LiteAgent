# Quick Start

This quick start demonstrates how to register a plugin, configure an AI connector, and run a minimal `LiteOrchestratorAgent`.

Requirements
- .NET 10 (or later)
- A valid API key for the AI provider you will use (OpenAI-compatible, Gemini, or Anthropic)

Example: Minimal console app

```csharp
// Program.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LiteAgent.Extensions;
using LiteAgent.Connectors;

// 1) Define a plugin class. Methods must be decorated with [LitePlugin].
public class BusinessTools
{
    [LitePlugin("Sends a greeting", maxRetries: 3)]
    public string Greet(string name) => $"Hello, {name}!";

    [LitePlugin("Returns inventory for a category")]
    public List<string> GetInventory(string category) => new() { "Laptop", "Mouse", "Keyboard" };
}

public static async Task<int> Main(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);

    // 2) Register plugin types with DI
    builder.Services.AddSingleton<BusinessTools>();

    // 3) Register an AI client (choose one connector). Example: Generic OpenAI-compatible client
    builder.Services.AddGenericOpenAILiteClient(apiKey: "YOUR_API_KEY", modelId: "gpt-4o-mini", endpoint: "https://api.openai.com/v1");

    // 4) Register and configure the LiteAgent
    builder.Services.AddLiteAgent(cfg =>
    {
        cfg.AddPlugin<BusinessTools>();
        cfg.SetTemperature(0.7f);
        cfg.SetMaxTokens(1000);
        cfg.SetMaxContextTokens(128000);
        cfg.SetMaxTurns(10);
    });

    using var host = builder.Build();

    // 5) Resolve the agent and run a query
    var agent = host.Services.GetRequiredService<LiteOrchestratorAgent>();

    // Optional: Add custom runtime context that will be injected into the system instructions
    agent.AddContext("You are concise and return only the requested information when possible.");

    // Send a natural language prompt. The model may return a TOON call and the agent will execute it.
    string response = await agent.SendMessageAsync("Greet Jorge and list the office inventory", stateless: true);

    Console.WriteLine("Agent response:\n" + response);

    return 0;
}
```

Notes
- `SendMessageAsync` accepts either a natural-language prompt (the model may decide to call plugins) or a direct TOON string (e.g., `greet{Jorge}`).
- To register a connector for Azure OpenAI, use `AddAzureOpenAILiteClient(apiKey,deploymentName,endpoint)`.
- To register a connector for Google Gemini use `AddGeminiLiteClient(apiKey,modelName)`.
- For Anthropic Claude use `AddClaudeLiteClient(apiKey,modelName)`.

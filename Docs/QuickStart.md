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

## Defining plugins

Plugins are plain C# classes whose *public instance methods* are decorated with the `[LitePlugin]` attribute. The attribute accepts an optional description and a `maxRetries` value. Method names are normalized to snake_case when the agent registers them (for example `GetUserAsync` -> `get_user_async`).

Rules and recommendations
- Methods must be public instance methods on a class registered in DI or provided via `RegisterToolInstances`.
- Parameter values are parsed from TOON strings and converted using `Convert.ChangeType` where possible. Provide arguments in the same order as the method signature.
- Parameters with default values are supported; if an argument is omitted the method's default value will be used.
- Escape literal pipe characters in arguments using `\|`.
- Avoid naming a plugin `execute_sequence` — that identifier is reserved for the built-in orchestrator.

Example plugin definitions

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteAgent.Tooling;

public class BusinessTools
{
    [LitePlugin("Sends a greeting", maxRetries: 3)]
    public string Greet(string name) => $"Hello, {name}!";

    [LitePlugin("Returns inventory for a category")]
    public List<string> GetInventory(string category) => new() { "Laptop", "Mouse", "Keyboard" };

    [LitePlugin("Async example that returns a user record")]
    public async Task<UserInfo> GetUserAsync(string username)
    {
        await Task.Delay(10);
        return new UserInfo { Id = 1, Email = $"{username}@example.com", Name = username };
    }

    [LitePlugin("Optional parameter example")]
    public string Echo(string message, int repeat = 1)
    {
        return string.Join("|", Enumerable.Repeat(message, repeat));
    }
}

public class UserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
```

How plugin results are represented
- Primitive and string results are returned as-is.
- Enumerables are serialized as TOON lists: `[item1|item2]`.
- Complex objects are serialized as TOON tuples: `(prop1:value,prop2:value)`.

Invoking plugins directly
- You can ask the model to call a plugin (the model may reply with a TOON call), or invoke a plugin directly by sending a TOON string to `SendMessageAsync`: `greet{Jorge}`.

Example: direct TOON invocation

```csharp
string result = await agent.SendMessageAsync("greet{Jorge}", stateless: true);
Console.WriteLine(result); // -> "Hello, Jorge!"
```


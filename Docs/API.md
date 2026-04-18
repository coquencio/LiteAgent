# API Reference — Public Surface

This page lists the public types and extension methods intended for library consumers. Internal types (such as `LiteActions`, `PluginParser`, and `LitePluginRegistry`) are not documented here.

## Connectors

### `ILiteClient` (namespace: `LiteAgent.Connectors`)

| Member | Signature | Description |
|---|---|---|
| `SetMaxTokens` | `void SetMaxTokens(int maxTokens)` | Configure the maximum output tokens for the client.
| `SetTemperature` | `void SetTemperature(float temperature)` | Configure sampling temperature.
| `GetCompletionAsync` | `Task<LiteResponse> GetCompletionAsync(List<LiteMessage> history)` | Send the conversation history to the model and return the generated content and token usage.

#### Records

| Type | Properties | Description |
|---|---|---|
| `LiteMessage` | `string Role`, `string Content` | Simple message wrapper used by the agent for history.
| `LiteUsage` | `int PromptTokens`, `int CompletionTokens`, `int TotalTokens` | Token accounting returned by connectors.
| `LiteResponse` | `string Content`, `LiteUsage Usage` | Response object returned by `ILiteClient` implementations.

### Built-in connector implementations (implement `ILiteClient`)

| Class | Constructor | Notes |
|---|---:|---|
| `LiteAzureOpenAIClient` | `LiteAzureOpenAIClient(string apiKey, string deployment, string endpoint)` | Azure OpenAI connector using `Azure.AI.OpenAI` types.
| `LiteGenericOpenAIClient` | `LiteGenericOpenAIClient(string apiKey, string modelId, string endpoint = "https://api.openai.com/v1")` | Generic OpenAI-compatible connector (use for OpenAI, Ollama, Groq, etc.).
| `LiteGeminiClient` | `LiteGeminiClient(string apiKey, string modelName = "gemini-1.5-flash")` | Google Gemini connector (uses `Google.GenAI`).
| `LiteClaudeClient` | `LiteClaudeClient(string apiKey, string modelName = "claude-3-5-sonnet-20240620")` | Anthropic Claude connector (uses `Anthropic.SDK`).

Each connector exposes the `ILiteClient` methods: `SetMaxTokens`, `SetTemperature`, and `GetCompletionAsync`.

## Agent

### `LiteOrchestratorAgent` (namespace: `LiteAgent.Connectors`)

| Member | Signature | Description |
|---|---|---|
| Constructor | `LiteOrchestratorAgent(ILiteClient aiClient, IServiceProvider serviceProvider, ILoggerFactory? loggerFactory)` | Create a new agent instance (normally resolved from DI via extension methods).
| `GetTokenUsage` | `LiteUsage GetTokenUsage()` | Returns accumulated token usage for the agent instance.
| `RegisterToolInstances` | `void RegisterToolInstances(params object[] instances)` | Register manual plugin instances at runtime.
| `Configure` | `void Configure(float temperature = 0.7f, int maxTokens = 1000, int maxTurns = 10)` | Update runtime client settings and `MaxTurns` safety limit.
| `AddContext` | `void AddContext(string context)` | Append runtime context that will be injected into the system instructions.
| `SendMessageAsync` | `Task<string> SendMessageAsync(string userMessage, bool stateless = true)` | Primary API: sends a user message (natural language or TOON) and returns the final assistant output. If `stateless` is `true`, history isn't preserved.
| `SendMessageAsync` | `Task<string> SendMessageAsync(string userMessage, List<LiteMessage> externalHistory)` | Alternate overload accepting an externally-provided conversation history.

Notes
- The agent will parse TOON calls produced by the model, execute plugins, insert tool results back into the conversation, and repeat until a natural-language final answer is delivered or `MaxTurns` is reached.

## Registration & DI helpers

### `LiteAgentExtensions` (namespace: `LiteAgent.Extensions`)

| Method | Signature | Description |
|---|---|---|
| `AddLiteAgent` | `IServiceCollection AddLiteAgent(this IServiceCollection services, Action<LiteAgentConfigurationBuilder>? configure = null)` | Registers the `LiteOrchestratorAgent` in DI. Use the `configure` callback to add plugin types and tune defaults.
| `AddAzureOpenAILiteClient` | `IServiceCollection AddAzureOpenAILiteClient(this IServiceCollection services, string apiKey, string deploymentName, string endpoint)` | Register Azure OpenAI connector as `ILiteClient`.
| `AddGeminiLiteClient` | `IServiceCollection AddGeminiLiteClient(this IServiceCollection services, string apiKey, string modelName = "gemini-1.5-flash")` | Register Gemini connector as `ILiteClient`.
| `AddClaudeLiteClient` | `IServiceCollection AddClaudeLiteClient(this IServiceCollection services, string apiKey, string modelName = "claude-3-5-sonnet-latest")` | Register Claude connector as `ILiteClient`.
| `AddGenericOpenAILiteClient` | `IServiceCollection AddGenericOpenAILiteClient(this IServiceCollection services, string apiKey, string modelId, string endpoint = "https://api.openai.com/v1")` | Register generic OpenAI-compatible connector as `ILiteClient`.

### `LiteAgentConfigurationBuilder` (fluent builder used in `AddLiteAgent`)

| Method | Signature | Description |
|---|---|---|
| `AddPlugin<T>()` | `LiteAgentConfigurationBuilder AddPlugin<T>() where T : class` | Register a plugin type (resolved from DI at runtime).
| `SetMaxTokens` | `LiteAgentConfigurationBuilder SetMaxTokens(int tokens)` | Set default max tokens.
| `SetTemperature` | `LiteAgentConfigurationBuilder SetTemperature(float temp)` | Set default sampling temperature.
| `SetMaxContextTokens` | `LiteAgentConfigurationBuilder SetMaxContextTokens(int tokens)` | Configure context pruning threshold.
| `SetMaxTurns` | `LiteAgentConfigurationBuilder SetMaxTurns(int turns)` | Configure the agentic loop safety limit.

## Plugin attribute

| Attribute | Signature | Description |
|---|---|---|
| `LitePlugin` | `[LitePlugin(string? description = null, int maxRetries = 2)]` | Mark a public instance method as a tool. `description` helps the LLM; `maxRetries` controls how many automatic retries are attempted on execution failures.

## Implementation notes
- Several internal helpers power the runtime: `LiteActions` handles plugin invocation and serialization, `PluginParser` parses TOON, `SequencePlugin` implements `execute_sequence` orchestration, and `PromptGenerator` builds system instructions including the plugin catalog.
- The registry normalizes method names to snake_case and reserves the `execute_sequence` name for internal orchestration.

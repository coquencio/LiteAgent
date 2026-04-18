# Core Concepts

This document explains the key design elements and runtime behaviour of `LiteAgent`.

TOON (Token-Oriented Object Notation)
- Lightweight, text-first format for tool calls. Syntax: `plugin_name{arg1|arg2}`.
- Use `\|` to escape literal pipe characters inside arguments.
- `execute_sequence{...}` is a special built-in orchestration call used to chain plugin invocations.

Plugin registration and discovery
- Methods are exposed to the agent by decorating them with the `LitePlugin` attribute.
- Plugin method names are normalized to snake_case when registered (e.g., `GetUserDetails` -> `get_user_details`).
- Register plugin classes in your DI container (e.g., `services.AddSingleton<MyTools>()`) and add them to the agent configuration with `cfg.AddPlugin<T>()`.

Agentic loop (Think → Act → Observe)
- The `LiteOrchestratorAgent` sends a system prompt + conversation history to the configured `ILiteClient`.
- If the LLM returns a TOON call, the agent attempts to parse and execute it via reflection.
- After execution, results are fed back into the conversation as `TOOL_RESULT: ...` and the loop repeats until the model returns a natural-language final answer or the configured `MaxTurns` is reached.

History management
- The agent supports three history modes:
	- **Instance history:** When you call `SendMessageAsync(..., stateless: false)` the agent appends messages to an internal `List<LiteMessage>` held on the agent instance. Use this when you want multi-turn conversations to persist between calls.
	- **Stateless calls:** Use `SendMessageAsync(userMessage, stateless: true)` to run a single, ephemeral conversation. The agent creates a fresh in-memory history for the call and does not persist it.
	- **External history:** You can load a serialized conversation from an external store (database, file, etc.), deserialize it to `List<LiteMessage>`, and pass it to `SendMessageAsync(userMessage, externalHistory)`. The agent will automatically inject missing system instructions via `EnsureSystemContext` and apply pruning.

Example (deserialize history from storage and execute):

```csharp
// using System.Text.Json;
string json = await conversationStore.LoadConversationJsonAsync(conversationId);
var externalHistory = JsonSerializer.Deserialize<List<LiteMessage>>(json)!;
string response = await agent.SendMessageAsync("Check inventory", externalHistory);
```

Notes:
- `AddContext(string)` appends runtime context to the agent; when using instance history this custom context will be preserved and injected into future turns.
- When persisting histories yourself, store only `Role` and `Content` for each `LiteMessage` and be mindful of privacy and security of stored prompts.

Retry and resiliency
- Each `LitePlugin` can specify `maxRetries` in its attribute. If an invocation throws or fails, the agent returns a `FIX_ATTEMPT:` signal to the model and allows it to correct arguments and re-call the tool (up to the configured limit).

MaxTurns (agentic loop limit)
- `MaxTurns` is the safety limit for the agent's internal think-act-observe loop. The default is 10. You can configure it:
	- During DI registration via `cfg.SetMaxTurns(n)` when calling `AddLiteAgent(...)`.
	- At runtime with `agent.Configure(..., maxTurns: n)`.
- Behaviour: the agent increments an internal turn counter each loop. If the model does not return a final natural-language response before the counter reaches `MaxTurns`, the agent stops and returns the error string `Error: Maximum agentic turns reached without a conclusion.`.
- Recommendation: set `MaxTurns` conservatively (5-20) depending on expected complexity. Larger values increase token usage and cost; smaller values reduce wasted cycles but may prematurely abort multi-step work.

Sequence orchestration
- Use `execute_sequence{step1|step2|...}` to run multiple plugin calls synchronously on the server.
- Inside sequences you can reference previous step outputs using `$1`, `$2`, or `$LAST` and access object properties using dot notation (e.g., `$1.email`).
- The sequence implementation returns a concise trace like: `[ #1: get_user -> (id:1,email:joe@x) ] [#2: send_email -> success]`.

Token handling and pruning
- Token estimation is approximate: `EstimateTokens` divides a message's character length by 4. This is a heuristic used to prune the oldest non-system messages when the estimated context exceeds `MaxContextTokens`.

Public vs internal surface
- Public: `ILiteClient`, the built-in connector implementations (`LiteAzureOpenAIClient`, `LiteGenericOpenAIClient`, `LiteGeminiClient`, `LiteClaudeClient`), `LiteOrchestratorAgent`, `LitePlugin` attribute, and `LiteAgentExtensions` for DI integration.
- Internal (implementation details): `LiteActions`, `PluginParser`, `LitePluginRegistry`, `SequencePlugin`, `PromptGenerator`. These are subject to change.

Security and safety
- The library executes application code via reflection when invoking plugin methods. Only register trusted plugin classes and avoid exposing untrusted instances to the agent.

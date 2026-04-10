# LiteAgent (Preview)

LiteAgent is a high-performance, token-efficient tool-calling library for .NET. It is designed to replace verbose JSON payloads in Large Language Model (LLM) workflows with **TOON (Token-Oriented Object Notation)** and provide a lightweight agentic runtime.

> **Status:** Proof of Concept (POC) / Early Preview. APIs and specifications are subject to change.

---

## The Problem: The "JSON Tax"

Standard LLM tool calling (such as function calling or plugin-based approaches) relies on heavy JSON schemas for definitions and escaped JSON strings for execution. This consumes a significant portion of the context window, increasing both cost and latency.

---

## The Solution: LiteAgent + TOON Protocol

LiteAgent introduces a compact, text-based communication layer using **TOON**.

By combining:
- A specialized system prompt generator  
- A lightweight parser  
- A minimal orchestration layer  

LiteAgent reduces the footprint of tool calling by up to **80%**, enabling faster and cheaper LLM interactions.

---

## Visual Comparison

- **Standard JSON:**  
  `{"tool_calls":[{"id":"123","function":{"name":"get_weather","arguments":"{\"city\":\"London\"}"}}]}`  
  (~60 tokens)

- **TOON (LiteAgent):**  
  `get_weather{London}`  
  (~6 tokens)

---

## Key Features

- **Zero-JSON Definitions**  
  No more massive JSON schemas in system prompts.

- **Token-Efficient Protocol (TOON)**  
  Compact syntax optimized for LLM communication.

- **Strongly Typed Mapping**  
  Automatic conversion from TOON text into C# types (`int`, `bool`, `DateTime`, etc.).

- **Lightweight Orchestration**  
  Built-in orchestration via `LiteOrchestrator` for handling tool execution loops.

- **Plugin System**  
  Simple and extensible plugin model powered by attributes and registries.

- **Async Native**  
  First-class support for `Task` and `Task<T>`.

---

## Project Structure

LiteAgent is organized into three main pillars:

- **Tooling**  
  Defines plugins and tool metadata  
  (`LitePlugin`, `ToonPluginDefinition`, `ToonPluginRegistry`)

- **Prompting**  
  Generates high-density system prompts for LLMs  
  (`PromptGenerator`)

- **Actions**  
  Handles parsing and execution of tool calls  
  (`LiteOrchestrator`, `PluginParser`)

---

## Quick Start

### 1. Define your Plugins

Decorate your methods with the `[LitePlugin]` attribute:

```csharp
public class SearchTools
{
    [LitePlugin("Search for job vacancies by technology and location")]
    public string SearchJobs(string tech, string location)
    {
        return "Found 3 jobs at KPMG, Microsoft, and Tesla";
    }
}
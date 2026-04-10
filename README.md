# ToonPlugin (Preview)

ToonPlugin is a high-performance, token-efficient tool-calling library for .NET. It is designed to replace verbose JSON payloads in Large Language Model (LLM) workflows with **TOON (Token-Oriented Object Notation)**.

> **Status:** Proof of Concept (POC) / Early Preview. APIs and specifications are subject to change.

## The Problem: The "JSON Tax"
Standard LLM Tool Calling (like OpenAI Functions or Semantic Kernel Plugins) relies on heavy JSON Schemas for definitions and escaped JSON strings for execution. This consumes a significant portion of the context window and increases costs and latency.

## The Solution: TOON Protocol
ToonPlugin forces the LLM to communicate using a compact, text-based notation. By using a specialized system prompt and a regex-based interceptor, we reduce the footprint of tool calling by up to **80%**.

### Visual Comparison
* **Standard JSON:** `{"tool_calls":[{"id":"123","function":{"name":"get_weather","arguments":"{\"city\":\"London\"}"}}]}` (~60 tokens)
* **ToonPlugin:** `get_weather{London}` (~6 tokens)

## Key Features
* **Zero-JSON Definitions:** No more massive JSON schemas in your system prompts.
* **Strongly Typed Mapping:** Automatic conversion from TOON text to C# types (`int`, `bool`, `DateTime`, etc.).
* **Recursive Serialization:** Complex objects and arrays are flattened into a dense `(key:val,list:[a|b])` format.
* **Async Native:** Built-in support for `Task` and `Task<T>` methods.

## Project Structure
The library is organized into three main pillars:
* **Tooling:** Attribute-based registry to discover and define "plugins" (tools).
* **Prompting:** Generates high-density system instructions for the LLM.
* **Actions:** Parsers and Orchestrators that intercept LLM output and execute C# code.

## Quick Start

### 1. Define your Plugins
Decorate your service methods with the `[ToonPlugin]` attribute.

```csharp
public class SearchTools
{
    [ToonPlugin("Search for job vacancies by technology and location")]
    public string SearchJobs(string tech, string location)
    {
        return "Found 3 jobs at KPMG, Microsoft, and Tesla";
    }
}

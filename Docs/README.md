# LiteAgent — Documentation

This folder contains developer-oriented documentation for the `LiteAgent` library.

Contents
- Quick start and examples: `QuickStart.md`
- Core architecture and concepts: `CoreConcepts.md`
- API reference for the public surface: `API.md`

Installation

Install the published package from NuGet:

```bash
dotnet add package LiteAgent --version 0.1.9
```

Or use the NuGet package manager UI to add `LiteAgent`.

If you are consuming the source package directly, ensure your project targets `.NET 10` or later and adds the required connector packages (e.g., `Anthropic.SDK`, `Google.GenAI`, `Azure.AI.OpenAI`). See the project `LiteAgent.csproj` for the exact versions.

Recommended reading order

1. `QuickStart.md`https://github.com/coquencio/LiteAgent/blob/main/Docs/QuickStart.md — minimal working example to get an agent running.
2. `CoreConcepts.md`https://github.com/coquencio/LiteAgent/blob/main/Docs/CoreConcepts.md — TOON format, plugins, orchestration, and token handling.
3. `API.md`https://github.com/coquencio/LiteAgent/blob/main/Docs/API.md — public types, extension methods, and configuration API.

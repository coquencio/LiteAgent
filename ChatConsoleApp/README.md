# ChatConsoleApp

This folder contains a reference console application used to test and validate the **LiteAgent** library. It serves as a sandbox for internal development and a usage example for the library's features.

## Purpose
The project demonstrates the core integration flow of the library:
* **Dependency Injection**: Registration of plugins and agents within an `IServiceCollection`.
* **Execution Performance**: Validating tool calls using the pre-compiled lambda engine.
* **TOON Protocol**: Testing the LLM's ability to generate and chain custom TOON syntax.

## Key Components
* **EmailPlugins.cs and UserPlugins.cs**: Sample tools decorated with `[LitePlugin]` for testing various signatures and return types.
* **Program.cs**: Main entry point that wires the DI container and manages the interactive agent loop.

## How to Use
1. Set your API credentials in Program.cs.
2. Run the application.

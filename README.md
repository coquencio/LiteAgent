# LiteAgent

LiteAgent is a compact, token-efficient AI agent tool-calling library for .NET. It uses a text-first, TOON (Token-Oriented Object Notation) protocol to represent tool calls and provides a lightweight orchestration runtime for executing plugin methods.

This repository contains the source package and developer documentation. Note: the content of this file is shipped with the NuGet package and is what nuget.org displays on the package page. Relative links (for example `Docs/README.md`) may be resolved by the NuGet website in unexpected ways; for reliable external documentation links we recommend using the GitHub URLs below.

Developer documentation (on GitHub)

- Docs home: https://github.com/Coquencio/LiteAgent/tree/main/Docs
- Quick start: https://github.com/Coquencio/LiteAgent/blob/main/Docs/QuickStart.md
- Core concepts: https://github.com/Coquencio/LiteAgent/blob/main/Docs/CoreConcepts.md
- API reference: https://github.com/Coquencio/LiteAgent/blob/main/Docs/API.md

NuGet package

- Package page: https://www.nuget.org/packages/LiteAgent/
- Install (current):

```bash
dotnet add package LiteAgent --version 0.1.8
```

If you previously published a package and see an outdated or confusing README on nuget.org, update the README in this repository and publish a new package version (NuGet shows the README bundled in the nupkg). Example commands:

```bash
dotnet pack Library/LiteAgent.csproj -c Release
dotnet nuget push Library/bin/Release/LiteAgent.0.1.8.nupkg -s https://api.nuget.org/v3/index.json -k <NUGET_API_KEY>
```

Why GitHub links?

- nuget.org rewrites or resolves relative links inside packaged README files; linking to `Docs/README.md` from the packaged README may redirect to the package landing page instead of the repository docs. Using absolute GitHub URLs avoids that.

If you want, I can update the README further to include a short embedded QuickStart snippet suitable for display on nuget.org (small, single example) and keep the deeper documentation in `Docs/` on GitHub.

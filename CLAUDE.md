# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Commands
- **Build**: `dotnet build` – compile the solution.
- **Run**: `dotnet run` – start the API (dev URLs: https://localhost:5001, http://localhost:5000).
- **Watch**: `dotnet watch run` – hot‑reload during development.
- **Test**: `dotnet test` – execute any test projects (none are present yet, but the command is ready).
- **Format**: `dotnet format` – apply the default .NET formatter.
- **Swagger UI**: after running in Development, browse to `/swagger`.
- **Single request example** (from README):
  ```bash
  curl -X POST https://localhost:5001/chat \
    -H "Content-Type: application/json" \
    -d '{"prompt": "Hello!"}'
  ```

## High‑Level Architecture
- **Program.cs** – entry point; configures services, Swagger, caching, and registers HTTP clients for AI providers.
- **Controllers** – thin HTTP layers (`ChatController.cs`, `WeatherForecastController.cs`) that forward to service interfaces.
- **Service Layer** – interfaces (`IOpenRouterService`, `INvidiaNimService`, `IOllamaService`) with concrete implementations (`OpenRouterService`, `NvidiaNimService`, `OllamaService`). These encapsulate HTTP calls to external AI APIs.
- **Options** – strongly‑typed configuration objects (`OpenRouterOptions`, `NvidiaNimOptions`) bound from `appsettings.json`.
- **AI Integration** – uses `Microsoft.Extensions.AI` abstractions with provider‑specific packages (`Microsoft.Extensions.AI.OpenAI`, `Microsoft.Extensions.AI.Ollama`).
- **Swagger / OpenAPI** – auto‑generated via Swashbuckle, exposing the API contract.
- **Graphify** – knowledge‑graph lives under `graphify-out/`; keep it up‑to‑date with `graphify update .` after code changes.

## Development Guidelines
- Target framework: **net10.0** (compatible with the latest .NET SDK).
- Secrets are stored in `appsettings.json` and scanned by **Gitleaks** (see `.github/workflows/gitleaks.yml`). Never commit real keys.
- When adding a new AI service, follow the existing pattern: create an interface, a concrete class, register it with `AddHttpClient` in `Program.cs`, and inject it via constructor.
- Run `graphify update .` after any modification to keep the graph current.

## Graphify Rules (from existing section)
- Use `graphify query "<question>"` when `graphify-out/graph.json` is present.
- Use `graphify path "<A>" "<B>"` to explore relationships.
- Use `graphify explain "<concept>"` for focused concepts.
- Prefer `graphify-out/wiki/index.md` for broad navigation.
- Read `graphify-out/GRAPH_REPORT.md` only if scoped queries lack context.
- After code changes, run `graphify update .`.


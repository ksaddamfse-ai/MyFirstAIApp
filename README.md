# MyFirstAIApp

[![build](https://github.com/ksaddamfse-ai/MyFirstAIApp/actions/workflows/ci.yml/badge.svg)](https://github.com/ksaddamfse-ai/MyFirstAIApp/actions/workflows/ci.yml)

**Unified AI provider abstraction for .NET** — swap between OpenRouter, Ollama, and Nvidia NIM by changing a config value. Zero code changes. Built on `Microsoft.Extensions.AI.IChatClient`.

```
docker run -p 8080:8080 \
  -e ProviderRegistry__OpenRouter__ApiKey=sk-... \
  myfirstaiapp
```

Providers: **OpenRouter**, **Ollama** (development only), **Nvidia NIM**.

## Why

Every AI provider has a different SDK, auth scheme, and API shape. `Microsoft.Extensions.AI` (`IChatClient`) solves that with a common abstraction — but wiring up multiple providers still means repetitive `Program.cs` code.

This project shows a **config-driven** approach: drop a JSON block in `appsettings.json`, and the provider is auto-registered in DI, auto-wired to the chat and benchmark endpoints, and auto-populated in the Swagger dropdown. Adding a new provider takes 10 seconds and zero C# changes.

## Features

- **Multi-model per provider** — configure multiple models per provider (e.g. `openrouter/free`, `google/gemma-4-31b-it:free`)
- **Config-driven registration** — `ProviderRegistry` in `appsettings.json` drives DI registration via a single loop
- **Enable/disable** — per-provider toggle at runtime, no code needed
- **Environment-aware** — Ollama lives in `appsettings.Development.json`, never shipped to production
- **Swagger dropdown** — provider and model dropdowns auto-populated from the registry
- **Chat API** — `POST /api/chat?provider=X&model=Y` with `IChatClient` pipeline (logging, OpenTelemetry, retry)
- **Benchmark API** — compare latency across providers via JSON body with structured provider+model pairs
- **Automatic retry** — `ClientRetryPolicy` (exponential backoff on 429/5xx) for all OpenAI-compatible providers
- **Health endpoint** — `GET /health` returns `{ status: "healthy" }`
- **OpenTelemetry** — tracing instrumentation for ASP.NET Core, HTTP client, and AI pipeline
- **Secret scanning** — Gitleaks on push/PR to `main`
- **Tests** — 23 xunit tests (17 unit + 6 integration)

## Getting Started

### Prerequisites

- .NET 10 SDK or Docker
- API key for OpenRouter and/or Nvidia NIM (optional — providers without a key are skipped)
- [Ollama](https://ollama.ai) running locally (optional)

### Run with Docker

```bash
docker build -t myfirstaiapp .
docker run -p 8080:8080 \
  -e ProviderRegistry__OpenRouter__ApiKey=sk-... \
  -e ProviderRegistry__NvidiaNim__ApiKey=sk-... \
  myfirstaiapp
```

### Run with .NET CLI

```bash
dotnet run
```

### Configure

**`appsettings.json`** — remote-only providers:
```json
"ProviderRegistry": {
    "OpenRouter": {
        "Enabled": true, "Type": "OpenAI",
        "ApiKey": "your-openrouter-api-key",
        "BaseUrl": "https://openrouter.ai/api/v1",
        "Models": ["openrouter/free", "google/gemma-4-31b-it:free"]
    },
    "NvidiaNim": {
        "Enabled": true, "Type": "OpenAI",
        "ApiKey": "your-nvidia-nim-api-key",
        "BaseUrl": "https://integrate.api.nvidia.com/v1",
        "Models": ["meta/llama-3.3-70b-instruct"]
    }
}
```

**`appsettings.Development.json`** — local-only providers:
```json
"ProviderRegistry": {
    "Ollama": {
        "Enabled": true, "Type": "Ollama",
        "BaseUrl": "http://localhost:11434",
        "Models": ["llama3"]
    }
}
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET`  | `/health` | Health check |
| `POST` | `/api/chat?question=...&provider=...&model=...` | Chat with a provider+model (defaults to OpenRouter, openrouter/free) |
| `GET`  | `/api/benchmark/providers` | List available `Provider__Model` targets |
| `POST` | `/api/benchmark` | Benchmark one or more targets via JSON body |

### Chat

```bash
curl -X POST "http://localhost:5184/api/chat?question=Hello&provider=OpenRouter&model=openrouter/free"
```

### Benchmark

```bash
# All enabled providers
curl -X POST http://localhost:5184/api/benchmark \
  -H "Content-Type: application/json" \
  -d '{"question":"Hello"}'

# Specific targets
curl -X POST http://localhost:5184/api/benchmark \
  -H "Content-Type: application/json" \
  -d '{
    "question": "Hello",
    "targets": [
      {"provider": "OpenRouter", "model": "openrouter/free"},
      {"provider": "Ollama", "model": "llama3"}
    ]
  }'
```

## Project Structure

```
MyFirstAIApp/
├── Controllers/        # ChatController, BenchmarkController
├── Services/           # IBenchmarkService / BenchmarkService, IChatClientFactory / ChatClientFactory
├── Models/             # BenchmarkEntry, BenchmarkRequest, ProviderTarget, ResolveClientResult
├── Settings/           # ProviderRegistryEntry (maps appsettings.json)
├── Filters/            # ProviderDropdownFilter (Swagger dropdown for provider, model)
├── Tests/              # xunit + Moq (23 tests across services and controllers)
├── Program.cs          # Config-driven provider registration loop + pipeline
├── Dockerfile          # Multi-stage build
└── appsettings*.json   # ProviderRegistry config
```

## How to Add a Provider

Add this block to `ProviderRegistry` in `appsettings.json`:

```json
"Groq": {
    "Enabled": true, "Type": "OpenAI",
    "ApiKey": "your-groq-api-key",
    "BaseUrl": "https://api.groq.com/openai/v1",
    "Models": ["mixtral-8x7b-32768"]
}
```

That's it. Registration, DI, Swagger dropdown, chat, benchmark, enable/disable — all derived automatically. No `Program.cs` changes.

### Provider Types

| Type | Implementation | ApiKey required |
|------|----------------|-----------------|
| `OpenAI` | `OpenAIClient` + `AsChatClient()` | Yes |
| `Ollama` | `OllamaChatClient` | No |

### Lock a Provider to Development Only

Put it in `appsettings.Development.json` instead of `appsettings.json`. It won't be available in production.

## Architecture

```
appsettings.json ──> ProviderRegistry ──> Program.cs loop ──> Keyed IChatClient (Provider__Model)
                                               │
                                               ├──> ChatController (validates provider + model against registry)
                                               │      └── ResolveClient builds composite key for DI lookup
                                               │
                                               └──> BenchmarkService (filters Enabled providers)
                                                        │
                                                        ├──> POST /api/benchmark [JSON body]
                                                        │      └── ProviderTarget → composite key → DI lookup
                                                        └──> ProviderDropdownFilter (Swagger)
```

## AI Providers

Each `Provider__Model` combination is registered as a separate keyed service:

| Keyed Service Example | Provider | Model |
|-----------------------|----------|-------|
| `OpenRouter__openrouter/free` | OpenRouter | openrouter/free |
| `OpenRouter__google/gemma-4-31b-it:free` | OpenRouter | google/gemma-4-31b-it:free |
| `Ollama__llama3` | Ollama | llama3 |
| `NvidiaNim__meta/llama-3.3-70b-instruct` | Nvidia NIM | meta/llama-3.3-70b-instruct |

## OpenTelemetry

Traces are instrumented for:
- **ASP.NET Core** — every HTTP request span
- **HTTP client** — outbound AI provider calls
- **IChatClient pipeline** — per-request AI spans via `UseOpenTelemetry()`

Add an exporter in `Program.cs` to view traces:

```csharp
// Already in the tracing pipeline, just add exporter:
tracing.AddConsoleExporter();  // dev: console output
// or
tracing.AddZipkinExporter();   // production: Zipkin/Jaeger
```

## Secret Scanning

Gitleaks runs on pushes to `main` and PRs targeting `main`.

```bash
gitleaks detect --source . --verbose
```

## License

MIT

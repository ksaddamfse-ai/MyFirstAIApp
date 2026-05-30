# MyFirstAIApp

.NET 10 Web API showcasing `Microsoft.Extensions.AI.IChatClient` with multi-provider support.
Config-driven provider registration — add a provider by dropping a JSON block, zero code changes.

Providers: **OpenRouter**, **Ollama** (development only), **Nvidia NIM**.

## Features

- **Config-driven registration** — `ProviderRegistry` in `appsettings.json` drives DI registration via a single loop
- **Enable/disable** — per-provider toggle at runtime, no code needed
- **Environment-aware** — Ollama lives in `appsettings.Development.json`, never shipped to production
- **Swagger dropdown** — provider dropdown auto-populated from the registry
- **Chat API** — `POST /api/chat` with dynamic provider selection
- **Benchmark API** — compare latency across providers with `POST /api/benchmark`
- **Secret scanning** — Gitleaks on push/PR to `main`

## Getting Started

### Prerequisites

- .NET 10 SDK
- API key for OpenRouter and/or Nvidia NIM (optional — providers without a key are skipped)
- [Ollama](https://ollama.ai) running locally (optional)

### Configure

**`appsettings.json`** — remote-only providers:
```json
"ProviderRegistry": {
    "OpenRouter": {
        "Enabled": true, "Type": "OpenAI",
        "ApiKey": "your-openrouter-api-key",
        "BaseUrl": "https://openrouter.ai/api/v1",
        "ModelName": "openrouter/free"
    },
    "NvidiaNim": {
        "Enabled": true, "Type": "OpenAI",
        "ApiKey": "your-nvidia-nim-api-key",
        "BaseUrl": "https://integrate.api.nvidia.com/v1",
        "ModelName": "meta/llama-3.3-70b-instruct"
    }
}
```

**`appsettings.Development.json`** — local-only providers:
```json
"ProviderRegistry": {
    "Ollama": {
        "Enabled": true, "Type": "Ollama",
        "BaseUrl": "http://localhost:11434",
        "ModelName": "llama3"
    }
}
```

### Run

```bash
dotnet run
```

App starts at `https://localhost:7164` and `http://localhost:5184`. Swagger at `/swagger`.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/chat?question=...&provider=...` | Chat with a provider (defaults to OpenRouter) |
| `GET`  | `/api/benchmark/providers` | List enabled providers |
| `POST` | `/api/benchmark?question=...&providers=...` | Benchmark one or more providers |

### Demo

```bash
# List available providers
curl http://localhost:5184/api/benchmark/providers

# Chat with OpenRouter
curl -X POST "http://localhost:5184/api/chat?question=Hello&provider=OpenRouter"

# Benchmark all enabled providers
curl -X POST "http://localhost:5184/api/benchmark?question=Hello"
```

## Project Structure

```
Controllers/        # ChatController.cs, BenchmarkController.cs
Services/           # IBenchmarkService / BenchmarkService
Models/             # ProviderRegistryEntry, ProviderInfo, BenchmarkEntry
Filters/            # ProviderDropdownFilter.cs (Swagger dropdown)
Program.cs          # ~30 lines — registration loop + pipeline
appsettings.json    # ProviderRegistry (remote providers)
appsettings.Development.json  # ProviderRegistry (local-only providers)
```

## How to Add a Provider

Add this block to `ProviderRegistry` in `appsettings.json`:

```json
"Groq": {
    "Enabled": true, "Type": "OpenAI",
    "ApiKey": "your-groq-api-key",
    "BaseUrl": "https://api.groq.com/openai/v1",
    "ModelName": "mixtral-8x7b-32768"
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
appsettings.json ──> ProviderRegistry ──> Program.cs loop ──> Keyed IChatClient
                                              │
                                              ├──> ChatController (validates Enabled flag)
                                              └──> BenchmarkService (filters Enabled providers)
                                                       │
                                                       └──> ProviderDropdownFilter (Swagger)
```

## AI Providers

| Keyed Service | Provider | Implementation |
|---------------|----------|----------------|
| `OpenRouter` | OpenRouter | `OpenAIClient` SDK via MEAI |
| `Ollama` | Ollama | `OllamaChatClient` |
| `NvidiaNim` | Nvidia NIM | `OpenAIClient` SDK via MEAI |

## Secret Scanning

Gitleaks runs on pushes to `main` and PRs targeting `main`.

```bash
gitleaks detect --source . --verbose
```

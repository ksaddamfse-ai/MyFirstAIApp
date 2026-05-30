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

- **Config-driven registration** — `ProviderRegistry` in `appsettings.json` drives DI registration via a single loop
- **Enable/disable** — per-provider toggle at runtime, no code needed
- **Environment-aware** — Ollama lives in `appsettings.Development.json`, never shipped to production
- **Swagger dropdown** — provider dropdown auto-populated from the registry
- **Chat API** — `POST /api/chat` with dynamic provider selection
- **Benchmark API** — compare latency across providers with `POST /api/benchmark`
- **Secret scanning** — Gitleaks on push/PR to `main`
- **Tests** — 17 xunit tests across services and controllers

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
MyFirstAIApp/
├── Controllers/        # ChatController, BenchmarkController
├── Services/           # IBenchmarkService / BenchmarkService
├── Models/             # ProviderInfo, BenchmarkEntry
├── Settings/           # ProviderRegistryEntry (maps appsettings.json)
├── Filters/            # ProviderDropdownFilter (Swagger)
├── Tests/              # xunit + Moq (17 tests, 1:1 with production files)
├── Program.cs          # ~30 lines — registration loop + pipeline
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

## License

MIT

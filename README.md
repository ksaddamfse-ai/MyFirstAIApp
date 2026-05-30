# MyFirstAIApp

.NET 10 Web API integrating OpenRouter, Ollama, and Nvidia NIM via `Microsoft.Extensions.AI`.

## Features

- **Chat API** – POST to `/api/chat` to interact with AI models
- **Benchmark API** – Compare response times across providers
- **Multi-Provider** – OpenRouter, Ollama, Nvidia NIM via `IChatClient`
- **Secret Scanning** – Gitleaks GitHub Action on push/PR to `main`

## Getting Started

1. **Prerequisites**
   - .NET 10 SDK
   - API keys for OpenRouter and/or Nvidia NIM (optional)
   - Ollama running locally (optional)

2. **Configure**
   Edit `appsettings.json`:
   ```json
   "OpenRouter": { "ApiKey": "your-key", "ModelName": "openrouter/free" }
   "Ollama":     { "BaseUrl": "http://localhost:11434", "ModelName": "llama3" }
   "NvidiaNim":  { "ApiKey": "nvapi-your-key", "ModelName": "meta/llama-3.3-70b-instruct" }
   ```

3. **Run**
   ```bash
   dotnet run
   ```
   App starts at `https://localhost:7164` and `http://localhost:5184`. Swagger at `/swagger`.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/chat?question=...` | Chat with AI |
| `POST` | `/api/chat/myai?question=...` | Chat with AI (alternate) |
| `GET`  | `/api/chat/benchmark/providers` | List available providers |
| `POST` | `/api/chat/benchmark?question=...&providers=...` | Run benchmark |

## Project Structure

```
Controllers/        # API endpoints (ChatController.cs)
Services/           # IMyAiService / MyAiService (wraps IChatClient)
                    # IBenchmarkService / BenchmarkService
Clients/            # Custom IChatClient implementations (OpenRouterAPIClient.cs)
Models/             # BenchmarkEntry, BenchmarkResult, ProviderInfo, BenchmarkOptions
Program.cs          # DI registration of AI providers
appsettings.json    # API keys (placeholder: "API-KEY")
```

## AI Providers

| Keyed Service | Provider | Implementation |
|---------------|----------|----------------|
| `OpenRouterOpenAI` | OpenRouter | `OpenAIClient` SDK |
| `Ollama` | Ollama | `OllamaChatClient` |
| `NvidiaNimOpenAI` | Nvidia NIM | `OpenAIClient` SDK |

## Secret Scanning

Gitleaks runs on pushes to `main` and PRs targeting `main`.

Run locally:
```bash
gitleaks detect --source . --verbose
```

## License

MIT

# MyFirstAIApp

.NET 10 Web API integrating OpenRouter, Ollama, and Nvidia NIM via `Microsoft.Extensions.AI` (`IChatClient`).

## Build & Run

```powershell
dotnet build
dotnet run
```

App listens on `https://localhost:7164` and `http://localhost:5184`. Swagger at `/swagger`.

## Test

23 xunit tests (17 unit + 6 integration) in `Tests/` folder at repo root.

## Project Structure

```
Controllers/        # API endpoints (ChatController.cs, BenchmarkController.cs)
Services/           # IBenchmarkService / BenchmarkService
Models/             # ProviderModels, BenchmarkEntry, BenchmarkRequest
Settings/           # ProviderRegistryEntry
Filters/            # ProviderDropdownFilter.cs (Swagger dropdown)
Program.cs          # DI registration of AI providers and pipeline
appsettings.json    # ProviderRegistry (remote providers)
appsettings.Development.json  # ProviderRegistry (local-only providers)
```

## AI Providers (DI)

Each `Provider__Model` combo registered as keyed `IChatClient`. Consumption via `[FromKeyedServices("Provider__Model")]`.

| Keyed Service Example | Provider | How |
|---|---|---|
| `OpenRouter__openrouter/free` | OpenRouter | `OpenAIClient` SDK via MEAI |
| `Ollama__llama3` | Ollama | `OllamaChatClient` |
| `NvidiaNim__meta/llama-3.3-70b-instruct` | Nvidia NIM | `OpenAIClient` SDK via MEAI |

## API Endpoints

- `POST /api/chat?question=...&provider=...&model=...` — calls `IChatClient` (default: OpenRouter, openrouter/free)
- `GET  /api/benchmark/providers` — list available `Provider__Model` targets
- `POST /api/benchmark` — benchmark targets (JSON body with `{ question, targets? }`)

## Config

Edit `appsettings.json`:
```json
"ProviderRegistry": {
    "OpenRouter": {
        "Enabled": true, "Type": "OpenAI",
        "ApiKey": "your-key",
        "BaseUrl": "https://openrouter.ai/api/v1",
        "Models": ["openrouter/free", "google/gemma-4-31b-it:free"]
    }
}
```

**Never commit real API keys.** Gitleaks runs on push/PR to `main`.

## Coding Conventions

- File-scoped namespaces (no braces)
- `MyFirstAIApp` root namespace
- Implicit usings enabled
- Nullable reference types enabled
- No comments on code unless explaining *why* not *what*
- Async all the way: suffix async methods with `Async`
- All services via interfaces (e.g. `IBenchmarkService` / `BenchmarkService`)
- Prefer primary constructors where appropriate
- Use `Microsoft.Extensions.AI.IChatClient` for AI provider abstraction
- Register providers as keyed services via `AddKeyedChatClient`
- Private methods go last in the file

## Karpathy Guidelines

Behavioral guidelines to reduce common LLM coding mistakes, derived from [Andrej Karpathy's observations](https://x.com/karpathy/status/2015883857489522876) on LLM coding pitfalls.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

### 1. Think Before Coding
**Don't assume. Don't hide confusion. Surface tradeoffs.**

- State assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them — don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First
**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If 200 lines could be 50, rewrite it.

Ask: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes
**Touch only what you must. Clean up only your own mess.**

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it — don't delete it.
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

### 4. Goal-Driven Execution
**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

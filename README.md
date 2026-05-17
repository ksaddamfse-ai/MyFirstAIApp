# MyFirstAIApp

A .NET 9 Web API demonstrating AI integration with OpenRouter, featuring a Chat endpoint and secret scanning via Gitleaks.

## Features

- **Chat API** – POST to `/chat` to interact with OpenRouter AI models.
- **Weather Forecast** – Standard weather forecast endpoint at `/weatherforecast`.
- **Secret Scanning** – Gitleaks GitHub Action runs on every push and PR to `main`.
- **Clean Architecture** – Separation of concerns with service interfaces and implementations.

## Project Structure

```
MyFirstAIApp/
├── Controllers/
│   ├── ChatController.cs
│   └── WeatherForecastController.cs
├── Services/
│   ├── IOpenRouterService.cs
│   ├── OpenRouterService.cs
│   ├── IOllamaService.cs
│   └── OllamaService.cs
├── OpenRouterOptions.cs
├── Program.cs
├── appsettings.json
└── Properties/
    └── launchSettings.json
```

## Getting Started

1. **Prerequisites**
   - .NET 9 SDK
   - Valid OpenRouter API key

2. **Configure**
   Replace `"REDACTED"` in `appsettings.json` with your actual OpenRouter API key:
   ```json
   "OpenRouter": {
     "ApiKey": "your-api-key-here"
   }
   ```

3. **Run**
   ```bash
   dotnet run
   ```
   App starts at `https://localhost:5001` and `http://localhost:5000`.

4. **Test Chat Endpoint**
   ```bash
   curl -X POST https://localhost:5001/chat \
     -H "Content-Type: application/json" \
     -d '{"prompt": "Hello!"}'
   ```

## Secret Scanning

This repo uses **Gitleaks** to detect secrets in code. The workflow (`.github/workflows/gitleaks.yml`) runs automatically on:
- Pushes to `main`
- Pull requests targeting `main`

To run Gitleaks locally:
```bash
gitleaks detect --source . --verbose
```

## Contributing

1. Create a feature branch from `main`.
2. Make changes and ensure no secrets are committed.
3. Open a pull request – Gitleaks will scan automatically.

## License

MIT

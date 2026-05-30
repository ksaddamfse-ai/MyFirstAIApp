using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using MyFirstAIApp.Models;

namespace MyFirstAIApp.Services;

public class BenchmarkService : IBenchmarkService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BenchmarkOptions _options;
    private readonly ILogger<BenchmarkService> _logger;
    private static readonly string[] AllProviderKeys = ["OpenRouterOpenAI", "Ollama", "NvidiaNimOpenAI"];

    public BenchmarkService(IServiceProvider serviceProvider, IOptions<BenchmarkOptions> options, ILogger<BenchmarkService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public List<ProviderInfo> GetAvailableProviders()
    {
        var keys = _options.ProviderKeys.Count > 0 ? [.. _options.ProviderKeys] : AllProviderKeys;

        var providers = new List<ProviderInfo>();

        foreach (var key in keys)
        {
            var client = _serviceProvider.GetKeyedService<IChatClient>(key);
            if (client is null)
                continue;

            var metadata = client.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;
            providers.Add(new ProviderInfo
            {
                Key = key,
                ProviderName = metadata?.ProviderName ?? key,
                ModelId = _options.ProviderModels.TryGetValue(key, out var m) ? m : "unknown"
            });
        }

        return providers;
    }

    public async Task<List<BenchmarkEntry>> RunBenchmarkAsync(string question, string[]? providerKeys, CancellationToken cancellationToken = default)
    {
        var keys = providerKeys is { Length: > 0 } ? providerKeys
            : _options.ProviderKeys.Count > 0 ? [.. _options.ProviderKeys] : AllProviderKeys;

        var tasks = keys.Select(key => RunSingleAsync(key, question, cancellationToken));
        var entries = await Task.WhenAll(tasks);
        return [.. entries.OfType<BenchmarkEntry>()];
    }

    private async Task<BenchmarkEntry?> RunSingleAsync(string key, string question, CancellationToken cancellationToken)
    {
        var client = _serviceProvider.GetKeyedService<IChatClient>(key);
        if (client is null)
        {
            _logger.LogWarning("Provider {Key} not registered, skipping", key);
            return null;
        }

        var metadata = client.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;
        var modelId = _options.ProviderModels.TryGetValue(key, out var m) ? m : "unknown";
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.GetResponseAsync(question, cancellationToken: cancellationToken);
            sw.Stop();

            _logger.LogInformation("Benchmark {Key} OK ({Ms}ms)", key, sw.ElapsedMilliseconds);

            return new BenchmarkEntry
            {
                Provider = key,
                ProviderName = metadata?.ProviderName ?? key,
                Model = modelId,
                Success = true,
                Response = response?.Text,
                LatencyMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogWarning(ex, "Benchmark {Key} FAILED ({Ms}ms)", key, sw.ElapsedMilliseconds);

            return new BenchmarkEntry
            {
                Provider = key,
                ProviderName = metadata?.ProviderName ?? key,
                Model = modelId,
                Success = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
}

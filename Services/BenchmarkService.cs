using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using MyFirstAIApp.Models;
using MyFirstAIApp.Settings;

namespace MyFirstAIApp.Services;

public class BenchmarkService : IBenchmarkService
{
    private readonly IChatClientFactory _clientFactory;
    private readonly Dictionary<string, ProviderRegistryEntry> _registry;
    private readonly ILogger<BenchmarkService> _logger;

    public BenchmarkService(
        IChatClientFactory clientFactory,
        IOptions<Dictionary<string, ProviderRegistryEntry>> registry,
        ILogger<BenchmarkService> logger)
    {
        _clientFactory = clientFactory;
        _registry = registry.Value;
        _logger = logger;
    }

    public List<ProviderInfo> GetAvailableProviders()
    {
        var providers = new List<ProviderInfo>();

        foreach (var (key, entry) in _registry)
        {
            if (!entry.Enabled) continue;

            var client = _clientFactory.GetClient(key);
            if (client is null) continue;

            providers.Add(new ProviderInfo
            {
                Key = key,
                ModelId = entry.ModelName
            });
        }

        return providers;
    }

    public async Task<List<BenchmarkEntry>> RunBenchmarkAsync(string question, string[]? providerKeys, CancellationToken cancellationToken = default)
    {
        IEnumerable<string> keys;
        if (providerKeys is { Length: > 0 })
            keys = providerKeys;
        else
            keys = _registry.Where(e => e.Value.Enabled).Select(e => e.Key);

        var tasks = keys.Select(key => RunSingleAsync(key, question, cancellationToken));
        var entries = await Task.WhenAll(tasks);
        return [.. entries];
    }

    private async Task<BenchmarkEntry> RunSingleAsync(string key, string question, CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient(key);
        if (client is null)
        {
            _logger.LogWarning("Provider {Key} not registered, skipping", key);
            return new BenchmarkEntry
            {
                Provider = key,
                Model = "unknown",
                Success = false,
                Error = $"Provider '{key}' not registered"
            };
        }

        var modelId = _registry.TryGetValue(key, out var entry) ? entry.ModelName : "unknown";
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.GetResponseAsync(question, cancellationToken: cancellationToken);
            sw.Stop();

            _logger.LogInformation("Benchmark {Key} OK ({Ms}ms)", key, sw.ElapsedMilliseconds);

            return new BenchmarkEntry
            {
                Provider = key,
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
                Model = modelId,
                Success = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
}

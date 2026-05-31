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

    public List<ProviderModels> GetAvailableProviders()
    {
        return _registry
            .Where(kvp => kvp.Value.Enabled)
            .Select(kvp => new ProviderModels
            {
                Name = kvp.Key,
                Models = kvp.Value.Models
                    .Where(m => _clientFactory.GetClient($"{kvp.Key}__{m}") is not null)
                    .ToList()
            })
            .Where(p => p.Models.Count > 0)
            .ToList();
    }

    public async Task<List<BenchmarkEntry>> RunBenchmarkAsync(string question, List<ProviderTarget>? targets, CancellationToken cancellationToken = default)
    {
        IEnumerable<string> keys;
        if (targets is { Count: > 0 })
            keys = targets.Select(t => $"{t.Provider}__{t.Model}");
        else
            keys = GetAvailableProviders()
                .SelectMany(p => p.Models.Select(m => $"{p.Name}__{m}"));

        var tasks = keys.Select(key => RunSingleAsync(key, question, cancellationToken));
        var entries = await Task.WhenAll(tasks);
        return [.. entries];
    }

    private async Task<BenchmarkEntry> RunSingleAsync(string target, string question, CancellationToken cancellationToken)
    {
        var parts = target.Split("__", 2);
        if (parts.Length != 2)
        {
            return new BenchmarkEntry
            {
                Provider = target,
                Model = "unknown",
                Success = false,
                Error = $"Invalid target format '{target}'. Expected 'Provider__Model'."
            };
        }

        var provider = parts[0];
        var model = parts[1];
        var client = _clientFactory.GetClient(target);
        if (client is null)
        {
            _logger.LogWarning("Provider {Provider} model {Model} not registered, skipping", provider, model);
            return new BenchmarkEntry
            {
                Provider = provider,
                Model = model,
                Success = false,
                Error = $"Provider '{provider}' model '{model}' not registered"
            };
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.GetResponseAsync(question, cancellationToken: cancellationToken);
            sw.Stop();

            _logger.LogDebug("Benchmark {Provider}/{Model} OK ({Ms}ms)", provider, model, sw.ElapsedMilliseconds);

            return new BenchmarkEntry
            {
                Provider = provider,
                Model = model,
                Success = true,
                Response = response?.Text,
                LatencyMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogWarning(ex, "Benchmark {Provider}/{Model} FAILED ({Ms}ms)", provider, model, sw.ElapsedMilliseconds);

            return new BenchmarkEntry
            {
                Provider = provider,
                Model = model,
                Success = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
}

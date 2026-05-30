using MyFirstAIApp.Models;

namespace MyFirstAIApp.Services;

public interface IBenchmarkService
{
    List<ProviderInfo> GetAvailableProviders();
    Task<List<BenchmarkEntry>> RunBenchmarkAsync(string question, string[]? providerKeys, CancellationToken cancellationToken = default);
}

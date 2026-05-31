using MyFirstAIApp.Models;

namespace MyFirstAIApp.Services;

public interface IBenchmarkService
{
    List<ProviderModels> GetAvailableProviders();
    Task<List<BenchmarkEntry>> RunBenchmarkAsync(string question, List<ProviderTarget>? targets, CancellationToken cancellationToken = default);
}

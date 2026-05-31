using MyFirstAIApp.Models;

namespace MyFirstAIApp.Services;

public interface IBenchmarkService
{
    List<string> GetAvailableProviders();
    Task<List<BenchmarkEntry>> RunBenchmarkAsync(string question, List<ProviderTarget>? targets, CancellationToken cancellationToken = default);
}

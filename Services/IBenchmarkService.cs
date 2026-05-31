using MyFirstAIApp.Models;

namespace MyFirstAIApp.Services;

public interface IBenchmarkService
{
    List<string> GetAvailableProviders();
    Task<List<BenchmarkEntry>> RunBenchmarkAsync(string question, string[]? targets, CancellationToken cancellationToken = default);
}

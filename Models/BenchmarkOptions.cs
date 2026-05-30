namespace MyFirstAIApp.Models;

public class BenchmarkOptions
{
    public List<string> ProviderKeys { get; init; } = [];
    public Dictionary<string, string> ProviderModels { get; init; } = [];
}

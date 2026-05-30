namespace MyFirstAIApp.Models;

public class BenchmarkEntry
{
    public string Provider { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Response { get; init; }
    public long LatencyMs { get; init; }
    public string? Error { get; init; }
}

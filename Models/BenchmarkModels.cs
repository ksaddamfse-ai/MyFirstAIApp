namespace MyFirstAIApp.Models;

public class ProviderInfo
{
    public string Key { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
}

public class BenchmarkResult
{
    public string Question { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public List<BenchmarkEntry> Entries { get; init; } = [];
}

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

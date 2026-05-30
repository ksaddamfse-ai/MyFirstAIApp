namespace MyFirstAIApp.Models;

public class BenchmarkResult
{
    public string Question { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public List<BenchmarkEntry> Entries { get; init; } = [];
}

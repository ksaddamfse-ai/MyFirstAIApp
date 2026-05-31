namespace MyFirstAIApp.Models;

public class BenchmarkRequest
{
    public string Question { get; set; } = "";
    public List<ProviderTarget>? Targets { get; set; }
}

public class ProviderTarget
{
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
}

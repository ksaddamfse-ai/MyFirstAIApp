namespace MyFirstAIApp.Models;

/// <summary>Request body for the benchmark endpoint.</summary>
public class BenchmarkRequest
{
    /// <summary>The prompt to send to each provider/model.</summary>
    public string Question { get; set; } = "";

    /// <summary>
    /// Optional list of provider/model targets. Leave null to benchmark all enabled providers.
    /// Use <c>GET /api/benchmark/providers</c> to discover available targets.
    /// </summary>
    public List<ProviderTarget>? Targets { get; set; }
}

/// <summary>A single provider/model pair to benchmark.</summary>
public class ProviderTarget
{
    /// <summary>Provider name (e.g. OpenRouter).</summary>
    public string Provider { get; set; } = "";

    /// <summary>Model name within the provider (e.g. openrouter/free).</summary>
    public string Model { get; set; } = "";
}

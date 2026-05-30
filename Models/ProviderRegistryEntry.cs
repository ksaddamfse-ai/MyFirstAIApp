namespace MyFirstAIApp.Models;

public class ProviderRegistryEntry
{
    public bool Enabled { get; init; } = true;
    public string Type { get; init; } = "OpenAI";
    public string? ApiKey { get; init; }
    public string BaseUrl { get; init; } = "";
    public string ModelName { get; init; } = "";
    public int Timeout { get; init; } = 60;
}

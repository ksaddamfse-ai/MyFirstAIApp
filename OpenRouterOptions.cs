using System.ComponentModel;

namespace MyFirstAIApp;

public class OpenRouterOptions
{
    [DisplayName("OpenRouter API Base URL")]
    public string? BaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    [DisplayName("OpenRouter API Key")]
    public string? ApiKey { get; set; }

    [DisplayName("OpenRouter Model Name")]
    public string? ModelName { get; set; } = "openrouter/free";

    [DisplayName("Timeout in seconds")]
    public int Timeout { get; set; } = 60;
}
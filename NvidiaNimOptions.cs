using System.ComponentModel;

namespace MyFirstAIApp;

public class NvidiaNimOptions
{
    [DisplayName("NVIDIA NIM API Base URL")]
    public string? BaseUrl { get; set; } = "https://integrate.api.nvidia.com/v1";

    [DisplayName("NVIDIA NIM API Key")]
    public string? ApiKey { get; set; }

    [DisplayName("NVIDIA NIM Model Name")]
    public string? ModelName { get; set; } = "meta/llama-3.1-405b-instruct";

    [DisplayName("Timeout in seconds")]
    public int Timeout { get; set; } = 60;
}
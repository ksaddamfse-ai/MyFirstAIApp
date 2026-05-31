using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MyFirstAIApp.Models;
using MyFirstAIApp.Services;
using MyFirstAIApp.Settings;

namespace MyFirstAIApp.Tests;

public class BenchmarkServiceTest
{
    private readonly Mock<IChatClient> _openRouterClient = new();
    private readonly Mock<IChatClient> _ollamaClient = new();
    private readonly Mock<IChatClientFactory> _factory = new();

    private Dictionary<string, ProviderRegistryEntry> CreateRegistry(bool ollamaEnabled = true)
    {
        return new()
        {
            ["OpenRouter"] = new()
            {
                Enabled = true,
                Type = "OpenAI",
                ApiKey = "sk-test",
                BaseUrl = "https://openrouter.ai/api/v1",
                Models = ["openrouter/free"]
            },
            ["Ollama"] = new()
            {
                Enabled = ollamaEnabled,
                Type = "Ollama",
                BaseUrl = "http://localhost:11434",
                Models = ["llama3"]
            }
        };
    }

    private void SetupFactory()
    {
        _factory.Setup(f => f.GetClient("OpenRouter", "openrouter/free")).Returns(_openRouterClient.Object);
        _factory.Setup(f => f.GetClient("Ollama", "llama3")).Returns(_ollamaClient.Object);
    }

    private BenchmarkService CreateService(Dictionary<string, ProviderRegistryEntry>? registry = null)
    {
        registry ??= CreateRegistry();
        SetupFactory();
        return new BenchmarkService(
            _factory.Object,
            Options.Create(registry),
            NullLogger<BenchmarkService>.Instance);
    }

    [Fact]
    public void GetAvailableProviders_ReturnsEnabledRegisteredProviders()
    {
        var service = CreateService();
        var result = service.GetAvailableProviders();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p == "OpenRouter__openrouter/free");
        Assert.Contains(result, p => p == "Ollama__llama3");
    }

    [Fact]
    public void GetAvailableProviders_SkipsDisabledProviders()
    {
        var registry = CreateRegistry(ollamaEnabled: false);
        var service = CreateService(registry);
        var result = service.GetAvailableProviders();

        Assert.Single(result);
        Assert.Equal("OpenRouter__openrouter/free", result[0]);
    }

    [Fact]
    public void GetAvailableProviders_SkipsProvidersNotInDi()
    {
        var registry = new Dictionary<string, ProviderRegistryEntry>
        {
            ["MissingProvider"] = new()
            {
                Enabled = true,
                Type = "OpenAI",
                ApiKey = "sk-test",
                BaseUrl = "https://example.com",
                Models = ["test"]
            }
        };
        var factory = new Mock<IChatClientFactory>();
        var service = new BenchmarkService(
            factory.Object,
            Options.Create(registry),
            NullLogger<BenchmarkService>.Instance);

        var result = service.GetAvailableProviders();

        Assert.Empty(result);
    }

    [Fact]
    public async Task RunBenchmarkAsync_RunsAllEnabledProvidersByDefault()
    {
        _openRouterClient
            .Setup(c => c.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => msgs.First().Text == "hello"),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "openrouter reply")));

        _ollamaClient
            .Setup(c => c.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => msgs.First().Text == "hello"),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ollama reply")));

        var service = CreateService();
        var results = await service.RunBenchmarkAsync("hello", null);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task RunBenchmarkAsync_RunsSpecifiedTargetsOnly()
    {
        _openRouterClient
            .Setup(c => c.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => msgs.First().Text == "hello"),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "openrouter reply")));

        var service = CreateService();
        var results = await service.RunBenchmarkAsync("hello", ["OpenRouter__openrouter/free"]);

        Assert.Single(results);
        Assert.Equal("OpenRouter", results[0].Provider);
        Assert.Equal("openrouter/free", results[0].Model);
        Assert.True(results[0].Success);
    }

    [Fact]
    public async Task RunBenchmarkAsync_ReturnsErrorEntryForUnregisteredTarget()
    {
        var service = CreateService();
        var results = await service.RunBenchmarkAsync("hello", ["NonExistent__model"]);

        Assert.Single(results);
        Assert.False(results[0].Success);
        Assert.Contains("NonExistent", results[0].Error);
    }

    [Fact]
    public async Task RunBenchmarkAsync_HandlesProviderException()
    {
        _openRouterClient
            .Setup(c => c.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => msgs.First().Text == "hello"),
                null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        var service = CreateService();
        var results = await service.RunBenchmarkAsync("hello", ["OpenRouter__openrouter/free"]);

        Assert.Single(results);
        Assert.False(results[0].Success);
        Assert.Equal("OpenRouter", results[0].Provider);
        Assert.Equal("openrouter/free", results[0].Model);
        Assert.Contains("API error", results[0].Error);
    }
}

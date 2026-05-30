using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MyFirstAIApp.Models;
using MyFirstAIApp.Settings;
using MyFirstAIApp.Services;

namespace MyFirstAIApp.Tests;

public class BenchmarkServiceTest
{
    private readonly Mock<IChatClient> _openRouterClient = new();
    private readonly Mock<IChatClient> _ollamaClient = new();

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
                ModelName = "openrouter/free"
            },
            ["Ollama"] = new()
            {
                Enabled = ollamaEnabled,
                Type = "Ollama",
                BaseUrl = "http://localhost:11434",
                ModelName = "llama3"
            }
        };
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IChatClient>("OpenRouter", _openRouterClient.Object);
        services.AddKeyedSingleton<IChatClient>("Ollama", _ollamaClient.Object);
        return services.BuildServiceProvider();
    }

    private BenchmarkService CreateService(Dictionary<string, ProviderRegistryEntry>? registry = null)
    {
        registry ??= CreateRegistry();
        return new BenchmarkService(
            CreateServiceProvider(),
            Options.Create(registry),
            NullLogger<BenchmarkService>.Instance);
    }

    [Fact]
    public void GetAvailableProviders_ReturnsEnabledRegisteredProviders()
    {
        var service = CreateService();
        var result = service.GetAvailableProviders();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Key == "OpenRouter");
        Assert.Contains(result, p => p.Key == "Ollama");
    }

    [Fact]
    public void GetAvailableProviders_SkipsDisabledProviders()
    {
        var registry = CreateRegistry(ollamaEnabled: false);
        var service = CreateService(registry);
        var result = service.GetAvailableProviders();

        Assert.Single(result);
        Assert.Equal("OpenRouter", result[0].Key);
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
                ModelName = "test"
            }
        };
        var service = CreateService(registry);
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
    public async Task RunBenchmarkAsync_RunsSpecifiedProvidersOnly()
    {
        _openRouterClient
            .Setup(c => c.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => msgs.First().Text == "hello"),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "openrouter reply")));

        var service = CreateService();
        var results = await service.RunBenchmarkAsync("hello", ["OpenRouter"]);

        Assert.Single(results);
        Assert.Equal("OpenRouter", results[0].Provider);
        Assert.True(results[0].Success);
    }

    [Fact]
    public async Task RunBenchmarkAsync_ReturnsErrorEntryForUnregisteredKey()
    {
        var service = CreateService();
        var results = await service.RunBenchmarkAsync("hello", ["NonExistent"]);

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
        var results = await service.RunBenchmarkAsync("hello", ["OpenRouter"]);

        Assert.Single(results);
        Assert.False(results[0].Success);
        Assert.Equal("OpenRouter", results[0].Provider);
        Assert.Contains("API error", results[0].Error);
    }
}

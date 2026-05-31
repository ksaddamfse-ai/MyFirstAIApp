using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MyFirstAIApp.Services;

namespace MyFirstAIApp.IntegrationTests;

public class MyFirstAIAppIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IChatClient> _mockClient = new();
    private readonly Mock<IChatClientFactory> _mockFactory = new();

    public MyFirstAIAppIntegrationTest()
    {
        _mockClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "mocked response")));

        _mockFactory
            .Setup(f => f.GetClient(It.IsAny<string>()))
            .Returns(_mockClient.Object);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ProviderRegistry:MockProvider:Enabled"] = "true",
                    ["ProviderRegistry:MockProvider:Type"] = "OpenAI",
                    ["ProviderRegistry:MockProvider:ApiKey"] = "sk-test",
                    ["ProviderRegistry:MockProvider:BaseUrl"] = "https://mock.api.com/v1",
                    ["ProviderRegistry:MockProvider:ModelName"] = "mock-model",
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IChatClientFactory>(_mockFactory.Object);
            });
        });
    }

    [Fact]
    public async Task GetProviders_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/benchmark/providers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var providers = JsonSerializer.Deserialize<JsonElement[]>(body);

        Assert.NotNull(providers);
        Assert.Contains(providers, p => p.GetProperty("key").GetString() == "MockProvider");
    }

    [Fact]
    public async Task Chat_ReturnsBadRequest_WhenQuestionMissing()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/chat", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Benchmark_ReturnsBadRequest_WhenQuestionMissing()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/benchmark", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Chat_ReturnsMockedResponse()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/chat?question=Hello&provider=MockProvider", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal("mocked response", text);
    }

    [Fact]
    public async Task Benchmark_ReturnsMockedResults()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/benchmark?question=Hi&providers=MockProvider", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<JsonElement[]>(body);

        Assert.NotNull(results);
        Assert.Single(results);
        Assert.True(results[0].GetProperty("success").GetBoolean());
        Assert.Equal("MockProvider", results[0].GetProperty("provider").GetString());
    }

    [Fact]
    public async Task Chat_ReturnsBadRequest_WhenProviderDisabled()
    {
        var disabledFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ProviderRegistry:DisabledProvider:Enabled"] = "false",
                    ["ProviderRegistry:DisabledProvider:Type"] = "OpenAI",
                    ["ProviderRegistry:DisabledProvider:ApiKey"] = "sk-test",
                    ["ProviderRegistry:DisabledProvider:BaseUrl"] = "https://mock.api.com/v1",
                    ["ProviderRegistry:DisabledProvider:ModelName"] = "mock-model",
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IChatClientFactory>(_mockFactory.Object);
            });
        });

        var client = disabledFactory.CreateClient();
        var response = await client.PostAsync("/api/chat?question=Hello&provider=DisabledProvider", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("disabled", body, StringComparison.OrdinalIgnoreCase);
    }
}

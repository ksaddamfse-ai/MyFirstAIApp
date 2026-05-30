using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MyFirstAIApp.IntegrationTests;

public class MyFirstAIAppIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MyFirstAIAppIntegrationTest()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ProviderRegistry:Ollama:Enabled"] = "true",
                    ["ProviderRegistry:Ollama:Type"] = "Ollama",
                    ["ProviderRegistry:Ollama:BaseUrl"] = "http://localhost:11434",
                    ["ProviderRegistry:Ollama:ModelName"] = "llama3",
                });
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
        Assert.Contains(providers, p => p.GetProperty("key").GetString() == "Ollama");
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
    public async Task ChatWithOllama_ReturnsResponse()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/chat?question=Say+hello&provider=Ollama", null);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("disabled", body, StringComparison.OrdinalIgnoreCase);
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(text);
    }

    [Fact]
    public async Task BenchmarkWithOllama_ReturnsResults()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/benchmark?question=Hi&providers=Ollama", null);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<JsonElement[]>(body);

        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }
}

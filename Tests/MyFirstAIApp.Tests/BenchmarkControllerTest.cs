using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Moq;
using MyFirstAIApp.Controllers;
using MyFirstAIApp.Models;
using MyFirstAIApp.Services;

namespace MyFirstAIApp.Tests;

public class BenchmarkControllerTest
{
    private readonly Mock<IBenchmarkService> _benchmarkService = new();
    private readonly List<ProviderInfo> _providers;

    public BenchmarkControllerTest()
    {
        _providers =
        [
            new() { Key = "OpenRouter", ModelId = "openrouter/free" },
            new() { Key = "Ollama", ModelId = "llama3" }
        ];
    }

    private BenchmarkController CreateController()
    {
        return new BenchmarkController(_benchmarkService.Object);
    }

    [Fact]
    public void GetProviders_ReturnsListFromService()
    {
        _benchmarkService
            .Setup(s => s.GetAvailableProviders())
            .Returns(_providers);

        var controller = CreateController();
        var result = controller.GetProviders();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<ProviderInfo>>(okResult.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void GetProviders_ReturnsEmpty_WhenNoProviders()
    {
        _benchmarkService
            .Setup(s => s.GetAvailableProviders())
            .Returns([]);

        var controller = CreateController();
        var result = controller.GetProviders();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<ProviderInfo>>(okResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task RunBenchmark_ReturnsBadRequest_WhenQuestionEmpty()
    {
        var controller = CreateController();
        var result = await controller.RunBenchmark("");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("question", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task RunBenchmark_ReturnsBadRequest_WhenQuestionWhitespace()
    {
        var controller = CreateController();
        var result = await controller.RunBenchmark("   ");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("question", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task RunBenchmark_ReturnsOkWithResults_WhenQuestionValid()
    {
        var entries = new List<BenchmarkEntry>
        {
            new() { Provider = "OpenRouter", Success = true, LatencyMs = 100 }
        };

        _benchmarkService
            .Setup(s => s.RunBenchmarkAsync("hello", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var controller = CreateController();
        var result = await controller.RunBenchmark("hello");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<BenchmarkEntry>>(okResult.Value);
        Assert.Single(list);
        Assert.True(list[0].Success);
    }

    [Fact]
    public async Task RunBenchmark_PassesProviderFilter()
    {
        var entries = new List<BenchmarkEntry>
        {
            new() { Provider = "OpenRouter", Success = true, LatencyMs = 50 }
        };

        _benchmarkService
            .Setup(s => s.RunBenchmarkAsync("hello", new[] { "OpenRouter" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var controller = CreateController();
        var result = await controller.RunBenchmark("hello", ["OpenRouter"]);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<BenchmarkEntry>>(okResult.Value);
        Assert.Single(list);
        Assert.Equal("OpenRouter", list[0].Provider);
    }
}

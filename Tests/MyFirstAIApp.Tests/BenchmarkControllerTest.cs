using Microsoft.AspNetCore.Mvc;
using Moq;
using MyFirstAIApp.Controllers;
using MyFirstAIApp.Models;
using MyFirstAIApp.Services;

namespace MyFirstAIApp.Tests;

public class BenchmarkControllerTest
{
    private readonly Mock<IBenchmarkService> _benchmarkService = new();
    private readonly List<ProviderModels> _providers;

    public BenchmarkControllerTest()
    {
        _providers =
        [
            new ProviderModels { Name = "OpenRouter", Models = ["openrouter/free"] },
            new ProviderModels { Name = "Ollama", Models = ["llama3"] }
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
        var list = Assert.IsType<List<ProviderModels>>(okResult.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal("OpenRouter", list[0].Name);
        Assert.Equal("llama3", list[1].Models[0]);
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
        var list = Assert.IsType<List<ProviderModels>>(okResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task RunBenchmark_ReturnsBadRequest_WhenQuestionEmpty()
    {
        var controller = CreateController();
        var result = await controller.RunBenchmark(new BenchmarkRequest());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("question", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task RunBenchmark_ReturnsBadRequest_WhenQuestionWhitespace()
    {
        var controller = CreateController();
        var result = await controller.RunBenchmark(new BenchmarkRequest { Question = "   " });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("question", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task RunBenchmark_ReturnsOkWithResults_WhenQuestionValid()
    {
        var entries = new List<BenchmarkEntry>
        {
            new() { Provider = "OpenRouter", Model = "openrouter/free", Success = true, LatencyMs = 100 }
        };

        _benchmarkService
            .Setup(s => s.RunBenchmarkAsync("hello", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var controller = CreateController();
        var result = await controller.RunBenchmark(new BenchmarkRequest { Question = "hello" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<BenchmarkEntry>>(okResult.Value);
        Assert.Single(list);
        Assert.True(list[0].Success);
    }

    [Fact]
    public async Task RunBenchmark_PassesTargetFilter()
    {
        var entries = new List<BenchmarkEntry>
        {
            new() { Provider = "OpenRouter", Model = "openrouter/free", Success = true, LatencyMs = 50 }
        };

        var request = new BenchmarkRequest
        {
            Question = "hello",
            Targets = [new ProviderTarget { Provider = "OpenRouter", Model = "openrouter/free" }]
        };

        _benchmarkService
            .Setup(s => s.RunBenchmarkAsync("hello",
                It.Is<List<ProviderTarget>>(t => t.Count == 1 && t[0].Provider == "OpenRouter"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var controller = CreateController();
        var result = await controller.RunBenchmark(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<BenchmarkEntry>>(okResult.Value);
        Assert.Single(list);
        Assert.Equal("OpenRouter", list[0].Provider);
        Assert.Equal("openrouter/free", list[0].Model);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MyFirstAIApp.Services;
using MyFirstAIApp.Settings;

namespace MyFirstAIApp.Tests;

public class ChatControllerTest
{
    private readonly Mock<IChatClient> _chatClient = new();
    private readonly Mock<IChatClientFactory> _factory = new();
    private readonly Dictionary<string, ProviderRegistryEntry> _registry;

    public ChatControllerTest()
    {
        _registry = new()
        {
            ["OpenRouter"] = new()
            {
                Enabled = true,
                Type = "OpenAI",
                ApiKey = "sk-test",
                BaseUrl = "https://openrouter.ai/api/v1",
                ModelName = "openrouter/free"
            },
            ["DisabledProvider"] = new()
            {
                Enabled = false,
                Type = "OpenAI",
                ApiKey = "sk-test",
                BaseUrl = "https://example.com",
                ModelName = "test"
            }
        };

        _factory
            .Setup(f => f.GetClient("OpenRouter"))
            .Returns(_chatClient.Object);
    }

    private ChatController CreateController()
    {
        var controller = new ChatController(
            NullLogger<ChatController>.Instance,
            Options.Create(_registry),
            _factory.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    [Fact]
    public async Task Ask_ReturnsOk_WhenProviderExistsAndEnabled()
    {
        _chatClient
            .Setup(c => c.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => msgs.First().Text == "hello"),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello there")));

        var controller = CreateController();
        var result = await controller.Ask("hello");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Hello there", okResult.Value);
    }

    [Fact]
    public async Task Ask_ReturnsBadRequest_WhenProviderDisabled()
    {
        var controller = CreateController();
        var result = await controller.Ask("hello", "DisabledProvider");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("disabled", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Ask_ReturnsBadRequest_WhenProviderNotFound()
    {
        var controller = CreateController();
        var result = await controller.Ask("hello", "NonExistent");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("disabled", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Ask_ReturnsEmptyString_WhenResponseTextNull()
    {
        _chatClient
            .Setup(c => c.GetResponseAsync(
                It.Is<IEnumerable<ChatMessage>>(msgs => msgs.First().Text == "hello"),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, null as string)));

        var controller = CreateController();
        var result = await controller.Ask("hello");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("", okResult.Value);
    }
}

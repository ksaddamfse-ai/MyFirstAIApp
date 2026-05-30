using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using MyFirstAIApp.Services;
using MyFirstAIApp.Settings;

namespace MyFirstAIApp;

[ApiController]
[Route("api/chat")]
public class ChatController(
    ILogger<ChatController> logger,
    IOptions<Dictionary<string, ProviderRegistryEntry>> registry,
    IChatClientFactory clientFactory) : ControllerBase
{
    private IChatClient? ResolveClient(string provider)
    {
        if (!registry.Value.TryGetValue(provider, out var entry) || !entry.Enabled)
            return null;

        return clientFactory.GetClient(provider);
    }

    [HttpPost]
    public async Task<IActionResult> Ask(
        [FromQuery] string question,
        [FromQuery] string provider = "OpenRouter",
        CancellationToken cancellationToken = default)
    {
        var client = ResolveClient(provider);
        if (client is null)
            return BadRequest($"Provider '{provider}' is disabled or not found");

        logger.LogInformation("Chat request ({Provider}): {Question}", provider, question);
        var response = await client.GetResponseAsync(question, cancellationToken: cancellationToken);
        return Ok(response?.Text);
    }

    [HttpPost("stream")]
    public async Task Stream(
        [FromQuery] string question,
        [FromQuery] string provider = "OpenRouter",
        CancellationToken cancellationToken = default)
    {
        var client = ResolveClient(provider);
        if (client is null)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync($"Provider '{provider}' is disabled or not found", cancellationToken);
            return;
        }

        logger.LogInformation("Stream request ({Provider}): {Question}", provider, question);

        HttpContext.Response.ContentType = "text/event-stream";
        HttpContext.Response.Headers.CacheControl = "no-cache";
        HttpContext.Response.Headers.Connection = "keep-alive";

        await foreach (var update in client.GetStreamingResponseAsync(question, cancellationToken: cancellationToken))
        {
            await HttpContext.Response.WriteAsync($"data: {update.Text}\n\n", cancellationToken);
            await HttpContext.Response.Body.FlushAsync(cancellationToken);
        }

        await HttpContext.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await HttpContext.Response.Body.FlushAsync(cancellationToken);
    }
}

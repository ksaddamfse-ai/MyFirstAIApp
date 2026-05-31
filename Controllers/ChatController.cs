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

    private IChatClient? ResolveClient(string provider)
    {
        if (!registry.Value.TryGetValue(provider, out var entry) || !entry.Enabled)
            return null;

        return clientFactory.GetClient(provider);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using MyFirstAIApp.Settings;

namespace MyFirstAIApp;

[ApiController]
[Route("api/chat")]
public class ChatController(
    ILogger<ChatController> logger,
    IOptions<Dictionary<string, ProviderRegistryEntry>> registry,
    IServiceProvider serviceProvider) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ask(
        [FromQuery] string question,
        [FromQuery] string provider = "OpenRouter",
        CancellationToken cancellationToken = default)
    {
        if (!registry.Value.TryGetValue(provider, out var entry) || !entry.Enabled)
            return BadRequest($"Provider '{provider}' is disabled");

        var client = serviceProvider.GetKeyedService<IChatClient>(provider);
        if (client is null)
            return BadRequest($"Provider '{provider}' not found.");

        logger.LogInformation("Chat request ({Provider}): {Question}", provider, question);
        var response = await client.GetResponseAsync(question, cancellationToken: cancellationToken);
        return Ok(response?.Text);
    }
}

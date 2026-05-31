using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using MyFirstAIApp.Models;
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
        [FromQuery] string model = "openrouter/free",
        CancellationToken cancellationToken = default)
    {
        var result = ResolveClient(provider, model);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        logger.LogDebug("Chat request ({Provider}/{Model}): {Question}", provider, model, question);
        var response = await result.Client!.GetResponseAsync(question, cancellationToken: cancellationToken);
        return Ok(response?.Text);
    }

    private ResolveClientResult ResolveClient(string provider, string model)
    {
        if (!registry.Value.TryGetValue(provider, out var entry))
            return new ResolveClientResult { Error = $"Provider '{provider}' not found in registry" };

        if (!entry.Enabled)
            return new ResolveClientResult { Error = $"Provider '{provider}' is disabled" };

        if (!entry.Models.Contains(model, StringComparer.OrdinalIgnoreCase))
            return new ResolveClientResult { Error = $"Provider '{provider}' does not have model '{model}'" };

        return new ResolveClientResult { Client = clientFactory.GetClient($"{provider}__{model}") };
    }
}

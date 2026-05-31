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
    /// <summary>Send a chat message to a specified AI provider and model.</summary>
    /// <param name="question">The user message to send.</param>
    /// <param name="provider">Provider name (e.g. OpenRouter). Defaults to OpenRouter.</param>
    /// <param name="model">Model name within the provider (e.g. openrouter/free). Defaults to openrouter/free.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI response text.</returns>
    /// <response code="200">Returns the AI response.</response>
    /// <response code="400">Provider or model is disabled, not found, or invalid.</response>
    [HttpPost]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
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

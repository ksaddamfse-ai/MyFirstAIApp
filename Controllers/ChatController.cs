using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace MyFirstAIApp;

[ApiController]
[Route("api/chat")]
public class ChatController(ILogger<ChatController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ask(
        [FromQuery] string question,
        [FromQuery] string provider = "OpenRouterOpenAI",
        CancellationToken cancellationToken = default)
    {
        var client = HttpContext.RequestServices.GetKeyedService<IChatClient>(provider);
        if (client is null)
            return BadRequest($"Provider '{provider}' not found.");
        
        logger.LogInformation("Chat request ({Provider}): {Question}", provider, question);
        var response = await client.GetResponseAsync(question, cancellationToken: cancellationToken);
        return Ok(response?.Text);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace MyFirstAIApp;

[ApiController]
[Route("api/chat")]
public class ChatController(ILogger<ChatController> logger, [FromKeyedServices("OpenRouterOpenAI")] IChatClient chatClient) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ask([FromQuery] string question, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received chat request: {Question}", question);
        var response = await chatClient.GetResponseAsync(question, cancellationToken: cancellationToken);
        logger.LogInformation("Returning answer (length {Length})", response?.Text?.Length ?? 0);
        return Ok(response?.Text);
    }
}

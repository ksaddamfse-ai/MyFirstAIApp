using Microsoft.AspNetCore.Mvc;
using MyFirstAIApp;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IOpenRouterService _ai;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IOpenRouterService ai, ILogger<ChatController> logger)
    {
        _ai = ai;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromQuery] string question)
    {
        _logger.LogInformation("Received chat request: {Question}", question);
        var answer = await _ai.AskAI(question);
        _logger.LogInformation("Returning answer (length {Length})", answer?.Length ?? 0);
        return Ok(answer);
    }
}
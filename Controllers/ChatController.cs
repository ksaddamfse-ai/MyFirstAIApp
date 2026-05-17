using Microsoft.AspNetCore.Mvc;
using MyFirstAIApp;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IOpenRouterService _openRouter;
    private readonly INvidiaNimService _nvidiaNim;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IOpenRouterService openRouter, INvidiaNimService nvidiaNim, ILogger<ChatController> logger)
    {
        _openRouter = openRouter;
        _nvidiaNim = nvidiaNim;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromQuery] string question)
    {
        _logger.LogInformation("Received chat request: {Question}", question);
        var answer = await _openRouter.AskAI(question);
        _logger.LogInformation("Returning answer (length {Length})", answer?.Length ?? 0);
        return Ok(answer);
    }

    [HttpPost("nvidia")]
    public async Task<IActionResult> AskNvidia([FromQuery] string question)
    {
        _logger.LogInformation("Received NVIDIA NIM chat request: {Question}", question);
        var answer = await _nvidiaNim.AskAI(question);
        _logger.LogInformation("Returning NVIDIA NIM answer (length {Length})", answer?.Length ?? 0);
        return Ok(answer);
    }
}
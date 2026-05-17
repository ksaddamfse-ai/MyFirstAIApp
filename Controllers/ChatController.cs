using Microsoft.AspNetCore.Mvc;
using MyFirstAIApp;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly INvidiaNimService _nvidiaNim;
    private readonly ILogger<ChatController> _logger;
    private readonly IMyAiService _myAiService;

    public ChatController(INvidiaNimService nvidiaNim, ILogger<ChatController> logger, IMyAiService myAiService)
    {
        _nvidiaNim = nvidiaNim;
        _logger = logger;
        _myAiService = myAiService;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromQuery] string question)
    {
        _logger.LogInformation("Received chat request: {Question}", question);
        var answer = await _myAiService.RunAsync(question);
        _logger.LogInformation("Returning answer (length {Length})", answer?.Length ?? 0);
        return Ok(answer);
    }

    [HttpPost("myai")]
    public async Task<IActionResult> AskMyAi([FromQuery] string question)
    {
        _logger.LogInformation("Received My AI chat request: {Question}", question);
        var answer = await _myAiService.RunAsync(question);
        _logger.LogInformation("Returning My AI answer (length {Length})", answer?.Length ?? 0);
        return Ok(answer);
    }
}
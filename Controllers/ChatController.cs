using Microsoft.AspNetCore.Mvc;
using MyFirstAIApp;
using MyFirstAIApp.Models;
using MyFirstAIApp.Services;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IMyAiService _myAiService;
    private readonly IBenchmarkService _benchmarkService;

    public ChatController(ILogger<ChatController> logger, IMyAiService myAiService, IBenchmarkService benchmarkService)
    {
        _logger = logger;
        _myAiService = myAiService;
        _benchmarkService = benchmarkService;
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

    [HttpGet("benchmark/providers")]
    public IActionResult GetProviders()
    {
        var providers = _benchmarkService.GetAvailableProviders();
        return Ok(providers);
    }

    [HttpPost("benchmark")]
    public async Task<IActionResult> RunBenchmark(
        [FromQuery] string question,
        [FromQuery] string? providers = null)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest("question is required");

        var providerKeys = providers?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var results = await _benchmarkService.RunBenchmarkAsync(question, providerKeys);
        return Ok(results);
    }
}
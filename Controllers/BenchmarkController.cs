using Microsoft.AspNetCore.Mvc;
using MyFirstAIApp.Services;

namespace MyFirstAIApp.Controllers;

[ApiController]
[Route("api/benchmark")]
public class BenchmarkController(IBenchmarkService benchmarkService) : ControllerBase
{
    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = benchmarkService.GetAvailableProviders();
        return Ok(providers);
    }

    [HttpPost]
    public async Task<IActionResult> RunBenchmark(
        [FromQuery] string question,
        [FromQuery] string[]? targets = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest("question is required");

        var results = await benchmarkService.RunBenchmarkAsync(question, targets, cancellationToken);
        return Ok(results);
    }
}

using Microsoft.AspNetCore.Mvc;
using MyFirstAIApp.Models;
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
        [FromBody] BenchmarkRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("question is required");

        var results = await benchmarkService.RunBenchmarkAsync(request.Question, request.Targets, cancellationToken);
        return Ok(results);
    }
}

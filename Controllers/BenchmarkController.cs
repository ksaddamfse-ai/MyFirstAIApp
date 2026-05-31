using Microsoft.AspNetCore.Mvc;
using MyFirstAIApp.Models;
using MyFirstAIApp.Services;

namespace MyFirstAIApp.Controllers;

[ApiController]
[Route("api/benchmark")]
public class BenchmarkController(IBenchmarkService benchmarkService) : ControllerBase
{
    /// <summary>List all enabled AI providers and their available models.</summary>
    /// <returns>An array of providers each with a list of models registered in DI.</returns>
    /// <response code="200">Returns the provider list.</response>
    [HttpGet("providers")]
    [ProducesResponseType<List<ProviderModels>>(StatusCodes.Status200OK)]
    public IActionResult GetProviders()
    {
        var providers = benchmarkService.GetAvailableProviders();
        return Ok(providers);
    }

    /// <summary>Run a latency benchmark against one or more AI providers.</summary>
    /// <remarks>
    /// Use <c>GET /api/benchmark/providers</c> to discover available provider/model targets.
    /// If <c>targets</c> is null or empty, all enabled providers are benchmarked.
    /// </remarks>
    /// <param name="request">Benchmark request with question and optional target list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of benchmark results with latency per provider/model.</returns>
    /// <response code="200">Returns benchmark results.</response>
    /// <response code="400">Question is required.</response>
    [HttpPost]
    [ProducesResponseType<List<BenchmarkEntry>>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
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

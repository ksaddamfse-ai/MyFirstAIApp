using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

public class OllamaService : IOllamaService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(IChatClient chatClient, ILogger<OllamaService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<string> AskAI(string prompt)
    {
        _logger.LogInformation("AskAI called with prompt: {Prompt}", prompt);

        var response = await _chatClient.GetResponseAsync(prompt);
        var text = response?.Text ?? string.Empty;

        _logger.LogInformation("AskAI received response: {ResponsePreview}", text.Length > 200 ? text[..200] + "..." : text);

        return text;
    }
}
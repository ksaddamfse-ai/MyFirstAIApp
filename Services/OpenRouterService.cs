using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyFirstAIApp
{
    public class OpenRouterService : IOpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouterService> _logger;
        private readonly OpenRouterOptions _options;

        public OpenRouterService(HttpClient httpClient, ILogger<OpenRouterService> logger, IOptions<OpenRouterOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;

            _httpClient.BaseAddress = new Uri((_options.BaseUrl ?? "https://openrouter.ai/api/v1").TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://MyFirstAIApp");
            _httpClient.DefaultRequestHeaders.Add("X-OpenRouter-Title", "MyFirstAIApp");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MyFirstAIApp/1.0");
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.Timeout);
        }

        public async Task<string> AskAI(string prompt)
        {
            var requestBody = new
            {
                model = _options.ModelName ?? "openrouter/free",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation("Sending request to OpenRouter API with model {Model}", _options.ModelName);

            using var response = await _httpClient.PostAsync("chat/completions", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenRouter API error: {StatusCode} - {Response}", response.StatusCode, responseContent.Substring(0, Math.Min(200, responseContent.Length)));
                return $"Error: {response.StatusCode}";
            }

            _logger.LogInformation("Received response from OpenRouter API (length {Length})", responseContent.Length);
            _logger.LogDebug("Response content: {Content}", responseContent.Substring(0, Math.Min(500, responseContent.Length)));

            try
            {
                var result = JsonSerializer.Deserialize<OpenRouterResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Choices?[0]?.Message?.Content ?? string.Empty;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON. Response starts with: {Start}", responseContent.Substring(0, Math.Min(50, responseContent.Length)));
                throw;
            }
        }
    }

    public class OpenRouterResponse
    {
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Content { get; set; }
    }
}

using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyFirstAIApp
{
    public class NvidiaNimService : INvidiaNimService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NvidiaNimService> _logger;
        private readonly NvidiaNimOptions _options;

        public NvidiaNimService(HttpClient httpClient, ILogger<NvidiaNimService> logger, IOptions<NvidiaNimOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;

            _httpClient.BaseAddress = new Uri((_options.BaseUrl ?? "https://integrate.api.nvidia.com/v1").TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MyFirstAIApp/1.0");
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.Timeout);
        }

        public async Task<string> AskAI(string prompt)
        {
            var requestBody = new
            {
                model = _options.ModelName ?? "meta/llama-3.1-405b-instruct",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.5,
                max_tokens = 1024,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation("Sending request to NVIDIA NIM API with model {Model}", _options.ModelName);

            using var response = await _httpClient.PostAsync("chat/completions", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("NVIDIA NIM API error: {StatusCode} - {Response}", response.StatusCode, responseContent.Substring(0, Math.Min(200, responseContent.Length)));
                return $"Error: {response.StatusCode}";
            }

            _logger.LogInformation("Received response from NVIDIA NIM API (length {Length})", responseContent.Length);
            _logger.LogDebug("Response content: {Content}", responseContent.Substring(0, Math.Min(500, responseContent.Length)));

            try
            {
                var result = JsonSerializer.Deserialize<NvidiaNimResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result?.Choices?[0]?.Message?.Content ?? string.Empty;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON. Response starts with: {Start}", responseContent.Substring(0, Math.Min(50, responseContent.Length)));
                throw;
            }
        }
    }

    public class NvidiaNimResponse
    {
        public List<NvidiaNimChoice> Choices { get; set; }
    }

    public class NvidiaNimChoice
    {
        public NvidiaNimMessage Message { get; set; }
    }

    public class NvidiaNimMessage
    {
        public string Content { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenRouter.NET;
using OpenRouter.NET.Models;

namespace MyFirstAIApp.Clients
{
    /// <summary>
    /// An <see cref="IChatClient"/> implementation that talks to the OpenRouter API.
    /// It mirrors the behaviour of the previous <c>OpenRouterService</c> but conforms to the
    /// Microsoft.Extensions.AI contract.
    /// </summary>
    public class OpenRouterAPIClient : IChatClient, IDisposable
    {
        private readonly OpenRouterClient _httpClient;
        private readonly OpenRouterOptions _options;
        private readonly ChatClientMetadata _metadata;
        private readonly ILogger<OpenRouterAPIClient> _logger;

        public OpenRouterAPIClient(IOptions<OpenRouterOptions> options, ILogger<OpenRouterAPIClient> logger)
        {
            _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
            _httpClient = new OpenRouterClient(new OpenRouterClientOptions { BaseUrl = _options.BaseUrl!, ApiKey = _options.ApiKey!});
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _metadata = new ChatClientMetadata("OpenRouterAPIClient", new Uri(_options.BaseUrl!), _options.ModelName);
        }

        // IChatClient implementation ------------------------------------------------
        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var chatCompletionRequest = new ChatCompletionRequest
            {
                Model = _options.ModelName ?? "openrouter/free",
                Messages = chatMessages.Select(m => Message.FromUser(m.Text)).ToList(),
            };

            _logger.LogInformation("Sending request to OpenRouter API (model {Model})", _options.ModelName);

            var response = await _httpClient.CreateChatCompletionAsync(chatCompletionRequest, cancellationToken);

            var reply = response?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
            var chatMessage = new ChatMessage(ChatRole.Assistant, reply.ToString());
            return new ChatResponse(chatMessage);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // OpenRouter currently does not provide a streaming endpoint in this wrapper.
            // We provide a simple implementation that yields a single full response.
            var fullResponse = await GetResponseAsync(chatMessages, options, cancellationToken);

            foreach (var update in fullResponse.ToChatResponseUpdates())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? key = null) =>
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType?.IsInstanceOfType(this) == true ? this :
            null;

        public void Dispose() => _httpClient.Dispose();
    }

    // DTOs matching the previous OpenRouterService response shape
    //internal class OpenRouterResponse
    //{
    //    public List<Choice>? Choices { get; set; }
    //}

    //internal class Choice
    //{
    //    public Message? Message { get; set; }
    //}

    //internal class Message
    //{
    //    public string? Content { get; set; }
    //}
}

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyFirstAIApp;
using MyFirstAIApp.Settings;
using MyFirstAIApp.Services;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

using var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    logging.AddConsole();
});
var logger = loggerFactory.CreateLogger("Program");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.ParameterFilter<ProviderDropdownFilter>());

// Config-driven provider registration
foreach (var section in builder.Configuration.GetSection("ProviderRegistry").GetChildren())
{
    var key = section.Key;
    var type = section["Type"];
    var modelName = section["ModelName"]!;

    if (type == "Ollama")
    {
        builder.Services.AddKeyedChatClient(key,
            new OllamaChatClient(section["BaseUrl"]!, modelName));
    }
    else
    {
        var apiKey = section["ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("Skipping provider {Key}: no ApiKey configured", key);
            continue;
        }

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(section["BaseUrl"]!) });
        builder.Services.AddKeyedChatClient(key, client.AsChatClient(modelName));
    }
}

builder.Services.Configure<Dictionary<string, ProviderRegistryEntry>>(
    builder.Configuration.GetSection("ProviderRegistry"));
builder.Services.AddSingleton<IChatClientFactory, ChatClientFactory>();
builder.Services.AddTransient<IBenchmarkService, BenchmarkService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();

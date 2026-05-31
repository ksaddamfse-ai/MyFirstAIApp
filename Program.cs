using Microsoft.Extensions.AI;
using MyFirstAIApp;
using MyFirstAIApp.Settings;
using MyFirstAIApp.Services;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.ParameterFilter<ProviderDropdownFilter>());

// Config-driven provider registration with middleware pipeline
foreach (var section in builder.Configuration.GetSection("ProviderRegistry").GetChildren())
{
    var key = section.Key;
    var type = section["Type"];
    var modelName = section["ModelName"]!;

    builder.Services.AddKeyedChatClient(key, serviceProvider =>
    {
        IChatClient client;
        if (type == "Ollama")
        {
            client = new OllamaChatClient(section["BaseUrl"]!, modelName);
        }
        else
        {
            var apiKey = section["ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return null!;

            client = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(section["BaseUrl"]!) })
                .GetChatClient(modelName)
                .AsIChatClient();
        }

        return client.AsBuilder()
            .UseLogging()
            .UseOpenTelemetry()
            .Build(serviceProvider);
    });
}

builder.Services.Configure<Dictionary<string, ProviderRegistryEntry>>(
    builder.Configuration.GetSection("ProviderRegistry"));
builder.Services.AddSingleton<IChatClientFactory, ChatClientFactory>();
builder.Services.AddTransient<IBenchmarkService, BenchmarkService>();

// OpenTelemetry: traces for ASP.NET Core and HTTP
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

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

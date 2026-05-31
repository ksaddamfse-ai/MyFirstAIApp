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
    var providerKey = section.Key;
    var type = section["Type"];
    var models = section.GetSection("Models").Get<List<string>>() ?? [];

    if (models.Count == 0)
        continue;

    foreach (var model in models)
    {
        var key = $"{providerKey}__{model}";

        if (type != "Ollama" && string.IsNullOrEmpty(section["ApiKey"]))
            continue;

        builder.Services.AddKeyedChatClient(key, serviceProvider =>
        {
            IChatClient client;
            if (type == "Ollama")
            {
                client = new OllamaChatClient(section["BaseUrl"]!, model);
            }
            else
            {
                client = new OpenAIClient(
                    new ApiKeyCredential(section["ApiKey"]!),
                    new OpenAIClientOptions { Endpoint = new Uri(section["BaseUrl"]!) })
                    .GetChatClient(model)
                    .AsIChatClient();
            }

            return client.AsBuilder()
                .UseLogging()
                .UseOpenTelemetry()
                .Build(serviceProvider);
        });
    }
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
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

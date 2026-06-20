using Anthropic;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using MyFirstAIApp;
using MyFirstAIApp.Settings;
using MyFirstAIApp.Services;
using OpenAI;
using OpenTelemetry.Trace;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.ParameterFilter<ProviderDropdownFilter>();
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MyFirstAIApp.xml"), includeControllerXmlComments: true);
});

foreach (var section in builder.Configuration.GetSection("ProviderRegistry").GetChildren())
{
    var providerKey = section.Key;
    var providerType = section["Type"] ?? "OpenAI";
    var models = section.GetSection("Models").Get<List<string>>() ?? [];

    if (models.Count == 0)
        continue;

    foreach (var model in models)
    {
        if (providerType != "AzureOpenAI"
            && string.IsNullOrEmpty(section["ApiKey"])
            && section["BaseUrl"]?.Contains("localhost") != true)
            continue;

        var key = $"{providerKey}__{model}";

        builder.Services.AddKeyedChatClient(key, serviceProvider =>
        {
            var client = providerType switch
            {
                "AzureOpenAI" => new AzureOpenAIClient(
                    new Uri(section["Endpoint"]!),
                    new ApiKeyCredential(section["ApiKey"]!))
                    .GetChatClient(model)
                    .AsIChatClient(),

                "Anthropic" => new AnthropicClient { ApiKey = section["ApiKey"]! }
                    .AsIChatClient(model),

                _ => new OpenAIClient(
                    new ApiKeyCredential(section["ApiKey"] ?? "not-needed"),
                    new OpenAIClientOptions { Endpoint = new Uri(section["BaseUrl"]!) })
                    .GetChatClient(model)
                    .AsIChatClient()
            };

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

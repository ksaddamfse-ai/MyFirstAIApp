using Microsoft.Extensions.AI;
using MyFirstAIApp;
using MyFirstAIApp.Models;
using MyFirstAIApp.Services;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.ParameterFilter<ProviderDropdownFilter>());
builder.Services.AddDistributedMemoryCache();

// AI Provider Integration

// OpenRouter
builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));
var openRouterApiKey = builder.Configuration["OpenRouter:ApiKey"];
var openRouterModel = builder.Configuration["OpenRouter:ModelName"];
var openRouterOptions = new OpenAIClientOptions
{
    Endpoint = new Uri(builder.Configuration["OpenRouter:BaseUrl"]!)
};
var openRouterClient = new OpenAIClient(new ApiKeyCredential(openRouterApiKey!), openRouterOptions);
builder.Services.AddKeyedChatClient("OpenRouterOpenAI", openRouterClient.AsChatClient(openRouterModel!));

// Ollama
builder.Services.AddKeyedChatClient("Ollama",
    new OllamaChatClient(
        builder.Configuration["Ollama:BaseUrl"]!,
        builder.Configuration["Ollama:ModelName"]));

// NvidiaNim
var nvidiaApiKey = builder.Configuration["NvidiaNim:ApiKey"];
var nvidiaModelName = builder.Configuration["NvidiaNim:ModelName"];
var nvidiaOptions = new OpenAIClientOptions
{
    Endpoint = new Uri(builder.Configuration["NvidiaNim:BaseUrl"]!)
};
var nvidiaClient = new OpenAIClient(new ApiKeyCredential(nvidiaApiKey!), nvidiaOptions);
builder.Services.AddKeyedChatClient("NvidiaNimOpenAI", nvidiaClient.AsChatClient(nvidiaModelName!));

builder.Services.Configure<BenchmarkOptions>(builder.Configuration.GetSection("Benchmark"));
builder.Services.AddTransient<IBenchmarkService, BenchmarkService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

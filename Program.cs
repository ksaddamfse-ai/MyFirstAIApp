using Microsoft.Extensions.AI;
using MyFirstAIApp;
using MyFirstAIApp.Services;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.ParameterFilter<ProviderDropdownFilter>());

// OpenRouter
var openRouter = new OpenAIClient(
    new ApiKeyCredential(builder.Configuration["OpenRouter:ApiKey"]!),
    new OpenAIClientOptions { Endpoint = new Uri(builder.Configuration["OpenRouter:BaseUrl"]!) });
builder.Services.AddKeyedChatClient("OpenRouterOpenAI",
    openRouter.AsChatClient(builder.Configuration["OpenRouter:ModelName"]!));

// Ollama
builder.Services.AddKeyedChatClient("Ollama",
    new OllamaChatClient(
        builder.Configuration["Ollama:BaseUrl"]!,
        builder.Configuration["Ollama:ModelName"]));

// Nvidia NIM
var nvidiaNim = new OpenAIClient(
    new ApiKeyCredential(builder.Configuration["NvidiaNim:ApiKey"]!),
    new OpenAIClientOptions { Endpoint = new Uri(builder.Configuration["NvidiaNim:BaseUrl"]!) });
builder.Services.AddKeyedChatClient("NvidiaNimOpenAI",
    nvidiaNim.AsChatClient(builder.Configuration["NvidiaNim:ModelName"]!));

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

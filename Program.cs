using Microsoft.Extensions.AI;
using MyFirstAIApp;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services to the container.
builder.Services.AddControllers();
// Add Swagger (Swashbuckle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDistributedMemoryCache();

// OpenRouter configuration
builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));

// OpenRouter setup
builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();

var app = builder.Build();

// Get a logger instance for program-level logs
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swashbuckle Swagger endpoints
    app.UseSwagger();
    app.UseSwaggerUI();

    // Keep the Microsoft.OpenApi endpoints mapped in Development as well
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

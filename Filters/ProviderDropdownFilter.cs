using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MyFirstAIApp.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyFirstAIApp;

public class ProviderDropdownFilter(IBenchmarkService benchmarkService) : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter.Name != "provider" && parameter.Name != "providers") return;

        var keys = benchmarkService.GetAvailableProviders().Select(p => p.Key).ToList();

        if (parameter.Name == "providers")
        {
            parameter.Schema.Type = "array";
            parameter.Schema.Items = new OpenApiSchema
            {
                Type = "string",
                Enum = keys.Select(k => new OpenApiString(k)).Cast<IOpenApiAny>().ToList()
            };
        }
        else
        {
            parameter.Schema.Type = "string";
            parameter.Schema.Enum = keys.Select(k => new OpenApiString(k)).Cast<IOpenApiAny>().ToList();
        }
    }
}

using Microsoft.Extensions.AI;

namespace MyFirstAIApp.Models;

public class ResolveClientResult
{
    public IChatClient? Client { get; init; }
    public string? Error { get; init; }

    public bool IsSuccess => Client is not null;
}

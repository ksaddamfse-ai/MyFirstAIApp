using Microsoft.Extensions.AI;

namespace MyFirstAIApp.Services;

public interface IChatClientFactory
{
    IChatClient? GetClient(string provider);
}

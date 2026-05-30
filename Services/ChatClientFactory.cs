using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MyFirstAIApp.Services;

public class ChatClientFactory(IServiceProvider serviceProvider) : IChatClientFactory
{
    public IChatClient? GetClient(string provider) =>
        serviceProvider.GetKeyedService<IChatClient>(provider);
}

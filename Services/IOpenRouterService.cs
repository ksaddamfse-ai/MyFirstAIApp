using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyFirstAIApp;

public interface IOpenRouterService
{
    Task<string> AskAI(string prompt);
}
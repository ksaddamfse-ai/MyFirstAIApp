using System.Threading.Tasks;

namespace MyFirstAIApp;

public interface INvidiaNimService
{
    Task<string> AskAI(string prompt);
}
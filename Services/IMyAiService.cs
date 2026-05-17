using System.Threading.Tasks;

namespace MyFirstAIApp
{
    public interface IMyAiService
    {
        Task<string> RunAsync(string question);
    }
}

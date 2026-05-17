using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace MyFirstAIApp
{
    public class MyAiService : IMyAiService
    {
        private readonly IChatClient _chatClient;

        public MyAiService([FromKeyedServices("OpenRouterOpenAI")]IChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public async Task<string> RunAsync(string question)
        {
            var response = await _chatClient.GetResponseAsync(question);
            System.Console.WriteLine(response?.Text);
            return response!.Text;
        }
    }
}

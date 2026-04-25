public interface IOllamaService
{
    Task<string> AskAI(string prompt);
}

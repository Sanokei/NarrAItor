using Anthropic;
using NarrAItor.Configuration;

namespace NarrAItor.LLM;

public static class LLM
{
    internal static async Task SendRequest(IList<Message> messages, int maxTokens = 250)
    {
        if (maxTokens < 250)
            Console.WriteLine("A higher token count is recomended.");
        // TODO: maybe use SecureString?
        string apiKey = Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY) ?? throw new Exception($"{Config.Names.Secrets.ANTHROPIC_API_KEY.Replace("__",":")} never passed.\nUsage: --BearerToken \"api-key\"\nor in the config file. \"BearerToken\":\"api-key\"");
        using var api = new AnthropicApi(apiKey);

        var response = await api.CreateMessageAsync
        (
            model: CreateMessageRequestModel.Claude35Sonnet20240620,
            messages: messages,
            maxTokens: maxTokens
        );
        
    }
}

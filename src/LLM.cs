using Anthropic;
namespace NarrAItor.LLM
{
    public static class LLM
    {
        internal static async Task SendRequest(IList<Message> messages, int maxTokens = 250)
        {
            if (maxTokens < 250)
                Console.WriteLine("A higher token count is recomended.");
            string apiKey = Environment.GetEnvironmentVariable("BearerToken") ?? throw new Exception($"BearerToken never passed. Usage: --BearerToken \"api-key-here\"");
            using var api = new AnthropicApi(apiKey);

            var response = await api.CreateMessageAsync(
                model: CreateMessageRequestModel.Claude35Sonnet20240620,
                messages: messages,
                maxTokens: maxTokens);
            throw new NotImplementedException();
        }
    }
}
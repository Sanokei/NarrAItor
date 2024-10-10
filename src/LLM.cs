using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using Anthropic.SDK.Extensions;
using Anthropic.SDK;
using Anthropic.SDK.Constants;

using MoonSharp.Interpreter;

using NarrAItor.Configuration;


namespace NarrAItor.LLM;

public static class Anthropic
{
    public static async Task<MessageResponse> Ask(List<Message> messages, int MaxTokens)
    {
        // TODO: maybe use SecureString?
        string apiKey = Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY) ?? throw new Exception($"{Config.Names.Secrets.ANTHROPIC_API_KEY} never passed.\nUsage: --BearerToken \"api-key\"");
        using var api = new AnthropicClient(apiKey);

        MessageResponse response = await api.Messages.GetClaudeMessageAsync
        (
            new MessageParameters()
            {
                Model = AnthropicModels.Claude35Sonnet,
                Messages = messages,
                MaxTokens = MaxTokens,
                Stream = false,
                Temperature = 0.3m,
            }
        );
        return response;
    }
}

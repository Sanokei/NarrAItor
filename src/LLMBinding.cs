using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using Anthropic.SDK.Extensions;
using Anthropic.SDK;
using Anthropic.SDK.Constants;

using MoonSharp.Interpreter;

using NarrAItor.Configuration;

namespace NarrAItor;

public static class LLM
{
    public static async Task<MessageResponse> Ask(List<Message> Messages, Dictionary<string,object> args)
    {
        // This can just thow an error. The LLM error handling expectedly only works with the LLM.
        string apiKey = Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY) ?? throw new Exception($"{Config.Names.Secrets.ANTHROPIC_API_KEY} never passed.\nUsage: --BearerToken \"api-key\"");
        using var api = new AnthropicClient(apiKey);

        MessageResponse response = await api.Messages.GetClaudeMessageAsync
        (
            new MessageParameters()
            {
                Model = AnthropicModels.Claude35Sonnet, // FIXME: Will get outdated.
                Messages = Messages,
                MaxTokens = args.TryGetValue("MaxTokens", out var MaxTokens) ? (int)MaxTokens : 0,
                Stream = args.TryGetValue("Stream", out var Stream) ? (bool)Stream : false,
                Temperature = args.TryGetValue("Temperature", out var Temperature) ? (decimal)Temperature : 0.3m,
                System = args.TryGetValue("System", out var SystemMessages) ? (List<SystemMessage>) SystemMessages : [],
            }
        );
        return response;
    }
}
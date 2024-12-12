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
    public static async Task<MessageResponse> Ask(List<Message> Messages, Dictionary<string, object> args)
    {
        // API Key retrieval
        string apiKey = Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY) 
            ?? throw new Exception($"{Config.Names.Secrets.ANTHROPIC_API_KEY} never passed.\nUsage: --BearerToken \"api-key\"");
        
        using var api = new AnthropicClient(apiKey);

        // Prepare MessageParameters with flexible configuration
        var parameters = new MessageParameters()
        {
            Model = args.TryGetValue("Model", out var Model) 
                ? (string)Model 
                : AnthropicModels.Claude35Sonnet,
            
            Messages = Messages,
            
            MaxTokens = args.TryGetValue("MaxTokens", out var MaxTokens) 
                ? (int)MaxTokens 
                : 2048,
            
            Stream = args.TryGetValue("Stream", out var Stream) 
                ? (bool)Stream 
                : false,
            
            Temperature = args.TryGetValue("Temperature", out var Temperature) 
                ? (decimal)Temperature 
                : 0.3m,
            
            System = args.TryGetValue("System", out var SystemMessages) 
                ? (List<SystemMessage>)SystemMessages 
                : [],
        };

        // Tool/Function Support
        if (args.TryGetValue("Tools", out var toolsObj) && toolsObj is List<Anthropic.SDK.Common.Tool> tools)
        {
            parameters.Tools = tools;

            // Optional Tool Choice
            if (args.TryGetValue("ToolChoice", out var toolChoiceObj) && toolChoiceObj is ToolChoice toolChoice)
            {
                parameters.ToolChoice = toolChoice;
            }
        }

        // Streaming vs Non-Streaming Response
        if ((bool)parameters.Stream)
        {
            // Streaming response handling
            var streamOutputs = new List<MessageResponse>();
            await foreach (var res in api.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (res.Delta != null)
                {
                    // You might want to add custom streaming logic here
                    // For example, writing to console or invoking a callback
                    Console.Write(res.Delta.Text);
                }
                streamOutputs.Add(res);
            }

            // Handle Tool Calls in Streaming Mode
            var finalOutput = streamOutputs.LastOrDefault();
            if (finalOutput?.ToolCalls != null && parameters.Tools != null)
            {
                foreach (var toolCall in finalOutput.ToolCalls)
                {
                    var response = await toolCall.InvokeAsync<string>();
                    Messages.Add(new Message(toolCall, response));
                }
            }

            return finalOutput ?? throw new Exception("No streaming response received");
        }
        else
        {
            // Standard non-streaming response
            var response = await api.Messages.GetClaudeMessageAsync(parameters);

            // Automatic Tool Call Handling
            if (response.ToolCalls != null && parameters.Tools != null)
            {
                foreach (var toolCall in response.ToolCalls)
                {
                    var _response = await toolCall.InvokeAsync<string>();
                    Messages.Add(new Message(toolCall, _response));
                }

                // Optional: Get final result after tool calls
                response = await api.Messages.GetClaudeMessageAsync(parameters);
            }

            return response;
        }
    }

    // Helper method to create tools from functions easily with explicit type specification
    public static Anthropic.SDK.Common.Tool CreateToolFromFunc<TResult>(string name, string description, Func<TResult> func)
    {
        return Anthropic.SDK.Common.Tool.FromFunc(name, func, description);
    }

    // Overload for functions with one parameter
    public static Anthropic.SDK.Common.Tool CreateToolFromFunc<T1, TResult>(string name, string description, Func<T1, TResult> func)
    {
        return Anthropic.SDK.Common.Tool.FromFunc(name, func, description);
    }

    // Overload for functions with two parameters
    public static Anthropic.SDK.Common.Tool CreateToolFromFunc<T1, T2, TResult>(string name, string description, Func<T1, T2, TResult> func)
    {
        return Anthropic.SDK.Common.Tool.FromFunc(name, func, description);
    }
}
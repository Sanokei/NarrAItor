using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using Anthropic.SDK.Extensions;
using Anthropic.SDK;
using Anthropic.SDK.Constants;

using MoonSharp.Interpreter;

using NarrAItor.Configuration;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using System.Text.Json;

namespace NarrAItor;
/// <summary>
/// Represents the result of a batch processing request
/// </summary>
public record BatchProcessingResult
{
    public string CustomId { get; init; }
    public MessageResponse Message { get; init; }
    public string ErrorMessage { get; init; }
    public bool Success { get; init; }
}

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

    public static async Task<MessageResponse> GeneratePrompt(Dictionary<string, string> userArgs, int maxTokens, string apiDocumentation)
    {
        string prompt = $@"
            Using the following user variables: {string.Join(", ", userArgs.Select(kv => $"{kv.Key}: {kv.Value}"))}
            The response must be within {maxTokens} tokens.
            Do NOT make up API endpoints. Only use the available API below.
            {apiDocumentation}

            Based on the above information, generate a concise and effective prompt that an LLM can use to perform a task. The prompt should be clear, specific, and actionable.
        ";

        var messages = new List<Message> { new Message(RoleType.User, prompt) };
        var args = new Dictionary<string, object>(); // No special arguments needed for prompt generation

        try
        {
            var response = await Ask(messages, args);
            return response; // Return the generated prompt
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating prompt: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Represents the structure of a batch processing request
    /// </summary>
    public class BatchRequest
    {
        [JsonPropertyName("custom_id")]
        public string CustomId { get; set; }

        [JsonPropertyName("params")]
        public MessageParameters Params { get; set; }
    }

    /// <summary>
    /// Represents the status and metadata of a message batch
    /// </summary>
    public class BatchStatus
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("processing_status")]
        public string ProcessingStatus { get; set; }

        [JsonPropertyName("request_counts")]
        public BatchRequestCounts RequestCounts { get; set; }

        [JsonPropertyName("ended_at")]
        public DateTime? EndedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("results_url")]
        public string ResultsUrl { get; set; }
    }

    /// <summary>
    /// Represents counts of different request states in a batch
    /// </summary>
    public class BatchRequestCounts
    {
        [JsonPropertyName("processing")]
        public int Processing { get; set; }

        [JsonPropertyName("succeeded")]
        public int Succeeded { get; set; }

        [JsonPropertyName("errored")]
        public int Errored { get; set; }

        [JsonPropertyName("canceled")]
        public int Canceled { get; set; }

        [JsonPropertyName("expired")]
        public int Expired { get; set; }
    }

    /// <summary>
    /// Represents a single result from batch processing
    /// </summary>
    public class BatchResult
    {
        [JsonPropertyName("custom_id")]
        public string CustomId { get; set; }

        [JsonPropertyName("result")]
        public BatchResultDetails Result { get; set; }
    }

    /// <summary>
    /// Contains the details of a batch result
    /// </summary>
    public class BatchResultDetails
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("message")]
        public MessageResponse Message { get; set; }

        [JsonPropertyName("error")]
        public ErrorDetails Error { get; set; }
    }

    /// <summary>
    /// Represents error details in batch processing
    /// </summary>
    public class ErrorDetails
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    private const string ANTHROPIC_API_BASE = "https://api.anthropic.com/v1/messages";
    private const string BATCH_API_PATH = "batches";
    private static readonly HttpClient httpClient = new();

    /// <summary>
    /// Creates a new message batch for processing
    /// </summary>
    /// <param name="batchRequests">Collection of requests to process in batch</param>
    /// <param name="defaultArgs">Default arguments for all requests</param>
    /// <returns>Batch identifier for status tracking</returns>
    public static async Task<string> CreateMessageBatch(
        List<(string CustomId, List<Message> Messages, Dictionary<string, object> Args)> batchRequests,
        Dictionary<string, object> defaultArgs = null)
    {
        var apiKey = Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY)
            ?? throw new Exception($"{Config.Names.Secrets.ANTHROPIC_API_KEY} not set");

        defaultArgs ??= new Dictionary<string, object>();

        var requests = batchRequests.Select(request =>
        {
            var args = new Dictionary<string, object>(defaultArgs);
            foreach (var arg in request.Args)
            {
                args[arg.Key] = arg.Value;
            }

            return new BatchRequest
            {
                CustomId = request.CustomId,
                Params = new MessageParameters
                {
                    Model = (string)args.GetValueOrDefault("Model", AnthropicModels.Claude35Sonnet),
                    Messages = request.Messages,
                    MaxTokens = (int)args.GetValueOrDefault("MaxTokens", 2048),
                    Temperature = (decimal)args.GetValueOrDefault("Temperature", 0.3m),
                    System = (List<SystemMessage>)args.GetValueOrDefault("System", new List<SystemMessage>())
                }
            };
        }).ToList();

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{ANTHROPIC_API_BASE}/{BATCH_API_PATH}")
        {
            Headers =
            {
                { "x-api-key", apiKey },
                { "anthropic-beta", "message-batches-2024-09-24" }
            },
            Content = JsonContent.Create(new { requests })
        };

        var response = await httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var batchStatus = await response.Content.ReadFromJsonAsync<BatchStatus>();
        return batchStatus?.Id ?? throw new Exception("Failed to create batch: No batch ID returned");
    }

    /// <summary>
    /// Retrieves the current status of a batch processing request
    /// </summary>
    /// <param name="batchId">Identifier of the batch to check</param>
    /// <returns>Current batch status information</returns>
    public static async Task<BatchStatus> GetBatchStatus(string batchId)
    {
        var apiKey = Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY)
            ?? throw new Exception($"{Config.Names.Secrets.ANTHROPIC_API_KEY} not set");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{ANTHROPIC_API_BASE}/{BATCH_API_PATH}/{batchId}")
        {
            Headers = { { "x-api-key", apiKey } }
        };

        var response = await httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BatchStatus>()
            ?? throw new Exception($"Failed to retrieve batch status for {batchId}");
    }

    /// <summary>
    /// Retrieves results from a completed batch
    /// </summary>
    /// <param name="batchId">Identifier of the completed batch</param>
    /// <returns>Collection of batch processing results</returns>
    public static async Task<List<BatchResult>> GetBatchResults(string batchId)
    {
        var apiKey = Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY)
            ?? throw new Exception($"{Config.Names.Secrets.ANTHROPIC_API_KEY} not set");

        var status = await GetBatchStatus(batchId);
        if (status.ProcessingStatus != "ended")
        {
            throw new InvalidOperationException($"Batch {batchId} has not completed processing");
        }

        if (string.IsNullOrEmpty(status.ResultsUrl))
        {
            throw new InvalidOperationException($"No results URL available for batch {batchId}");
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, status.ResultsUrl)
        {
            Headers = { { "x-api-key", apiKey } }
        };

        var response = await httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        var results = new List<BatchResult>();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        
        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            var result = JsonSerializer.Deserialize<BatchResult>(line);
            if (result != null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    /// <summary>
    /// Monitors batch processing until completion or timeout
    /// </summary>
    /// <param name="batchId">Identifier of the batch to monitor</param>
    /// <param name="pollingInterval">Milliseconds between status checks</param>
    /// <param name="timeout">Maximum milliseconds to wait</param>
    /// <returns>Collection of batch processing results</returns>
    public static async Task<List<BatchResult>> WaitForBatchCompletion(
        string batchId,
        int pollingInterval = 5000,
        int timeout = 86400000)
    {
        var startTime = DateTime.UtcNow;
        while (true)
        {
            if ((DateTime.UtcNow - startTime).TotalMilliseconds > timeout)
            {
                throw new TimeoutException($"Batch {batchId} processing timed out after {timeout}ms");
            }

            var status = await GetBatchStatus(batchId);
            if (status.ProcessingStatus == "ended")
            {
                return await GetBatchResults(batchId);
            }

            await Task.Delay(pollingInterval);
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
    // Overload for functions with three parameters
    public static Anthropic.SDK.Common.Tool CreateToolFromFunc<T1, T2, T3, TResult>(string name, string description, Func<T1, T2, T3, TResult> func)
    {
        return Anthropic.SDK.Common.Tool.FromFunc(name, func, description);
    }
    // Overload for functions with three parameters
    public static Anthropic.SDK.Common.Tool CreateToolFromFunc<T1, T2, T3, T4, TResult>(string name, string description, Func<T1, T2, T3, T4, TResult> func)
    {
        return Anthropic.SDK.Common.Tool.FromFunc(name, func, description);
    }
}
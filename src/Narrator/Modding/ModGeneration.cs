using BatchResult = NarrAItor.LLM.BatchResult;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using MoonSharp.Interpreter;

namespace NarrAItor.Narrator.Modding;

/// <summary>
/// Manages the generation and validation of narrator mods, including batch processing capabilities.
/// Handles the complete lifecycle of mod creation from initial proposal to final implementation.
/// </summary>
public class ModGeneration
{
    private readonly NarratorBot _bot;
    private readonly Dictionary<string, object> _defaultArgs;
    private const int MAX_CONCURRENT_GENERATIONS = 5;
    private const string NARRATORAPI_DOCUMENTATION_PATH = "./NarrAItor/NarratorAPI.md";

    /// <summary>
    /// Represents the complete state of a mod during its generation process.
    /// Tracks all intermediate steps and validation results.
    /// </summary>
    private class ModGenerationState
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Proposal { get; set; }
        public string CSharpCode { get; set; }
        public string Documentation { get; set; }
        public string LuaScript { get; set; }
        public Dictionary<string, string> PromptArgs { get; set; }
        public List<Message> ConversationHistory { get; set; }

        public ModGenerationState()
        {
            ConversationHistory = new List<Message>();
            PromptArgs = new Dictionary<string, string>();
        }

        /// <summary>
        /// Validates the completeness and integrity of the mod generation state
        /// </summary>
        public bool Validate() => !string.IsNullOrEmpty(Name) &&
                                !string.IsNullOrEmpty(Description) &&
                                !string.IsNullOrEmpty(Proposal) &&
                                !string.IsNullOrEmpty(LuaScript);
    }

    /// <summary>
    /// Defines the parameters for a batch mod generation request
    /// </summary>
    public record ModGenerationRequest
    {
        public string Name { get; init; }
        public string Description { get; init; }
        public Dictionary<string, object> Parameters { get; init; }
    }

    /// <summary>
    /// Contains the results of a completed mod generation process
    /// </summary>
    public record ModGenerationResult
    {
        public string Name { get; init; }
        public bool Success { get; init; }
        public string ErrorMessage { get; init; }
        public NarratorMod GeneratedMod { get; init; }
        public string CSharpCode { get; init; }
        public string LuaScript { get; init; }
    }

    /// <summary>
    /// Initializes a new instance of the ModGeneration class
    /// </summary>
    /// <param name="bot">The NarratorBot instance that will own generated mods</param>
    public ModGeneration(NarratorBot bot)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _defaultArgs = new Dictionary<string, object>
        {
            { "MaxTokens", 4000 },
            { "Temperature", 0.7m }
        };
    }

    /// <summary>
    /// Generates a single mod with specified name and description
    /// </summary>
    public async Task<string> CreateMod(string modName, string modDescription)
    {
        ValidateInputParameters(modName, modDescription);

        try
        {
            var state = new ModGenerationState
            {
                Name = modName,
                Description = modDescription
            };

            state = await GenerateInitialProposal(state);
            state = await ValidateAndRefineProposal(state);
            state = await GenerateImplementation(state);
            state = await ValidateModState(state);
            state = await PerformTechnicalReview(state);

            return await CreateModFiles(state);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Mod generation failed for '{modName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Processes multiple mod generation requests in parallel using batch processing
    /// </summary>
    public async Task<IEnumerable<ModGenerationResult>> GenerateModsBatch(IEnumerable<ModGenerationRequest> requests)
    {
        var results = new ConcurrentBag<ModGenerationResult>();
        var batchRequests = new List<(string CustomId, List<Message> Messages, Dictionary<string, object> Args)>();
        var requestMap = new ConcurrentDictionary<string, ModGenerationRequest>();

        foreach (var request in requests)
        {
            try
            {
                ValidateInputParameters(request.Name, request.Description);
                
                var customId = $"proposal_{request.Name}";
                var promptArgs = new Dictionary<string, string>
                {
                    { "modName", request.Name },
                    { "modDescription", request.Description },
                    { "botObjective", _bot.Objective },
                    { "requirements", Prompts.NarratorBot.BaseRequirements }
                };

                var messages = new List<Message>
                {
                    new Message(RoleType.User, Prompts.NarratorBot.SystemContext),
                    new Message(RoleType.User, JsonSerializer.Serialize(promptArgs))
                };

                batchRequests.Add((customId, messages, request.Parameters ?? new Dictionary<string, object>()));
                requestMap[customId] = request;
            }
            catch (Exception ex)
            {
                results.Add(new ModGenerationResult
                {
                    Name = request.Name,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        try
        {
            var batchId = await LLM.CreateMessageBatch(batchRequests);
            var batchResults = await LLM.WaitForBatchCompletion(batchId);

            var implementationBatchRequests = await PrepareImplementationRequests(batchResults, requestMap);
            
            if (implementationBatchRequests.Any())
            {
                var implBatchId = await LLM.CreateMessageBatch(implementationBatchRequests);
                var implResults = await LLM.WaitForBatchCompletion(implBatchId);
                
                foreach (var result in await ProcessImplementationResults(implResults))
                {
                    results.Add(result);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Batch processing error: {ex.Message}");
        }

        return results;
    }

    /// <summary>
    /// Generates an initial proposal for a mod based on its current state
    /// </summary>
    private async Task<ModGenerationState> GenerateInitialProposal(ModGenerationState state)
    {
        var promptArgs = new Dictionary<string, string>
        {
            { "modName", state.Name },
            { "modDescription", state.Description },
            { "botObjective", _bot.Objective },
            { "requirements", Prompts.NarratorBot.BaseRequirements }
        };
        state.PromptArgs = promptArgs;
        var basePrompt = Prompts.NarratorBot.SystemContext;
        state.Proposal = await GeneratePromptAndAsk(state, basePrompt);
        
        if (string.IsNullOrEmpty(state.Proposal))
        {
            throw new InvalidOperationException("Failed to generate initial proposal");
        }

        return state;
    }

    /// <summary>
    /// Validates and refines the current mod proposal
    /// </summary>
    private async Task<ModGenerationState> ValidateAndRefineProposal(ModGenerationState state)
    {
        var promptArgs = new Dictionary<string, string>
        {
            { "proposal", state.Proposal },
            { "criteria", Prompts.NarratorBot.ValidationCriteria }
        };
        state.PromptArgs = promptArgs;
        var basePrompt = $"Validate mod proposal:\n{state.Proposal}";
        var validationResult = await GeneratePromptAndAsk(state, basePrompt);
        
        if (!validationResult?.Contains("approved", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            throw new InvalidOperationException("Mod proposal failed validation criteria");
        }

        return state;
    }

    /// <summary>
    /// Generates and validates a complete mod implementation
    /// </summary>
    private async Task<ModGenerationState> GenerateImplementation(ModGenerationState state)
    {
        state.CSharpCode = await GenerateModuleCode(state);
        state.LuaScript = await GenerateLuaImplementation(state);
        return state;
    }

    /// <summary>
    /// Validates the state management of a mod implementation
    /// </summary>
    private async Task<ModGenerationState> ValidateModState(ModGenerationState state)
    {
        var promptArgs = new Dictionary<string, string>
        {
            { "code", state.CSharpCode },
            { "criteria", Prompts.NarratorBot.ValidationCriteria }
        };
        state.PromptArgs = promptArgs;
        var basePrompt = "Verify state management for mod implementation";
        await GeneratePromptAndAsk(state, basePrompt);
        return state;
    }

    /// <summary>
    /// Performs a technical review of the mod implementation
    /// </summary>
    private async Task<ModGenerationState> PerformTechnicalReview(ModGenerationState state)
    {
        var promptArgs = new Dictionary<string, string>
        {
            { "code", state.CSharpCode },
            { "criteria", Prompts.NarratorBot.TechnicalCriteria }
        };
        state.PromptArgs = promptArgs;
        var basePrompt = "Perform technical review of implementation";
        await GeneratePromptAndAsk(state, basePrompt);
        return state;
    }

    /// <summary>
    /// Generates prompts and processes responses from the LLM
    /// </summary>
    private async Task<string> GeneratePromptAndAsk(ModGenerationState state, string basePrompt)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var promptResponse = await LLM.GeneratePrompt(
            state.PromptArgs,
            _defaultArgs["MaxTokens"] as int? ?? 4000,
            await GetApiDocumentation()
        );

        if (promptResponse?.Content == null)
        {
            throw new InvalidOperationException("Failed to generate prompt");
        }

        var generatedPrompt = promptResponse.Content.FirstOrDefault()?.ToString();
        var messages = new List<Message>(state.ConversationHistory)
        {
            new Message(RoleType.User, basePrompt),
            new Message(RoleType.User, generatedPrompt)
        };

        var response = await LLM.Ask(messages, _defaultArgs);
        state.ConversationHistory.AddRange(new[] {
            new Message(RoleType.User, generatedPrompt),
            new Message(RoleType.Assistant, response.Content?.FirstOrDefault()?.ToString())
        });

        return response.Content?.FirstOrDefault()?.ToString();
    }

    /// <summary>
    /// Generates the module code for a mod
    /// </summary>
    private async Task<string> GenerateModuleCode(ModGenerationState state)
    {
        var promptArgs = new Dictionary<string, string>
        {
            { "className", GetFormattedClassName(state.Name) },
            { "proposal", state.Proposal },
            { "requirements", Prompts.NarratorBot.BaseRequirements }
        };
        state.PromptArgs = promptArgs;
        return await GeneratePromptAndAsk(state, "Generate C# implementation based on:");
    }

    /// <summary>
    /// Generates the Lua implementation for a mod
    /// </summary>
    private async Task<string> GenerateLuaImplementation(ModGenerationState state)
    {
        var promptArgs = new Dictionary<string, string>
        {
            { "proposal", state.Proposal },
            { "requirements", Prompts.NarratorBot.BaseRequirements }
        };
        state.PromptArgs = promptArgs;
        return await GeneratePromptAndAsk(state, Prompts.NarratorBot.SystemContext);
    }

    /// <summary>
    /// Creates the physical mod files and registers the mod
    /// </summary>
    private async Task<string> CreateModFiles(ModGenerationState state)
    {
        string modsDirectory = Path.Combine(AppContext.BaseDirectory, "Mods");
        Directory.CreateDirectory(modsDirectory);

        string className = GetFormattedClassName(state.Name);
        string csFilePath = Path.Combine(modsDirectory, $"{className}.cs");
        string luaFilePath = Path.Combine(modsDirectory, $"{state.Name.Replace(" ", "_")}.lua");

        await File.WriteAllTextAsync(csFilePath, state.CSharpCode);
        await File.WriteAllTextAsync(luaFilePath, state.LuaScript);

        var newMod = new NarratorMod(state.LuaScript, _bot.script, _bot.RequiredMods);
        _bot.RequiredMods[state.Name] = newMod;

        return csFilePath;
    }

    /// <summary>
    /// Validates the input parameters for mod generation
    /// </summary>
    private void ValidateInputParameters(string modName, string modDescription)
    {
        if (string.IsNullOrWhiteSpace(modName))
            throw new ArgumentException("Mod name cannot be empty", nameof(modName));
        
        if (string.IsNullOrWhiteSpace(modDescription))
            throw new ArgumentException("Mod description cannot be empty", nameof(modDescription));
        
        if (!Regex.IsMatch(modName, @"^[a-zA-Z0-9_]+$"))
            throw new ArgumentException("Mod name must contain only alphanumeric characters and underscores", nameof(modName));
        
        if (_bot.RequiredMods.ContainsKey(modName))
            throw new ArgumentException($"Mod '{modName}' already exists", nameof(modName));
    }

    /// <summary>
    /// Prepares implementation requests for batch processing, converting batch results into implementation tasks
    /// </summary>
    /// <param name="batchResults">Results from the initial batch proposal generation</param>
    /// <param name="requestMap">Mapping of request IDs to their original generation requests</param>
    /// <returns>List of implementation requests ready for batch processing</returns>
    private async Task<List<(string CustomId, List<Message> Messages, Dictionary<string, object> Args)>> 
        PrepareImplementationRequests(List<LLM.BatchResult> batchResults, ConcurrentDictionary<string, ModGenerationRequest> requestMap)
    {
        var implementationRequests = new List<(string CustomId, List<Message> Messages, Dictionary<string, object> Args)>();

        foreach (var result in batchResults.Where(r => r.Result.Type == "succeeded"))
        {
            if (!requestMap.TryGetValue(result.CustomId, out var originalRequest))
                continue;

            var proposal = result.Result.Message.Content.FirstOrDefault()?.ToString();
            if (string.IsNullOrEmpty(proposal)) 
                continue;

            var implementationCustomId = $"impl_{originalRequest.Name}";
            var implementationMessages = new List<Message>
            {
                new Message(RoleType.User, "Generate C# and Lua implementation based on the approved proposal."),
                new Message(RoleType.User, proposal)
            };

            implementationRequests.Add((
                implementationCustomId,
                implementationMessages,
                originalRequest.Parameters ?? new Dictionary<string, object>()
            ));
        }

        return implementationRequests;
    }

    /// <summary>
    /// Processes the results of implementation batch requests and generates final mod artifacts
    /// </summary>
    /// <param name="implResults">Results from the batch implementation process</param>
    /// <returns>Collection of processed mod generation results</returns>
    private async Task<IEnumerable<ModGenerationResult>> ProcessImplementationResults(List<LLM.BatchResult> implResults)
    {
        var results = new List<ModGenerationResult>();

        foreach (var implResult in implResults)
        {
            var modName = implResult.CustomId.Replace("impl_", "");
            
            if (implResult.Result.Type != "succeeded")
            {
                results.Add(new ModGenerationResult
                {
                    Name = modName,
                    Success = false,
                    ErrorMessage = implResult.Result.Error?.Message ?? "Implementation failed"
                });
                continue;
            }

            try
            {
                var content = implResult.Result.Message.Content.FirstOrDefault()?.ToString();
                if (string.IsNullOrEmpty(content)) continue;

                var (csharpCode, luaScript) = ParseImplementationContent(content);
                var mod = await CreateAndValidateMod(modName, csharpCode, luaScript);

                results.Add(new ModGenerationResult
                {
                    Name = modName,
                    Success = true,
                    GeneratedMod = mod,
                    CSharpCode = csharpCode,
                    LuaScript = luaScript
                });
            }
            catch (Exception ex)
            {
                results.Add(new ModGenerationResult
                {
                    Name = modName,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Creates and validates a mod from generated implementation code
    /// </summary>
    /// <param name="modName">Name of the mod being created</param>
    /// <param name="csharpCode">Generated C# implementation code</param>
    /// <param name="luaScript">Generated Lua implementation code</param>
    /// <returns>Validated NarratorMod instance</returns>
    private async Task<NarratorMod> CreateAndValidateMod(string modName, string csharpCode, string luaScript)
    {
        string modsDirectory = Path.Combine(AppContext.BaseDirectory, "Mods");
        Directory.CreateDirectory(modsDirectory);

        string className = GetFormattedClassName(modName);
        string csFilePath = Path.Combine(modsDirectory, $"{className}.cs");
        string luaFilePath = Path.Combine(modsDirectory, $"{modName.Replace(" ", "_")}.lua");

        await File.WriteAllTextAsync(csFilePath, csharpCode);
        await File.WriteAllTextAsync(luaFilePath, luaScript);

        var mod = new NarratorMod(luaScript, _bot.script, _bot.RequiredMods);
        _bot.RequiredMods[modName] = mod;

        return mod;
    }

    /// <summary>
    /// Parses implementation content to extract C# and Lua code segments
    /// </summary>
    /// <param name="content">Raw implementation content containing both C# and Lua code</param>
    /// <returns>Tuple containing separated C# and Lua code</returns>
    /// <exception cref="FormatException">Thrown when the implementation format is invalid</exception>
    private (string CSharpCode, string LuaScript) ParseImplementationContent(string content)
    {
        // Extract C# code segment
        var csharpMatch = Regex.Match(content, @"```csharp\s*(.*?)\s*```", RegexOptions.Singleline);
        
        // Extract Lua code segment
        var luaMatch = Regex.Match(content, @"```lua\s*(.*?)\s*```", RegexOptions.Singleline);

        if (!csharpMatch.Success || !luaMatch.Success)
        {
            throw new FormatException("Invalid implementation format: Could not extract both C# and Lua code segments");
        }

        return (csharpMatch.Groups[1].Value.Trim(), luaMatch.Groups[1].Value.Trim());
    }

    /// <summary>
    /// Formats a mod name into a valid class name
    /// </summary>
    /// <param name="modName">Raw mod name to format</param>
    /// <returns>Properly formatted class name</returns>
    private string GetFormattedClassName(string modName)
    {
        // Ensure first character is uppercase and remove spaces
        return $"{char.ToUpper(modName[0])}{modName.Substring(1).Replace(" ", "")}";
    }

    /// <summary>
    /// Retrieves the NarratorAPI documentation content
    /// </summary>
    /// <returns>API documentation content as string</returns>
    /// <exception cref="FileNotFoundException">Thrown when documentation file cannot be found</exception>
    private async Task<string> GetApiDocumentation()
    {
        string documentationPath = Path.Combine(AppContext.BaseDirectory, NARRATORAPI_DOCUMENTATION_PATH);
        
        if (!File.Exists(documentationPath))
        {
            throw new FileNotFoundException($"NarratorAPI documentation file not found at: {documentationPath}");
        }

        return await File.ReadAllTextAsync(documentationPath);
    }
}
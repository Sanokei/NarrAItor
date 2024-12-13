using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using MoonSharp.Interpreter;

namespace NarrAItor.Narrator.Modding;

public class ModGeneration
{
    private readonly NarratorBot _bot;
    private readonly Dictionary<string, object> _defaultArgs;
    private readonly string _apiDocumentation;

    private class ModGenerationState
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Proposal { get; set; }
        public string CSharpCode { get; set; }
        public string LuaScript { get; set; }
        public List<Message> ConversationHistory { get; set; }
    }

    public ModGeneration(NarratorBot bot)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _defaultArgs = new Dictionary<string, object>
        {
            { "MaxTokens", 4000 },
            { "Temperature", 0.7m }
        };
        
        string docPath = Path.Combine(AppContext.BaseDirectory, "Documentation.md");
        _apiDocumentation = File.Exists(docPath) ? File.ReadAllText(docPath) : "";
    }

    public async Task<string> CreateMod(string modName, string modDescription)
    {
        ValidateInputParameters(modName, modDescription);

        try
        {
            var state = new ModGenerationState
            {
                Name = modName,
                Description = modDescription,
                ConversationHistory = new List<Message>()
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

    private async Task<ModGenerationState> GenerateInitialProposal(ModGenerationState state)
    {
        var messages = new List<Message>
        {
            new Message(RoleType.User, Prompts.NarratorBot.SystemContext),
            new Message(RoleType.User, $@"
                Create a mod with:
                Name: {state.Name}
                Description: {state.Description}
                Bot Objective: {_bot.Objective}

                {Prompts.NarratorBot.BaseRequirements}

                Available API Documentation:
                {_apiDocumentation}
            ")
        };

        var response = await LLM.Ask(messages, _defaultArgs);
        state.Proposal = response.Content?.FirstOrDefault()?.ToString();
        state.ConversationHistory.AddRange(messages);
        
        return state;
    }

    private async Task<ModGenerationState> ValidateAndRefineProposal(ModGenerationState state)
    {
        var messages = new List<Message>(state.ConversationHistory)
        {
            new Message(RoleType.User, $@"
                Validate this mod proposal:
                {state.Proposal}

                {Prompts.NarratorBot.ValidationCriteria}
            ")
        };

        var response = await LLM.Ask(messages, _defaultArgs);
        var validationResult = response.Content?.FirstOrDefault()?.ToString();
        
        if (!validationResult?.Contains("approved", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            throw new InvalidOperationException("Mod proposal failed validation criteria");
        }

        state.ConversationHistory.AddRange(messages);
        return state;
    }

    private async Task<ModGenerationState> ValidateModState(ModGenerationState state)
    {
        var messages = new List<Message>(state.ConversationHistory)
        {
            new Message(RoleType.User, $@"
                Verify state management for:
                {state.CSharpCode}

                {Prompts.NarratorBot.ValidationCriteria}
            ")
        };

        var response = await LLM.Ask(messages, _defaultArgs);
        state.ConversationHistory.AddRange(messages);
        return state;
    }

    private async Task<ModGenerationState> PerformTechnicalReview(ModGenerationState state)
    {
        var messages = new List<Message>(state.ConversationHistory)
        {
            new Message(RoleType.User, $@"
                Technical review for:
                {state.CSharpCode}

                {Prompts.NarratorBot.TechnicalCriteria}
            ")
        };

        var response = await LLM.Ask(messages, _defaultArgs);
        state.ConversationHistory.AddRange(messages);
        return state;
    }

    private async Task<ModGenerationState> GenerateImplementation(ModGenerationState state)
    {
        state.CSharpCode = await GenerateModuleCode(state);
        state.LuaScript = await GenerateLuaImplementation(state);
        return state;
    }

    private async Task<string> GenerateModuleCode(ModGenerationState state)
    {
        var messages = new List<Message>(state.ConversationHistory)
        {
            new Message(RoleType.User, $@"
                Generate C# implementation for:
                Class Name: {GetFormattedClassName(state.Name)}
                Proposal: {state.Proposal}

                {Prompts.NarratorBot.BaseRequirements}
            ")
        };

        var response = await LLM.Ask(messages, _defaultArgs);
        return response.Content?.FirstOrDefault()?.ToString();
    }

    private async Task<string> GenerateLuaImplementation(ModGenerationState state)
    {
        var messages = new List<Message>(state.ConversationHistory)
        {
            new Message(RoleType.User, Prompts.NarratorBot.SystemContext),
            new Message(RoleType.User, $@"
                Generate Lua implementation for:
                {state.Proposal}

                Available API Documentation:
                {_apiDocumentation}

                {Prompts.NarratorBot.BaseRequirements}
            ")
        };

        var response = await LLM.Ask(messages, _defaultArgs);
        return response.Content?.FirstOrDefault()?.ToString();
    }

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

    private string GetFormattedClassName(string modName)
    {
        return $"{char.ToUpper(modName[0])}{modName.Substring(1).Replace(" ", "")}";
    }
}
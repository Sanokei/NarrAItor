using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Interop;

using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;

using NarrAItor.Narrator.NarratorExceptions;
using NarrAItor.Narrator.Modding;
using NarrAItor.Narrator.Modding.Base;

namespace NarrAItor.Narrator;

public class NarratorBot : INarratorBot
{
    private readonly ModGeneration _modGeneration;
    private const int MAX_WORKSPACE_MESSAGES = 10;
    private const int OBJECTIVE_CHECK_INTERVAL = 5;

    public NarratorBot(
        string Name = "DefaultNarrator",
        string Version = "0.0.0",
        int MaxTotalToken = 0,
        string Objective = "Become the best narrator you can be.",
        string UserObjective = "You create modules for Narrator Bots.",
        string Personality = "You act like a Narrator.",
        string CurrentObjective = "",
        Script script = null,
        Dictionary<string, NarratorMod> InstalledMods = null,
        Dictionary<string, NarratorMod> ModsDirectory = null
    )
    {
        this.Name = Name;
        this.Version = Version;
        this.MaxTotalTokens = MaxTotalToken <= 0 ? this.MaxTotalTokens : MaxTotalToken;
        this.Objective = Objective;
        this.CurrentObjective = CurrentObjective;
        this.UserObjective = UserObjective;
        this.Personality = Personality;

        this.script = script ?? this.script;
        this.InstalledMods = InstalledMods ?? this.InstalledMods;
        this.ModsDirectory = ModsDirectory ?? this.ModsDirectory;
        this.RequiredMods = new();

        _modGeneration = new ModGeneration(this);
    }

    private Dictionary<string, Anthropic.SDK.Common.Tool> _tools = new();
    public List<Anthropic.SDK.Common.Tool> availableTools = new();
    private void InitializeToolset()
    {
        _tools = new Dictionary<string, Anthropic.SDK.Common.Tool>
        {
            {
                "analyze_objective",
                LLM.CreateToolFromFunc<string, Task<string>>(
                    "analyze_objective",
                    "Analyzes the current objective and breaks it down into actionable steps",
                    async (objective) =>
                    {
                        var args = new Dictionary<string, string> {
                            { "objective", objective },
                            { "personality", Personality },
                            { "context", "objective analysis" }
                        };
                        return (await LLM.GeneratePrompt(args, MaxTotalTokens, ""))?.Content?.FirstOrDefault()?.ToString() ?? "";
                    })
            },
            {
                "create_mod",
                LLM.CreateToolFromFunc<string, string, Task<string>>(
                    "create_mod",
                    "Creates a new mod with specified name and description",
                    async (name, description) =>
                    {
                        try
                        {
                            var result = await _modGeneration.CreateMod(name, description);
                            return $"Successfully created mod: {result}";
                        }
                        catch (Exception ex)
                        {
                            return $"Failed to create mod: {ex.Message}";
                        }
                    })
            },
            {
                "execute_mod",
                LLM.CreateToolFromFunc<string, Task<string>>(
                    "execute_mod",
                    "Executes a specified mod",
                    async (modName) =>
                    {
                        if (!RequiredMods.TryGetValue(modName, out var mod))
                        {
                            return $"Mod not found: {modName}";
                        }
                        await mod.Run();
                        return $"Executed mod: {modName}";
                    })
            },
            {
                "verify_objective_completion",
                LLM.CreateToolFromFunc<string, Task<bool>>(
                    "verify_objective_completion",
                    "Checks if the current objective has been completed",
                    async (objective) =>
                    {
                        var messages = workspace.TakeLast(MAX_WORKSPACE_MESSAGES).ToList();
                        messages.Add(new Message(RoleType.User, $"Has this objective been completed: {objective}?"));
                        var response = await LLM.Ask(messages, args);
                        return response.Content?.FirstOrDefault()?.ToString()?.Contains("completed", StringComparison.OrdinalIgnoreCase) ?? false;
                    })
            }
        };
        availableTools = _tools.Values.ToList();
    }
    public static void InitializeScript(Script script)
    {
        script.Options.DebugPrint = (x) => { Console.WriteLine(x); };
        script.Options.DebugInput = (x) => { return Console.ReadLine(); };

        ((ScriptLoaderBase)script.Options.ScriptLoader).IgnoreLuaPathGlobal = true;

        Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<TaskDescriptor>((script, task) =>
        {
            return DynValue.NewYieldReq(new[]
            {
                DynValue.FromObject(script, new AnonWrapper<TaskDescriptor>(task))
            });
        });


        script.Globals["AsAssistantMessage"] = (Func<string, DynValue>)(content =>
        {
            var table = new Table(script);
            table["role"] = "assistant";
            table["content"] = content;
            return DynValue.NewTable(table);
        });
    }
    public static void InitializeUserVarsTable(Script script)
    {
        if (script.Globals.Get("uservars").Type == DataType.Nil)
        {
            script.Globals["uservars"] = new Table(script);
        }
    }

    public void Initialize()
    {
        InitializeScript(script);
        InitializeUserVarsTable(script);
        InitializeToolset();
    }

    public async Task Run()
    {
        try
        {
            await ProcessObjective();
        }
        catch (Exception ex)
        {
            workspace.Add(new Message(RoleType.User, $"Error during execution: {ex.Message}"));
            throw;
        }
    }

    private async Task ProcessObjective()
    {
        if (string.IsNullOrEmpty(CurrentObjective))
        {
            CurrentObjective = Objective;
        }

        var objectiveAnalysis = await InvokeTool<string>("analyze_objective", CurrentObjective);
        workspace.Add(new Message(RoleType.User, $"Objective Analysis: {objectiveAnalysis}"));

        int messageCount = 0;
        bool objectiveCompleted = false;

        while (!objectiveCompleted && RemainingTokens > 0)
        {
            try
            {
                var messages = new List<Message>(workspace.TakeLast(MAX_WORKSPACE_MESSAGES))
                {
                    new Message(RoleType.User, "What action should be taken next to achieve the objective?")
                };

                var response = await LLM.Ask(messages, args);
                UpdateTokenUsage(response);

                var action = response.Content?.FirstOrDefault()?.ToString();

                if (!string.IsNullOrEmpty(action))
                {
                    workspace.Add(new Message(RoleType.User, $"Planned action: {action}"));
                    await ExecuteAction(action);
                }

                messageCount++;
                if (messageCount % OBJECTIVE_CHECK_INTERVAL == 0)
                {
                    objectiveCompleted = await InvokeTool<bool>("verify_objective_completion", CurrentObjective);
                }

                ManageWorkspaceSize();
            }
            catch (Exception ex)
            {
                workspace.Add(new Message(RoleType.User, $"Error during action execution: {ex.Message}"));
                throw;
            }
        }
    }

    private async Task ExecuteAction(string action)
    {
        if (action.Contains("create mod", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(action, @"create mod ['""]([^'""]+)['""] with description ['""]([^'""]+)['""]");
            if (match.Success)
            {
                await InvokeTool<string>("create_mod", match.Groups[1].Value, match.Groups[2].Value);
            }
        }
        else if (action.Contains("execute mod", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(action, @"execute mod ['""]([^'""]+)['""]");
            if (match.Success)
            {
                await InvokeTool<string>("execute_mod", match.Groups[1].Value);
            }
        }
    }

    private async Task<T> InvokeTool<T>(string toolName, params object[] parameters)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            throw new ArgumentException($"Tool '{toolName}' not found.", nameof(toolName));
        }

        var args = new Dictionary<string, object>
        {
            { "MaxTokens", 2048 },
            { "Temperature", 0.7m },
            { "Tools", new List<Anthropic.SDK.Common.Tool> { tool } }
        };

        var toolMessage = new Message(RoleType.User, $"Invoking tool: {tool.Function.Name}");
        workspace.Add(toolMessage);

        List<Message> messagesToSend = workspace.ToList();

        var response = await LLM.Ask(messagesToSend, args);

        workspace.Add(new Message(RoleType.Assistant, response.Content.FirstOrDefault()?.ToString()));

        try
        {
            // Handle cases where the response is a list of content
            if(response.Content != null && response.Content.Count > 0)
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)string.Join("",response.Content.Select(x => x.ToString()));
                }
                else if (typeof(T) == typeof(bool))
                {
                    return (T)(object)(response.Content.Any(x => x.ToString().Contains("completed", StringComparison.OrdinalIgnoreCase)));
                }
            }
            return (T)Convert.ChangeType(response.Content?.FirstOrDefault()?.ToString() ?? "", typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not convert tool response to type {typeof(T).Name}. Response Content: {JsonConvert.SerializeObject(response.Content)}", ex);
        }
    }
    
    private void ManageWorkspaceSize()
    {
        if (workspace.Count > MAX_WORKSPACE_MESSAGES * 2)
        {
            workspace = workspace.Skip(workspace.Count - MAX_WORKSPACE_MESSAGES).ToList();
        }
    }

    private void UpdateTokenUsage(MessageResponse response)
    {
        if (response?.Usage != null)
        {
            _currentUsage += new TokenUsage(response);
        }
    }

    // Properties and fields
    public Script script { get; set; } = new();
    public string Name { get; set; }
    public string Version { get; set; }
    public string Objective { get; set; }
    public string CurrentObjective { get; set; }
    public string UserObjective { get; set; }
    public string Personality { get; set; }
    public Dictionary<string, NarratorMod> ModsDirectory { get; set; } = new();
    public Dictionary<string, NarratorMod> RequiredMods { get; set; } = new();
    public Dictionary<string, NarratorMod> InstalledMods { get; set; } = new();
    public int MaxTotalTokens { get; set; } = 50000;

    private List<Message> workspace = new();
    private Dictionary<string, object> args = new();

    private TokenUsage _currentUsage;
    public int RemainingTokens => MaxTotalTokens - _currentUsage.Total;
    public record struct TokenUsage
    {
        public int InputTokens { get; init; }
        public int OutputTokens { get; init; }
        public int CacheCreationInputTokens { get; init; }
        public int CacheReadInputTokens { get; init; }

        public int Total => InputTokens + OutputTokens;

        public TokenUsage(MessageResponse response) : this(
            response.Usage?.InputTokens ?? 0,
            response.Usage?.OutputTokens ?? 0,
            response.Usage?.CacheCreationInputTokens ?? 0,
            response.Usage?.CacheReadInputTokens ?? 0)
        { }

        public TokenUsage(int inputTokens, int outputTokens, int cacheCreationTokens, int cacheReadTokens)
        {
            InputTokens = inputTokens;
            OutputTokens = outputTokens;
            CacheCreationInputTokens = cacheCreationTokens;
            CacheReadInputTokens = cacheReadTokens;
        }

        public static TokenUsage operator +(TokenUsage a, TokenUsage b) => new(
            a.InputTokens + b.InputTokens,
            a.OutputTokens + b.OutputTokens,
            a.CacheCreationInputTokens + b.CacheCreationInputTokens,
            a.CacheReadInputTokens + b.CacheReadInputTokens
        );
    }
}
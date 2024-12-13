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
    private readonly string styleGuidePath = Path.Join(Directory.GetCurrentDirectory(), "NarrAItor", "NarratorStyleGuide.md");
    private readonly string apiDocPath = Path.Join(Directory.GetCurrentDirectory(), "NarrAItor", "NarratorAPI.md");
    private readonly string readMePath = Path.Join(Directory.GetCurrentDirectory(), "NarrAItor", "README.md");

    public async Task Run()
    {
        // Read style guide and API documentation
        string styleGuide = File.ReadAllText(styleGuidePath);
        string apiDoc = File.ReadAllText(apiDocPath);
        string readme = File.ReadAllText(readMePath);
        
        // Add initial message defining who we are
        workspace.Add(new Message(RoleType.User, $"{Name} {Version}: {Personality}"));

        workspace.Add(new Message(RoleType.User, $@"{readme}\n{styleGuide}\n{apiDoc}"));

        // Process user objective to set current objective
        workspace.Add(new Message(RoleType.User, UserObjective));
        var response = await LLM.Ask(workspace, args);
        
        // Update current objective and overall objective based on response
        CurrentObjective = response.Content.FirstOrDefault()?.ToString();
        Objective = $"Process user request: {UserObjective}";
        
        // Add response to workspace for context
        workspace.Add(new Message(RoleType.Assistant, response.Content.FirstOrDefault()?.ToString()));
        
        // Update token tracking
        UpdateTokens(response.Usage.InputTokens, response.Usage.OutputTokens);
    }

    private readonly ModGeneration _modGeneration;

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

    public void Initialize(out Script m_script)
    {
        Initialize();
        m_script = script;
    }

    public void Initialize()
    {
        InitializeScript(script);
        InitializeUserVarsTable(script);
        InitializedArgs();
    }

    /// <summary>
    /// Handles tool initialization and configuration for the NarratorBot
    /// </summary>
    public void InitializedArgs()
    {
        // Initialize base configuration
        args = new Dictionary<string, object>
        {
            { "MaxTokens", 4000 },
            { "Temperature", 0.7m }
        };

        // Define available tools collection
        args["Tools"] = new List<Anthropic.SDK.Common.Tool>
        {
            // Core LLM interaction tool
            LLM.CreateToolFromFunc<List<Message>, Dictionary<string, object>, Task<MessageResponse>>(
                "think",
                "Enables direct LLM interaction for cognitive processing",
                LLM.Ask
            ),

            // Mod creation tool
            LLM.CreateToolFromFunc<string, string, Task<string>>(
                "create_mod",
                "Creates a new mod with specified name and description",
                async (name, description) =>
                {
                    try
                    {
                        var result = await _modGeneration.CreateMod(name,description);
                        return $"Successfully created mod: {result}";
                    }
                    catch (Exception ex)
                    {
                        return $"Failed to create mod: {ex.Message}";
                    }
                }
            ),

            // Mod execution tool
            LLM.CreateToolFromFunc<string, Task<string>>(
                "run_mod",
                "Executes a specified mod by name",
                async (modName) =>
                {
                    try
                    {
                        // Verify mod exists
                        if (!RequiredMods.TryGetValue(modName, out var mod))
                        {
                            return $"Mod not found: {modName}";
                        }

                        // Initialize if needed
                        if (mod.script == null)
                        {
                            mod.Initialize();
                        }

                        // Execute the mod
                        await mod.Run();
                        return $"Successfully executed mod: {modName}";
                    }
                    catch (Exception ex)
                    {
                        return $"Failed to execute mod: {ex.Message}";
                    }
                }
            )
        };
    }

    /// <summary>
    /// Retrieves a list of currently loaded mods
    /// </summary>
    /// <returns>String containing mod names and their status</returns>
    private string GetLoadedMods()
    {
        var modList = RequiredMods.Select(kvp => 
            $"- {kvp.Key}: {(kvp.Value.script != null ? "Initialized" : "Not Initialized")}"
        );
        return string.Join("\n", modList);
    }

    /// <summary>
    /// Validates if a mod name is acceptable for creation
    /// </summary>
    /// <param name="modName">The proposed name for the mod</param>
    /// <returns>True if the name is valid, false otherwise</returns>
    private bool IsValidModName(string modName)
    {
        if (string.IsNullOrWhiteSpace(modName)) return false;
        
        // Check if mod already exists
        if (RequiredMods.ContainsKey(modName)) return false;
        
        // Validate name format (alphanumeric with underscores)
        return Regex.IsMatch(modName, @"^[a-zA-Z0-9_]+$");
    }

    // Properties
    public Script script { get; set; } = new();
    public string Name { get; set; } = "DefaultNarrator";
    public string Version { get; set; } = "0.0.0";
    public string Objective { get; set; } = "Become the best narrator you can be.";
    public string CurrentObjective { get; set; } = "";
    public string UserObjective { get; set; } = "You create modules for Narrator Bots.";
    public string Personality { get; set; } = "You act like a Narrator.";
    public Dictionary<string, NarratorMod> ModsDirectory { get; set; } = new();
    public Dictionary<string, NarratorMod> RequiredMods { get; set; } = new();
    public Dictionary<string, NarratorMod> InstalledMods { get; set; } = new();
    public int MaxTotalTokens { get; set; } = 50000;

    // Basic workspace and args
    private List<Message> workspace = new();
    private Dictionary<string, object> args = new();

    // Token tracking
    public record struct TokenUsage(int Input, int Output)
    {
        public int Total => Input + Output;
    }
    
    private TokenUsage _currentUsage;
    public int RemainingTokens => MaxTotalTokens - _currentUsage.Total;
    
    public void UpdateTokens(int input, int output)
    {
        _currentUsage = new TokenUsage(
            _currentUsage.Input + input,
            _currentUsage.Output + output
        );
    }
}
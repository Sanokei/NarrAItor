using System.IO;
using Newtonsoft.Json;
using NarrAItor.Narrator.NarratorExceptions;
using NarrAItor.Narrator.Modding;
using NarrAItor.Narrator.Modding.Base;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Interop;
using System.Threading.Tasks;

using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Anthropic.SDK.Common;

namespace NarrAItor.Narrator;
public class NarratorBot : INarratorBot
{
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

        Initialize();
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
    }

    public async Task<string> GenerateModProposal(string modName, string modDescription)
    {
        if (string.IsNullOrWhiteSpace(modName))
            throw new ArgumentException("Mod name cannot be empty", nameof(modName));

        if (string.IsNullOrWhiteSpace(modDescription))
            throw new ArgumentException("Mod description cannot be empty", nameof(modDescription));

        var messages = new List<Message>
        {
            new Message(RoleType.User, $@"Create a comprehensive mod proposal for a mod named '{modName}'.
                    Description: {modDescription}

                    Provide a detailed proposal including:
                    1. Mod Concept Overview
                    - Core functionality
                    - Potential use cases
                    - Unique features

                    2. Lua Script Structure
                    - Function signatures
                    - Key methods
                    - User variable interactions
                    - Event handling patterns

                    3. C# Mod Implementation
                    - Inheritance from NarratorMod base class
                    - Proposed method implementations
                    - Interaction with NarratorAPI

                    4. Suggested Interaction Methods
                    - How the mod will integrate with existing systems
                    - Potential extension points

                    Style Guidelines:
                    - Use uservars for persistent state management
                    - Keep error handling minimal and focused
                    - Prioritize readability and purpose
                    - Leverage NarratorAPI capabilities
                    - Follow modding system's design principles")
        };

        var response = await LLM.Ask(messages, new Dictionary<string, object>
        {
            { "MaxTokens", 4000 },
            { "Temperature", 0.7 },
            {
                "System", @"You are an expert C# and Lua mod generator for a Narrator Bot modding system.
                    - Generate a comprehensive and creative mod proposal
                    - Ensure technical feasibility and modularity
                    - Follow the provided style guidelines
                    - Create a mod that demonstrates innovation
                    - Use clear, concise, and implementable code structures"
            }
        });

        string modProposal = response.Content.FirstOrDefault().ToString();
        string modClassName = $"{char.ToUpper(modName[0])}{modName.Substring(1).Replace(" ", "")}Mod";

        string modFileContent = await GenerateModFile(modClassName, modProposal);

        string luaScriptContent = await GenerateLuaScript(modName, modProposal);

        string modsDirectory = Path.Combine(AppContext.BaseDirectory, "Mods");
        Directory.CreateDirectory(modsDirectory);

        string csFilePath = Path.Combine(modsDirectory, $"{modClassName}.cs");
        string luaFilePath = Path.Combine(modsDirectory, $"{modName.Replace(" ", "_")}.lua");

        File.WriteAllText(csFilePath, modFileContent);
        File.WriteAllText(luaFilePath, luaScriptContent);

        NarratorMod newMod = new NarratorMod(modName, luaScriptContent, script, RequiredMods);
        RequiredMods[modName] = newMod;
        InstalledMods[modName] = newMod;

        return csFilePath;
    }

    public async Task<string> GenerateModFile(string modClassName, string modProposal)
    {
        var messages = new List<Message>
        {
            new Message(RoleType.User, $@"
                    Based on the following mod proposal, generate a complete C# code for the mod class '{modClassName}'.
                    
                    Mod Proposal:
                    {modProposal}

                    The generated C# class must:
                    1. Inherit from NarratorMod.
                    2. Implement the necessary methods (e.g., OnStart, OnEvent) as defined in the proposal.
                    3. Integrate with the NarratorAPI and follow the design principles.
                    4. Be modular, readable, and functional.
                    5. Be capable of dynamically interacting with Lua scripts if necessary.
                    6. Include comments explaining the purpose of each section and method.

                    The code should be in standard C# format, suitable for integration with the Narrator Bot system."
            )
        };

        var response = await LLM.Ask(messages, new Dictionary<string, object>
        {
            { "MaxTokens", 4000 },
            { "Temperature", 0.7 },
            {
                "System", @"You are an expert in generating mod code for the Narrator Bot system. Your task is to generate clean, modular, and functional C# code for mods that integrate with the NarratorAPI."
            }
        });

        string modFileContent = response.Content.FirstOrDefault()?.ToString();

        if (string.IsNullOrWhiteSpace(modFileContent))
        {
            throw new InvalidOperationException("Failed to generate mod file content.");
        }

        return modFileContent;
    }

    public async Task<string> GenerateLuaScript(string modName, string modProposal)
    {
        var messages = new List<Message>
        {
            new Message(RoleType.User, $@"
                    Based on the following mod proposal, generate a Lua script for the mod '{modName}'.
                    
                    Mod Proposal:
                    {modProposal}

                    The Lua script must:
                    1. Use the provided user variables and arguments.
                    2. Integrate with the C# mod for dynamic interaction.
                    3. Include event handling, variable management, and any specific behavior described in the proposal.
                    4. Be written in Lua, following standard practices for modding in the Narrator Bot system.
                    5. Include clear, concise comments to explain how each section of the script works.

                    The code should be in standard Lua format, suitable for integration with the Narrator Bot system."
            )
        };

        var response = await LLM.Ask(messages, new Dictionary<string, object>
        {
            { "MaxTokens", 4000},
            { "Temperature", 0.7 },
            {
                "System", @"You are an expert in generating Lua script code for the Narrator Bot system. Your task is to generate clean, modular, and functional Lua code for mods that integrate with the C# mod."
            }
        });

        string luaScriptContent = response.Content.FirstOrDefault()?.ToString();

        if (string.IsNullOrWhiteSpace(luaScriptContent))
        {
            throw new InvalidOperationException("Failed to generate Lua script content.");
        }

        return luaScriptContent;
    }

    public async Task Run()
    {
        var workspace = new List<Message>();
        var args = new Dictionary<string, object>
        {
            { "MaxTokens", 4000 },
            { "Temperature", 0.7 },
        };

        args["Tools"] = new List<Anthropic.SDK.Common.Tool>
        {
            LLM.CreateToolFromFunc<List<Message>, Dictionary<string, object>, Task<MessageResponse>>(
                "think",
                "This tool allows the NarratorBot to send a request to an LLM and receive a response.",
                LLM.Ask
            ),
            LLM.CreateToolFromFunc<string, string, Task<string>>(
                "createmod",
                "This tool allows the NarratorBot to create a mod with the given name and Lua code.",
                async (modName, luaCode) =>
                {
                    var mod = new NarratorMod(modName, luaCode, script, InstalledMods);
                    InstalledMods.Add(modName, mod);
                    await mod.Run();
                    return "Mod created and executed successfully.";
                }
            )
        };

        var phases = new Dictionary<string, (int, int)>
        {
            { "Exploratory", (1, 1800) },
            { "Refinement", (1801, 5000) },
            { "Validation", (5001, 8000) },
            { "Deployment", (8001, int.MaxValue) }
        };

        CurrentTokenUsage = new TokenUsageStruct();

        string currentPhase = "Exploratory";
        bool isModApproved = false;
        string modProposal = "";
        string luaCode = "";

        while (CurrentTokenUsage.TotalTokensUsed < MaxTotalTokens)
        {
            currentPhase = DetermineNextPhase(CurrentTokenUsage.TotalTokensUsed, phases);

            (modProposal, luaCode) = await GetCurrentState(workspace, args, currentPhase);

            (int tokensSpent, isModApproved) = await EvaluateModProposal(modProposal, luaCode, currentPhase, workspace, args);
            CurrentTokenUsage.InputTokens += tokensSpent;

            if (tokensSpent == 0)
            {
                workspace.Clear();
                workspace.Add(new Message(RoleType.User, "The mod has been scrapped. Propose a new mod to solve the problem."));
                var response = await LLM.Ask(workspace, args);
                AddMessagesToWorkspace(workspace, response);
                (modProposal, luaCode) = await GetCurrentState(workspace, args, currentPhase);
                continue;
            }

            if (isModApproved)
            {
                break;
            }

            workspace.Clear();
            workspace.Add(new Message(RoleType.User,
                $"The judges spent {tokensSpent} tokens and the current phase is '{currentPhase}'. " +
                "You can continue refining the mod proposal and Lua code, or you can use the 'createmod' tool to create and execute a mod."));

            var refinementResponse = await LLM.Ask(workspace, args);
            AddMessagesToWorkspace(workspace, refinementResponse);
        }

        if (isModApproved)
        {
            SaveModProposal(modProposal, "mod_proposal.txt");
            SaveLuaCode(luaCode, "generated_mod.lua");

            var mod = new NarratorMod("GeneratedMod", luaCode, script, RequiredMods);
            InstalledMods.Add("GeneratedMod", mod);
            await mod.Run();
        }
    }
    private async Task<(string, string)> GetCurrentState(List<Message> workspace, Dictionary<string, object> args, string currentPhase)
    {
        string modProposal = "";
        string luaCode = "";

        if (workspace.Any()) // Check if the workspace has any messages
        {
            // Extract from the last ASSISTANT message
            var lastAssistantMessage = workspace.LastOrDefault(m => m.Role == RoleType.Assistant);
            if(lastAssistantMessage != null)
            {
                modProposal = ExtractModProposalFromContent(lastAssistantMessage.Content);
                luaCode = ExtractLuaCodeFromContent(lastAssistantMessage.Content);
            }
        }


        if (string.IsNullOrEmpty(modProposal) || string.IsNullOrEmpty(luaCode))
        {
            workspace.Clear();
            workspace.Add(new Message(RoleType.User, "Propose a mod and corresponding Lua code to solve a problem."));
            var response = await LLM.Ask(workspace, args);
            AddMessagesToWorkspace(workspace, response);

            // Extract directly from response content
            modProposal = ExtractModProposalFromContent(response.Content);
            luaCode = ExtractLuaCodeFromContent(response.Content);
        }

        return (modProposal, luaCode);
    }

    private async Task<(int, bool)> EvaluateModProposal(string modProposal, string luaCode, string currentPhase, List<Message> workspace, Dictionary<string, object> args)
    {
        int estimatedTokensForEvaluation = EstimateTokens(modProposal) + EstimateTokens(luaCode) + 200;
        if (!CanProcessTokens(estimatedTokensForEvaluation))
        {
            Console.WriteLine("Not enough tokens remaining for evaluation.");
            return (0, false);
        }

        workspace.Clear();
        workspace.Add(new Message(RoleType.User,
            $@"Evaluate the following mod proposal and Lua code in the context of the '{currentPhase}' phase:
            Mod Proposal:
            {modProposal}

            Lua Code:
            ```lua
            {luaCode}
            ```
            Consider:

            Does the Lua code implement the proposal's functionality?
            Are there any errors or potential issues in the code?
            Is the code well-structured and readable?
            Is the proposal clear and feasible?
            Respond with:

            The number of tokens you spent on this evaluation.

            'true' if the mod is approved, 'false' otherwise.

            Optional: A short explanation of your decision.")
        );

        var response = await LLM.Ask(workspace, args);
        AddMessagesToWorkspace(workspace, response);

        int tokensSpent = CountTokensInMessages(response.Content.ToList());
        bool isModApproved = false;
        string llmResponse = response.Content.LastOrDefault()?.ToString() ?? "";

        if (llmResponse.Contains("true", StringComparison.OrdinalIgnoreCase) || llmResponse.Contains("approved", StringComparison.OrdinalIgnoreCase))
        {
            isModApproved = true;
        }
        else if (llmResponse.Contains("false", StringComparison.OrdinalIgnoreCase) || llmResponse.Contains("rejected", StringComparison.OrdinalIgnoreCase) || llmResponse.Contains("not approved", StringComparison.OrdinalIgnoreCase))
        {
            isModApproved = false;
        }
        else
        {
            Console.WriteLine($"Warning: Unexpected evaluation response format: {llmResponse}");
        }

        return (tokensSpent, isModApproved);
    }

     private string ExtractModProposalFromContent(List<ContentBase> content)
    {
        if (content == null || !content.Any()) return "";

        foreach (var contentBase in content)
        {
            if (contentBase is TextContent textContent)
            {
                var match = Regex.Match(textContent.Text, @"Mod Proposal:\n(.*?)\nLua Code:", RegexOptions.Singleline);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }
        return "";
    }
    private string ExtractLuaCodeFromContent(List<ContentBase> content)
    {
        if (content == null || !content.Any()) return "";
        foreach (var contentBase in content)
        {
            if (contentBase is TextContent textContent)
            {
                var match = Regex.Match(textContent.Text, @"```lua\n(.*?)\n", RegexOptions.Singleline);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }
        return "";
    }

    private int CountTokensInMessages(List<ContentBase> content)
    {
        if (content == null || !content.Any())
        {
            return 0;
        }
        int totalTokens = 0;
        foreach (var contentBase in content)
        {
            if (contentBase is TextContent textContent)
            {
                totalTokens += (int)Math.Ceiling(textContent.Text.Split().Length * 0.75);
            }
            else if (contentBase is ImageContent)
            {
                totalTokens += 1;
            }
        }
        return totalTokens;
    }


    private void AddMessagesToWorkspace(List<Message> workspace, MessageResponse response)
    {
        if (response?.Content == null || !response.Content.Any())
        {
            Console.WriteLine("Warning: Null or empty response content received.");
            return;
        }

        foreach (var contentBase in response.Content)
        {
            if (contentBase is TextContent textContent) // Only add TextContent as Message
            {
                workspace.Add(new Message(RoleType.Assistant, textContent.Text));
            }
            else
            {
                Console.WriteLine($"Warning: Non-text content received and ignored: {contentBase.GetType().Name}");
            }
        }
       CurrentTokenUsage.OutputTokens += CountTokensInContent(response.Content);
    }

    private string DetermineNextPhase(int totalTokensUsed, Dictionary<string, (int, int)> phases)
    {
        return phases.FirstOrDefault(p => totalTokensUsed >= p.Value.Item1 && totalTokensUsed <= p.Value.Item2).Key ?? "Deployment";
    }

    private string ExtractModProposal(Message message)
    {
        if (message == null || message.Content == null || !message.Content.Any()) return "";

        foreach (var contentBase in message.Content)
        {
            if (contentBase is TextContent textContent)
            {
                var match = Regex.Match(textContent.Text, @"Mod Proposal:\n(.*?)\nLua Code:", RegexOptions.Singleline);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }
        return ""; // Return empty string if no match is found in any content part
    }

    private string ExtractLuaCode(Message message)
    {
        if (message == null || message.Content == null || !message.Content.Any()) return "";

        foreach (var contentBase in message.Content)
        {
            if (contentBase is TextContent textContent)
            {
                var match = Regex.Match(textContent.Text, @"```lua\n(.*?)\n", RegexOptions.Singleline);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }
        return ""; // Return empty string if no match is found in any content part
    }

    private int CountTokensInContent(List<ContentBase> content)
    {
        if (content == null || content.Count == 0)
        {
            return 0;
        }

        int totalTokens = 0;
        foreach (var contentBase in content)
        {if (contentBase is TextContent textContent)
            {
                totalTokens += (int)Math.Ceiling(textContent.Text.Split().Length * 0.75);
            }
            else if (contentBase is ImageContent)
            {
                totalTokens += 1;
            }
        }
        return totalTokens;
    }

    private string ExtractLuaCode(MessageResponse response)
    {
        var regex = new Regex(@"```lua\n(.*?)```", RegexOptions.Singleline);
        var match = regex.Match(response.Content.Last().ToString());

        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return "No lua code found.";
    }

    private void SaveModProposal(string modProposal, string fileName)
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        File.WriteAllText(filePath, modProposal);
    }

    private void SaveLuaCode(string luaCode, string fileName)
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        File.WriteAllText(filePath, luaCode);
    }

    private int EstimateTokens(string text)
    {
        return (int)Math.Ceiling(text.Split().Length * 0.75);
    }

    private int CountTokensInMessages(List<Message> messages)
    {
        int totalTokens = 0;
        foreach (var message in messages)
        {
            totalTokens += EstimateTokens(message.Content.FirstOrDefault().ToString());
        }
        return totalTokens;
    }



    Script _script = new();
    public Script script { get => _script; set => _script = value; }
    string _Name = "DefaultNarrator";
    public string Name { get => _Name; set => _Name = value; }

    string _Version = "0.0.0";
    public string Version { get => _Version; set => _Version = value; }

    string _Objective = "Become the best narrator you can be.";
    public string Objective { get => _Objective; set => _Objective = value; }

    string _CurrentObjective = "";
    public string CurrentObjective { get => _CurrentObjective; set => _CurrentObjective = value; }

    string _UserObjective = "You create modules for Narrator Bots.";
    public string UserObjective { get => _UserObjective; set => _UserObjective = value; }

    string _Personality = "You act like a Narrator.";
    public string Personality { get => _Personality; set => _Personality = value; }

    Dictionary<string, NarratorMod> _ModsDirectory = new();
    public Dictionary<string, NarratorMod> ModsDirectory { get; set; }

    Dictionary<string, NarratorMod> _InstalledMods = new();
    public Dictionary<string, NarratorMod> InstalledMods { get => _InstalledMods; set => _InstalledMods = value; }

    private Dictionary<string, NarratorMod> _RequiredMods = new();
    public Dictionary<string, NarratorMod> RequiredMods { get => _RequiredMods; set => _RequiredMods = value; }

    int _MaxTotalTokens = 50000;
    public int MaxTotalTokens { get => _MaxTotalTokens; set => _MaxTotalTokens = value; }

    public struct TokenUsageStruct
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokensUsed => InputTokens + OutputTokens;
    }

    public TokenUsageStruct CurrentTokenUsage = new TokenUsageStruct();
    public int RemainingTokens => MaxTotalTokens - CurrentTokenUsage.TotalTokensUsed;

    public bool CanProcessTokens(int requestedTokens)
    {
        return RemainingTokens >= requestedTokens;
    }

    public void UpdateTokenUsage(int inputTokens, int outputTokens)
    {
        CurrentTokenUsage.InputTokens += inputTokens;
        CurrentTokenUsage.OutputTokens += outputTokens;
    }
}
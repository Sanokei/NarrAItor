using System.IO;
using Newtonsoft.Json;
using NarrAItor.Narrator.NarratorExceptions;
using NarrAItor.Narrator.Modding;
using NarrAItor.Narrator.Modding.Base;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Interop;
using System.Threading.Tasks;

namespace NarrAItor.Narrator;
public class NarratorBot : INarratorBot
{
    public static void InitializeScript(Script script)
    {

        // UserData.RegisterType<NarratorApi>();   
        // FIXME: set path.
        // ((ScriptLoaderBase)script.Options.ScriptLoader).ModulePaths = ScriptLoaderBase.UnpackStringPaths(System.IO.Path.Combine("/modules/","?") + ".lua");
        
        script.Options.DebugPrint = (x) => {Console.WriteLine(x);};
        script.Options.DebugInput = (x) => { return Console.ReadLine(); };
        
        ((ScriptLoaderBase)script.Options.ScriptLoader).IgnoreLuaPathGlobal = true;
        
        Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<TaskDescriptor>((script, task) =>
        {
            // Important !!!
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
        // script goes through some global and local changes before it leaves
        // so this function could be useful, but dangerous.
        Initialize();
        m_script = script;
    }
    public void Initialize()
    {
        InitializeScript(script);
        InitializeUserVarsTable(script);
    }
    
    public Task Run()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Initializes a new instance of the NarratorBot class.
    /// </summary>
    /// <param name="Name">The name of the narrator bot.</param>
    /// <param name="Version">The version of the narrator bot.</param>
    /// <param name="Objective">The objective of the narrator bot.</param>
    /// <param name="CurrentObjective">The current objective of the narrator bot.</param>
    /// <param name="UserObjective">The user's objective for the narrator bot.</param>
    /// <param name="Personality">The personality of the narrator bot.</param>
    /// <param name="script">The script used by the narrator bot.</param>
    /// <param name="InstalledMods">The installed mods for the narrator bot.</param>
    /// <param name="ModsDirectory">The directory of mods for the narrator bot.</param>
    public NarratorBot(
        string Name = "DefaultNarrator",
        string Version = "0.0.0",
        string Objective = "Become the best narrator you can be.",
        string CurrentObjective = "",
        string UserObjective = "You create modules for Narrator Bots.",
        string Personality = "You act like a Narrator.",
        Script script = null,
        Dictionary<string, NarratorMod> InstalledMods = null,
        Dictionary<string, NarratorMod> ModsDirectory = null
    )
        {
        this.Name = Name;
        this.Version = Version;
        this.Objective = Objective;
        this.CurrentObjective = CurrentObjective;
        this.UserObjective = UserObjective;
        this.Personality = Personality;

        this.script = script ?? this.script;
        this.InstalledMods = InstalledMods ?? this.InstalledMods;
        this.ModsDirectory = ModsDirectory ?? this.InstalledMods;;
        
        Initialize();
    }

    // Interface
    Script _script = new(); // FIXME: NarratorBot should hold better ownership of it's script.
    public Script script{get => _script; set => _script = value;}
    string _Name = "DefaultNarrator";
    public string Name { get => _Name; set => _Name = value;}

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

    Dictionary<string, NarratorMod> _ModsDirectory = [];
    public Dictionary<string, NarratorMod> ModsDirectory { get;set; }

    Dictionary<string, NarratorMod> _InstalledMods = [];
    public Dictionary<string, NarratorMod> InstalledMods { get => _InstalledMods; set => _InstalledMods = value; }

    int _MaxTokens = 1500;
    public int MaxTokens { get => _MaxTokens; set => _MaxTokens = value; } // cant have anything lower than 1: value < 1 ? 5000 : value; fix?
    int _CurrentTokenCount = 0;
    public int CurrentTokenCount { get => _CurrentTokenCount; set => _CurrentTokenCount = value; }
    int _ContextWindow = 20000;
    public int ContextWindow { get => _ContextWindow; set => _ContextWindow = value; }
}
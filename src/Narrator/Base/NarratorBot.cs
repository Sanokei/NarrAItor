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
    private void InitializeScript()
    {
        UserData.RegisterType<NarratorApi>();   
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
    private void InitializeUserVarsTable()
    {
        if (script.Globals.Get("uservars").Type == DataType.Nil)
        {
            script.Globals["uservars"] = new Table(script);
        }
    }

    public void Initialize()
    {
        InitializeScript();
        InitializeUserVarsTable();
    }

    public Task Run()
    {
        throw new NotImplementedException();
    }

    //
    public NarratorBot()
    {
    }

    // Interface
    Script _script = new(); // FIXME: NarratorBot should hold better ownership of script.
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
using System;
using System.Threading;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.CoreLib.IO;
using MoonSharp.Interpreter.Loaders;

using NarrAItor.Narrator;

namespace NarrAItor.Narrator.Modding;

class NarratorMod
{
    static Timer _UpdateTimer;
    static readonly object _LockObject = new object();
    public string LuaFileData = "";
    // Asssume the path and NarratorName are different.
    public string PathToMod = "";
    public NarratorMod(string RelativePath, string LuaFileData)
    {
        this.LuaFileData = LuaFileData;
        this.PathToMod = Path.Join(Directory.GetCurrentDirectory(), RelativePath);
    }

    public void Initialize()
    {
        UserData.RegisterAssembly();
        // Because you only can set one narrator and not mulitple this is going to just be a global
        script.Globals["narrator"] = new NarratorApi(new NarratorObject());
    }

    void Update(object state)
    {
        lock (_LockObject)
        {
            onUpdate?.Invoke();
        }
    }
    public ScriptFunctionDelegate onAwake, onStart, onUpdate;
    
    public Script script = new();

    public void Run()
    {
        // UserData.RegisterAssembly();

        
        script.Options.DebugPrint = (x) => {Console.WriteLine(x);};
        ((ScriptLoaderBase)script.Options.ScriptLoader).IgnoreLuaPathGlobal = true;
        // FIXME: set path.
        // ((ScriptLoaderBase)script.Options.ScriptLoader).ModulePaths = ScriptLoaderBase.UnpackStringPaths(System.IO.Path.Combine("/modules/","?") + ".lua");
        
        
        DynValue fn = script.DoString(LuaFileData);

        onAwake = script.Globals.Get("Awake") != DynValue.Nil ? script.Globals.Get("Awake").Function.GetDelegate() : null;
        onStart = script.Globals.Get("Start") != DynValue.Nil ? script.Globals.Get("Start").Function.GetDelegate() : null;

        onAwake?.Invoke();
        onStart?.Invoke();   
        
        _UpdateTimer = new Timer(Update, null, 0, 16);
        onUpdate = script.Globals.Get("Update") != DynValue.Nil ? script.Globals.Get("Update").Function.GetDelegate() : null;
    }
}

[MoonSharpUserData]
class NarratorApi
{
    public NarratorObject Narrator;
    internal NarratorApi(NarratorObject Narrator)
    {
        this.Narrator = Narrator;
    }
    /// <summary>
    /// Use <> text to speech to speak the phrase given.
    /// </summary>
    /// <param name="Phrase">The phrase to be spoken</param>
    public void say(string Phrase)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// Prints Phrase to Console
    /// </summary>
    /// <param name="Phrase">The phrase to be printed to the console</param>
    public void print(string Phrase)
    {
        Console.WriteLine(Phrase);
    }
}
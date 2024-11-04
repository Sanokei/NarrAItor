using System;
using System.Threading;

using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.CoreLib.IO;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Interop;

using NarrAItor.Narrator;
using System.Reflection.Metadata;
using NarrAItor.Narrator.Modding.Base;

namespace NarrAItor.Narrator.Modding;
/// <summary>
/// Acts as the base mod for all
/// </summary>
public class NarratorMod : INarratorMod
{
    // The delegate will be moved to the bot.
    // // FIXME: ParentBot isn't set yet, so invoking OnAddedEvent(ParentBot) will just be with (empty object)
    // public NarratorMod()
    // {
    //     INarratorMod.OnAddedEvent += OnEnable;
    //     INarratorMod.OnRemovedEvent += OnDisable;
    // }
    
    /*
        //WARNING: This MAY be the way to do it, im not really sure.

        So the way I see it right now is that, script control within the mod is not strictly nessesary.
        It may be more optimal to have every mod have their own script so during NEAIL stage, it could run in parrallel.
        As the Google C++ style guide says, always bend to optimization.
    */
    public Script script; // = new();
    
    public ScriptFunctionDelegate onAwake, onStart, onUpdate;
    string _LuaFileData = "";
    public string LuaFileData { get => _LuaFileData; set => _LuaFileData = value; }
    private INarratorBot _ParentBot;
    public INarratorBot ParentBot{get => _ParentBot;set => _ParentBot = value;}

    // fucking run-order shit
    public void OnEnable(INarratorBot ParentBot)
    {
        this.ParentBot = ParentBot;
    }

    public void OnDisable(INarratorBot ParentBot)
    {
        this.ParentBot = null;
    }
    static Timer _UpdateTimer;
    static readonly object _LockObject = new object();

    /// <summary>
    /// Runs when added to a Narrator Bot.
    /// </summary>
    ///
    public void Initialize()
    {
        script.Globals[this.GetType().Name] = new NarratorApi(this);
        new NarratorMod();
    }

    void Update(object state)
    {
        lock (_LockObject)
        {
            onUpdate?.Invoke();
        }
    }

    public async Task Run()
    {
        // UserData.RegisterAssembly();
        onAwake = script.Globals.Get("Awake") != DynValue.Nil ? script.Globals.Get("Awake").Function.GetDelegate() : null;
        onStart = script.Globals.Get("Start") != DynValue.Nil ? script.Globals.Get("Start").Function.GetDelegate() : null;
        
        _UpdateTimer = new Timer(Update, null, 0, 16);

        try
        {
            DynValue fn = await script.DoStringAsync(LuaFileData);
        }
        catch (Exception e)
        {
            // give the error back to the llm?
            
            Console.WriteLine("The lua script abort with exception. \n{0}", e);
        } 
        onAwake?.Invoke();
        onStart?.Invoke();  
        onUpdate = script.Globals.Get("Update") != DynValue.Nil ? script.Globals.Get("Update").Function.GetDelegate() : null; 
    }

    public static List<Message> BuildMessagesListFromTable(Table MessagesTable)
    {
        var messages = new List<Message>();

        foreach (TablePair pair in MessagesTable.Pairs)
        {
            if (pair.Value.Type == DataType.String)
            {
                messages.Add(new Message (RoleType.User, pair.Value.String ));
            }
            else if (pair.Value.Type == DataType.Table)
            {
                var table = pair.Value.Table;
                if (table.Get("role").String == "assistant") // still O(N+1)? lol
                {
                    messages.Add(new Message (RoleType.Assistant, table.Get("content").String ));
                }
            }
        }
        return messages;
    }
}
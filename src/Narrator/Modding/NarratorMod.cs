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

namespace NarrAItor.Narrator.Modding;

class NarratorMod
{
    static Timer _UpdateTimer;
    static readonly object _LockObject = new object();
    public int MaxTokens = 250;
    public string LuaFileData = "";
    // Asssume the path and NarratorName are different.
    public string PathToMod = "";
    public NarratorMod(string RelativePath, string LuaFileData)
    {
        this.LuaFileData = LuaFileData;
        this.PathToMod = Path.Join(Directory.GetCurrentDirectory(), RelativePath);
    }
    private void InitializeUserVarsTable()
    {
        if (script.Globals.Get("uservars").Type == DataType.Nil)
        {
            script.Globals["uservars"] = new Table(script);
        }
    }

    public Script Initialize()
    {
        UserData.RegisterType<NarratorApi>();
        
        // FIXME: set path.
        // ((ScriptLoaderBase)script.Options.ScriptLoader).ModulePaths = ScriptLoaderBase.UnpackStringPaths(System.IO.Path.Combine("/modules/","?") + ".lua");
        
        script.Options.DebugPrint = (x) => {Console.WriteLine(x);};
        ((ScriptLoaderBase)script.Options.ScriptLoader).IgnoreLuaPathGlobal = true;
        
        Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<TaskDescriptor>((script, task) =>
        {
            // Important !!!
            return DynValue.NewYieldReq(new[]
            {
                DynValue.FromObject(script, new AnonWrapper<TaskDescriptor>(task))
            });
        });
        // Because you only can set one narrator and not mulitple this is going to just be a global
        script.Globals["narrator"] = new NarratorApi(this, new NarratorBot());
        
        script.Globals["AsAssistantMessage"] = (Func<string, DynValue>)(content =>
        {
            var table = new Table(script);
            table["role"] = "assistant";
            table["content"] = content;
            return DynValue.NewTable(table);
        });

        InitializeUserVarsTable();

        return script;
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
                if (table.Get("role").String == "assistant")
                {
                    messages.Add(new Message (RoleType.Assistant, table.Get("content").String ));
                }
            }
        }
        return messages;
    }

    public DynValue BuildDynValueFromMessagesAndResponse(List<Message> messages, MessageResponse response)
    {
        var table = new Table(script);
        var messagesTable = new Table(script);
        // Add original messages to the table
        for (int i = 0; i < messages.Count; i++)
        {
            var msgTable = new Table(script);
            msgTable["role"] = messages[i].Role == RoleType.User ? "user" : "assistant";
            msgTable["content"] = messages[i].ToString();
            messagesTable[i + 1] = DynValue.NewTable(msgTable);
        }
        
        // Add the new response message to the table
        var responseTable = new Table(script);
        responseTable["role"] = "assistant";
        responseTable["content"] = response.Message.ToString();
        messagesTable[messages.Count + 1] = DynValue.NewTable(responseTable);
        
        table["messages"] = DynValue.NewTable(messagesTable);

        // Add the raw response content as a separate field
        table["content"] = response.Message.ToString();

        return DynValue.NewTable(table);
    }
}
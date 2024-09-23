using System;
using System.Threading;

using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.CoreLib.IO;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Interop;

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
        script.Globals["narrator"] = new NarratorApi(this, new NarratorObject());
        script.Globals["AsAssistantMessage"] = (Func<string, DynValue>)(content =>
        {
            var table = new Table(script);
            table["role"] = "assistant";
            table["content"] = content;
            return DynValue.NewTable(table);
        });

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
    public static List<Message> BuildMessagesListFromTable(DynValue MessagesTable)
    {
        var messages = new List<Message>();

        foreach (TablePair pair in MessagesTable.Table.Pairs)
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

    public static DynValue BuildDynValueFromMessagesAndResponse(List<Message> messages, MessageResponse response)
    {
        var script = new Script(); // FIXME
        var table = new Table(script);

        // Add original messages to the table
        for (int i = 0; i < messages.Count; i++)
        {
            var messageTable = new Table(script);
            messageTable["role"] = messages[i].Role == RoleType.User ? "user" : "assistant";
            messageTable["content"] = messages[i].Content;
            table[i + 1] = DynValue.NewTable(messageTable);
        }

        // Add the new response message to the table
        var responseTable = new Table(script);
        responseTable["role"] = "assistant";
        responseTable["content"] = response.Content;
        table[messages.Count + 1] = DynValue.NewTable(responseTable);

        // Add the raw response content as a separate field
        table["response"] = response.Content;

        return DynValue.NewTable(table);
    }
}

// [MoonSharpUserData]
public class NarratorApi
{
    NarratorMod _ParentMod;
    public NarratorObject Narrator;
    internal NarratorApi(NarratorMod parent, NarratorObject Narrator)
    {
        this.Narrator = Narrator;
        this._ParentMod = parent;
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

    // public async Task<DynValue> think(DynValue MessagesTable)
    // {
    //     var messages = NarratorMod.BuildMessagesListFromTable(MessagesTable);
    //     var response = await LLM.Anthropic.Ask(messages);
    //     return DynValue.NewString(response);
    // }

    public static TaskDescriptor think(DynValue MessagesTable)
    {
        var messages = NarratorMod.BuildMessagesListFromTable(MessagesTable);
        return TaskDescriptor.Build(async () =>
        {
            try
            {
                await LLM.Anthropic.Ask(messages);
                var response = await LLM.Anthropic.Ask(messages);
                return NarratorMod.BuildDynValueFromMessagesAndResponse(messages, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Anthropic API call: {ex.Message}");
                throw;
            }
        });
    }
}
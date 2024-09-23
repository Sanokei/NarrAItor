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
public class NarratorPrompts
{
    public const string PROMPT = "create a lua program using the following API to create ";
    public string prompt(params string[] args)
    {
        return $"using {args.Select(p=>p.ToString())}, {PROMPT}";
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
    /// Prints Phrase to Console
    /// </summary>
    /// <param name="Phrase">The phrase to be printed to the console</param>
    public void print(string Phrase)
    {
        Console.WriteLine(Phrase);
    }

    /// <summary>
    /// Use <> text to speech to speak the phrase given.
    /// </summary>
    /// <param name="Phrase">The phrase to be spoken</param>
    public void say(string Voice, string Phrase)
    {
        throw new NotImplementedException();
    }

    // public async Task<DynValue> think(DynValue MessagesTable)
    // {
    //     var messages = NarratorMod.BuildMessagesListFromTable(MessagesTable);
    //     var response = await LLM.Anthropic.Ask(messages);
    //     return DynValue.NewString(response);
    // }
    public TaskDescriptor think(DynValue Message)
    {
        switch (Message.Type)
        {
            case DataType.String:
                return think(Message.String);
            case DataType.Table:
                return think(Message.Table);
            default:
                throw new ArgumentException($"Unsupported input type for think: {Message.Type}");
        }
    }
    public TaskDescriptor think(string Message)
    {
        var table = new Table(_ParentMod.script);
        var messageTable = new Table(_ParentMod.script);
        messageTable["role"] = DynValue.NewString("user");
        messageTable["content"] = DynValue.NewString(Message);
        table[1] = DynValue.NewTable(messageTable);

        return think(DynValue.NewTable(table));
    }
    public TaskDescriptor think(Table MessagesTable)
    {
        
        var messages = NarratorMod.BuildMessagesListFromTable(MessagesTable);
        return TaskDescriptor.Build(async () =>
        {
            try
            {
                await LLM.Anthropic.Ask(messages);
                var response = await LLM.Anthropic.Ask(messages);
                return _ParentMod.BuildDynValueFromMessagesAndResponse(messages, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Anthropic API call: {ex.Message}");
                throw;
            }
        });
    }

    internal DynValue prompt(params DynValue[] args)
    {

        return DynValue.NewString("");
    }
    public DynValue prompt()
    {
        return prompt(new DynValue[0]);
    }
    public DynValue prompt(DynValue arg1)
    {
        return prompt(new DynValue[1]{arg1});
    }
    public DynValue prompt(DynValue arg1, DynValue arg2)
    {
        return prompt(new DynValue[2]{arg1,arg2});
    }
    public DynValue prompt(DynValue arg1, DynValue arg2, DynValue arg3)
    {
        return prompt(new DynValue[3]{arg1,arg2,arg3});
    }
    public DynValue prompt(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
    {
        return prompt(new DynValue[4]{arg1,arg2,arg3,arg4});
    }
    public DynValue prompt(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4, DynValue arg5)
    {
        return prompt(new DynValue[5]{arg1,arg2,arg3,arg4,arg5});
    }
    public DynValue prompt(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4, DynValue arg5, DynValue arg6)
    {
        return prompt(new DynValue[6]{arg1,arg2,arg3,arg4,arg5,arg6});
    }
}
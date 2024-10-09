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
/// <summary>
/// The base "NarratorMod" that allows for the NarratorMods to exist.
/// Basically just a wrapper mod for Anthropic.SDK
/// </summary>

public class NarratorApi
{
    NarratorMod _ParentMod;
    public NarratorBot Narrator;
    internal NarratorApi(NarratorMod parent, NarratorBot Narrator)
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
    internal TaskDescriptor think(string Message)
    {
        var table = new Table(_ParentMod.script);
        table[1] = DynValue.NewString(Message);

        return think(DynValue.NewTable(table));
    }
    internal TaskDescriptor think(Table MessagesTable)
    {
        
        var messages = NarratorMod.BuildMessagesListFromTable(MessagesTable);
        return TaskDescriptor.Build(async () =>
        {
            try
            {
                var response = await LLM.Anthropic.Ask(messages, _ParentMod.MaxTokens);
                return _ParentMod.BuildDynValueFromMessagesAndResponse(messages, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Anthropic API call: {ex.Message}");
                throw;
            }
        });
    }
    public DynValue prompt(DynValue argsTable)
    {
        if (argsTable.Type != DataType.Table)
        {
            throw new ScriptRuntimeException("Expected a table as input for prompt");
        }

        var variables = new Dictionary<string, string>();
        var userVarsTable = _ParentMod.script.Globals.Get("uservar").Table;

        foreach (TablePair pair in argsTable.Table.Pairs)
        {
            if (pair.Value.Type == DataType.Table)
            {
                var innerTable = pair.Value.Table;
                if (innerTable.Length >= 2)
                {
                    string key = innerTable[1].ToString();
                    string value = innerTable[2].ToString();
                    variables[key] = value;

                    // Add the variable to the uservars table
                    userVarsTable[key] = DynValue.NewString(value);
                }
            }
        }
        string result = NarratorPrompts.prompt(new Dictionary<string, object>
        {
            { "uservars", variables },
            { "maxtokens", _ParentMod.MaxTokens }
        });
        return DynValue.NewString(result);
    }

    public DynValue prompt()
    {
        return prompt(DynValue.NewTable(_ParentMod.script));
    }
}
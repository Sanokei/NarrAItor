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
    /// <summary>
    /// Store info on who is the parent mod, but keep as protected as possible
    /// </summary>
    internal NarratorMod _ParentMod;

    // public NarratorBot Narrator;
    // internal NarratorApi(NarratorMod parent, NarratorBot Narrator)
    // {
    //     this.Narrator = Narrator;
    //     this._ParentMod = parent;
    // }
    internal NarratorApi(NarratorMod parent)
    {
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
                var response = await LLM.Anthropic.Ask(messages, _ParentMod.ParentBot.MaxTokens); // slow way of getting maxtokens
                return _ParentMod.BuildDynValueFromMessagesAndResponse(messages, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Anthropic API call: {ex.Message}");
                throw;
            }
        });
    }
}
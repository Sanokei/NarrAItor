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
                return BuildDynValueFromMessagesAndResponse(messages, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Anthropic API call: {ex.Message}");
                throw;
            }
        });
    }
    internal DynValue BuildDynValueFromMessagesAndResponse(List<Message> messages, MessageResponse response)
    {
        var table = new Table(_ParentMod.script);
        var messagesTable = new Table(_ParentMod.script);
        // Add original messages to the table
        for (int i = 0; i < messages.Count; i++)
        {
            var msgTable = new Table(_ParentMod.script);
            msgTable["role"] = messages[i].Role == RoleType.User ? "user" : "assistant";
            msgTable["content"] = messages[i].ToString();
            messagesTable[i + 1] = DynValue.NewTable(msgTable);
        }
        
        // Add the new response message to the table
        var responseTable = new Table(_ParentMod.script);
        responseTable["role"] = "assistant";
        responseTable["content"] = response.Message.ToString();
        messagesTable[messages.Count + 1] = DynValue.NewTable(responseTable);
        
        table["messages"] = DynValue.NewTable(messagesTable);

        // Add the raw response content as a separate field
        table["content"] = response.Message.ToString();

        return DynValue.NewTable(table);
    }
}
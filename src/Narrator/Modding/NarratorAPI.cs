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
public static class NarratorApi
{
    // Wrapper
    public static TaskDescriptor think(this NarratorMod _ParentMod, DynValue input)
    {
        switch (input.Type)
        {
            case DataType.String:
                return think(_ParentMod, input.String);
            case DataType.Table:
                return think(_ParentMod, input.Table);
            default:
                throw new ArgumentException($"Unsupported input type for think: {input.Type}");
        }
    }
    public static TaskDescriptor think(DynValue input1, DynValue input2)
    {
        // FIXME
        return think(input1, input2);
    }

    //
    internal static TaskDescriptor think(this NarratorMod _ParentMod, string Message)
    {
        var table = new Table(_ParentMod.script);
        table["messages"] = DynValue.NewTable(new Table(_ParentMod.script) { [1] = DynValue.NewString(Message) });
        return think(_ParentMod, table);
    }
    internal static TaskDescriptor think(this NarratorMod _ParentMod, Table Messages)
    {
        return think(_ParentMod, Messages, null);
    }
    internal static TaskDescriptor think(this NarratorMod _ParentMod, Table Messages, Table configTable)
    {
        var messages = new List<Message>();
        var args = new Dictionary<string, object>();

        if (configTable.Get("messages") != DynValue.Nil)
        {
            messages = NarratorMod.BuildMessagesListFromTable(configTable.Get("messages").Table);
        }

        // Parse configuration options
        if (configTable.Get("max_tokens") != DynValue.Nil)
            args["MaxTokens"] = (int)configTable.Get("max_tokens").Number;

        if (configTable.Get("stream") != DynValue.Nil)
            args["Stream"] = configTable.Get("stream").Boolean;

        if (configTable.Get("temperature") != DynValue.Nil)
            args["Temperature"] = (decimal)configTable.Get("temperature").Number;

        if (configTable.Get("system") != DynValue.Nil)
        {
            var systemMessages = new List<SystemMessage>();
            if(configTable.Get("system").Type == DataType.Table)
                foreach (var pair in configTable.Get("system").Table.Pairs)
                {
                    systemMessages.Add(new SystemMessage(pair.Value.String));
                }
            else if(configTable.Get("system").Type == DataType.String)
                systemMessages.Add(new SystemMessage(configTable.Get("system").String ));
            args["System"] = systemMessages;
        }

        return TaskDescriptor.Build(async () =>
        {
            try
            {
                var response = await LLM.Ask(messages, args);
                return _ParentMod.BuildDynValueFromMessagesAndResponse(messages, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Anthropic API call: {ex.Message}");
                throw;
            }
        });
    }

    internal static DynValue BuildDynValueFromMessagesAndResponse(this NarratorMod _ParentMod, List<Message> messages, MessageResponse response)
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
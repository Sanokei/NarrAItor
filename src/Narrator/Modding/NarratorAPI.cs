using System;
using System.Threading;

using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
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
    // Existing methods remain the same...

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

        // New: Tool Support
        if (configTable.Get("tools") != DynValue.Nil)
        {
            var toolsTable = configTable.Get("tools").Table;
            var tools = new List<Anthropic.SDK.Common.Tool>();

            foreach (var pair in toolsTable.Pairs)
            {
                if (pair.Value.Type == DataType.Function)
                {
                    // Create a tool from a Lua function
                    var tool = CreateToolFromLuaFunction(_ParentMod, pair.Value, pair.Key.String);
                    tools.Add(tool);
                }
            }

            args["Tools"] = tools;
        }

        // New: Tool Choice Support
        if (configTable.Get("tool_choice") != DynValue.Nil)
        {
            var toolChoiceTable = configTable.Get("tool_choice").Table;
            var toolChoice = new ToolChoice
            {
                Type = toolChoiceTable.Get("type").String == "tool" 
                    ? ToolChoiceType.Tool 
                    : ToolChoiceType.Auto,
                Name = toolChoiceTable.Get("name")?.String
            };
            args["ToolChoice"] = toolChoice;
        }

        return TaskDescriptor.Build(async () =>
        {
            try
            {
                var response = await LLM.Ask(messages, args);
                return NarratorMod.BuildDynValueFromMessagesAndResponse(messages, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Anthropic API call: {ex.Message}");
                throw;
            }
        });
    }

    // Helper method to create a tool from a Lua function
    private static Anthropic.SDK.Common.Tool CreateToolFromLuaFunction(NarratorMod mod, DynValue luaFunction, string name)
    {
        // Create a C# function that can be called from the tool
        Func<string, string> wrappedFunction = (input) =>
        {
            // Call the Lua function
            var result = mod.script.Call(luaFunction, DynValue.NewString(input));
            return result.String;
        };

        // Create a tool from the wrapped function
        return LLM.CreateToolFromFunc(
            name, 
            $"Tool created from Lua function: {name}", 
            wrappedFunction
        );
    }
}
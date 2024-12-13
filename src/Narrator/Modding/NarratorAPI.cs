using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

using NarrAItor.Narrator;

namespace NarrAItor.Narrator.Modding;

/// <summary>
/// Provides an API for interacting with the Anthropic LLM from Lua scripts within Narrator mods.
/// </summary>
public static class NarratorApi
{
    /// <summary>
    /// Sends a request to the Anthropic LLM with a list of messages and configuration options.
    /// </summary>
    internal static TaskDescriptor think(this NarratorMod _ParentMod, Table messagesTable, Table configTable)
    {
        var messages = NarratorMod.BuildMessagesListFromTable(messagesTable); // Use helper method
        var args = new Dictionary<string, object>();

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
            var systemValue = configTable.Get("system");
            if (systemValue.Type == DataType.Table)
            {
                foreach (var pair in systemValue.Table.Pairs)
                {
                    systemMessages.Add(new SystemMessage(pair.Value.String));
                }
            }
            else if (systemValue.Type == DataType.String)
            {
                systemMessages.Add(new SystemMessage(systemValue.String));
            }
            args["System"] = systemMessages;
        }

        // Tool Support
        if (configTable.Get("tools") != DynValue.Nil)
        {
            var toolsTable = configTable.Get("tools").Table;
            var tools = new List<Anthropic.SDK.Common.Tool>();

            foreach (var pair in toolsTable.Pairs)
            {
                if (pair.Value.Type == DataType.Function)
                {
                    var tool = CreateToolFromLuaFunction(_ParentMod, pair.Value, pair.Key.String);
                    tools.Add(tool);
                }
            }

            args["Tools"] = tools;
        }

        // Tool Choice Support
        if (configTable.Get("tool_choice") != DynValue.Nil)
        {
            var toolChoiceTable = configTable.Get("tool_choice").Table;
            var toolChoice = new ToolChoice
            {
                Type = toolChoiceTable.Get("type").String == "tool" ? ToolChoiceType.Tool : ToolChoiceType.Auto,
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
                throw; // Re-throw the exception after logging
            }
        });
    }

    public static string GET_API_DOCUMENTATION => File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "Documentation.md")).ToString();

    /// <summary>
    /// Generates a prompt string based on user variables and API documentation.
    /// </summary>
    internal static TaskDescriptor prompt(this NarratorMod _ParentMod, Table argsTable)
    {
        return TaskDescriptor.Build(async () =>
        {
        if (argsTable == null)
        {
            return DynValue.NewString("Error: Arguments table cannot be null.");
        }

        DynValue userargsDyn = argsTable.Get("userargs");
        DynValue maxtokensDyn = argsTable.Get("maxtokens");
        DynValue temperatureDyn = argsTable.Get("temperature");
        DynValue systemDyn = argsTable.Get("system");
        DynValue toolsDyn = argsTable.Get("tools");
        DynValue toolChoiceDyn = argsTable.Get("tool_choice");

        if (userargsDyn.Type != DataType.Table || maxtokensDyn.Type != DataType.Number)
        {
            return DynValue.NewString("Error: Invalid arguments. Expected a table for 'userargs' and a number for 'maxtokens'.");
        }

        Table userargsTable = userargsDyn.Table;
        Dictionary<string, string> userargs = new Dictionary<string, string>();
        foreach (var pair in userargsTable.Pairs)
        {
            if (pair.Value.Type == DataType.String)
            {
            userargs.Add(pair.Key.String, pair.Value.String);
            }
        }

        int MaxTokens = (int)maxtokensDyn.Number;
        decimal temperature = temperatureDyn.Type == DataType.Number ? (decimal)temperatureDyn.Number : 0.7m; // Default temperature
        List<SystemMessage> systemMessages = new();
        if (systemDyn.Type == DataType.Table)
        {
            foreach (var pair in systemDyn.Table.Pairs)
            {
            systemMessages.Add(new SystemMessage(pair.Value.String));
            }
        }
        else if (systemDyn.Type == DataType.String)
        {
            systemMessages.Add(new SystemMessage(systemDyn.String));
        }

        List<Anthropic.SDK.Common.Tool> tools = new();
        if (toolsDyn != DynValue.Nil && toolsDyn.Type == DataType.Table)
        {
            var toolsTable = toolsDyn.Table;
            foreach (var pair in toolsTable.Pairs)
            {
            if (pair.Value.Type == DataType.Function)
            {
                var tool = CreateToolFromLuaFunction(_ParentMod, pair.Value, pair.Key.String);
                tools.Add(tool);
            }
            }
        }

        ToolChoice? toolChoice = null;
        if (toolChoiceDyn != DynValue.Nil && toolChoiceDyn.Type == DataType.Table)
        {
            var toolChoiceTable = toolChoiceDyn.Table;
            toolChoice = new ToolChoice
            {
            Type = toolChoiceTable.Get("type").String == "tool" ? ToolChoiceType.Tool : ToolChoiceType.Auto,
            Name = toolChoiceTable.Get("name")?.String
            };
        }

        // Construct the prompt string using user-provided data and API documentation
        string prompt = $@"
            Using the following user variables: {string.Join(", ", userargs.Select(kv => $"{kv.Key}: {kv.Value}"))}
            The response must be within {MaxTokens} tokens.
            Do NOT make up API endpoints. Only use the available API below.
            {GET_API_DOCUMENTATION}
        ";

        // Create a message list for the LLM call
        var messages = new List<Message> { new Message(RoleType.User, prompt) };

        // Prepare arguments for the LLM call
        var args = new Dictionary<string, object>()
        {
            { "MaxTokens", MaxTokens },
            { "Temperature", temperature },
            { "System", systemMessages },
            { "Tools", tools },
            { "ToolChoice", toolChoice }
        };

        try
        {
            // Use LLM.Ask to generate the final prompt
            var response = await LLM.Ask(messages, args);
            return NarratorMod.BuildDynValueFromMessagesAndResponse(messages, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Anthropic API call: {ex.Message}");
            return DynValue.NewString($"Error in Anthropic API call: {ex.Message}");
        }
        });
    }


    // Helper method to create a tool from a Lua function
    private static Anthropic.SDK.Common.Tool CreateToolFromLuaFunction(NarratorMod mod, DynValue luaFunction, string name)
    {
        Func<string, string> wrappedFunction = (input) =>
        {
            try
            {
                var result = mod.script.Call(luaFunction, DynValue.NewString(input));
                return result.String;
            }
            catch (ScriptRuntimeException ex)
            {
                Console.WriteLine($"Lua Error in tool '{name}': {ex.DecoratedMessage}");
                return $"Error in tool '{name}': {ex.DecoratedMessage}"; // Return error to LLM
            }
            catch (Exception ex)
            {
                Console.WriteLine($"C# Error wrapping Lua tool '{name}': {ex.Message}");
                return $"Error in tool '{name}': {ex.Message}"; // Return error to LLM
            }
        };

        return LLM.CreateToolFromFunc(name, $"Tool created from Lua function: {name}", wrappedFunction);
    }
}
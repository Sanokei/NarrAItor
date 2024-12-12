using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using MoonSharp.Interpreter;

using NarrAItor.Narrator;
using NarrAItor.Narrator.Modding.Base;
using NarrAItor.Narrator.Modding;

public class Liam : NarratorMod, INarratorMod
{
    // The main prompt method that takes a table of arguments
    public async Task<DynValue> prompt(DynValue argsTable)
    {
        // Validate that argsTable is a Lua table
        if (argsTable.Type != DataType.Table)
        {
            throw new ScriptRuntimeException("Expected a table as input for prompt");
        }

        // Prepare the user-specific variables (uservars)
        var variables = new Dictionary<string, string>();
        var userVarsTable = script.Globals.Get("uservar").Table;

        // Extract user arguments from the Lua table and populate variables
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

                    // Add the variable to the uservars table for persistent state
                    userVarsTable[key] = DynValue.NewString(value);
                }
            }
        }

        // Prepare the request for the AI
        var promptResponse = await NarratorPrompts.prompt(new Dictionary<string, object>
        {
            { "userargs", variables },
            { "maxtokens", ParentBot.MaxTotalTokens }
        });

        // Return the result from the AI
        return DynValue.NewString(promptResponse);
    }

    // A default prompt method if no args are provided
    public async Task<DynValue> prompt()
    {
        return await prompt(DynValue.NewTable(ParentBot.script));
    }
}

public static class NarratorPrompts
{
    // Placeholder for API documentation (could be loaded dynamically)
    public static string GET_API_DOCUMENTATION => File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "Documentation.md")).ToString();
    public static string EXAMPLES = "";
    public static string PROMPT = $"Create a Lua program to generate narration in the style specified. Use the voice specified. Only return Lua code. Do not use ``` to make it a code block.";

    /// <summary>
    /// Generates the prompt to send to the AI.
    /// </summary>
    public static async Task<string> prompt(Dictionary<string, object> args)
    {
        // Extract user arguments and token count from the provided args
        if (args["userargs"] is not Dictionary<string, string> userargs || args["maxtokens"] is not int MaxTokens)
        {
            return "<rationality> when prompting, make sure to have the correct arguments passed.</rationality>";
        }

        // Example: "uservars" can be a dictionary of key-value pairs
        userargs = (Dictionary<string, string>)args["userargs"];
        MaxTokens = (int)args["maxtokens"];

        // Format the prompt
        return $@"
            Using the following user variables: {string.Join(", ", userargs.Select(kv => $"{kv.Key}: {kv.Value}"))}
            {PROMPT}
            The response must be within {MaxTokens} tokens.
            Do NOT make up API endpoints. Only use the available API below.
            {GET_API_DOCUMENTATION}";
    }
}

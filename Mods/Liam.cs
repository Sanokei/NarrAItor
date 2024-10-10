using System;
using System.Threading;

using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.CoreLib.IO;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Interop;

using NarrAItor.Narrator;
using NarrAItor.Narrator.Modding.Base;
using NarrAItor.Narrator.Modding;

public class Liam : NarratorMod, INarratorMod
{
    public DynValue prompt(DynValue argsTable)
    {
        if (argsTable.Type != DataType.Table)
        {
            throw new ScriptRuntimeException("Expected a table as input for prompt");
        }

        var variables = new Dictionary<string, string>();
        var userVarsTable = script.Globals.Get("uservar").Table;

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
            { "maxtokens", ParentBot.MaxTokens }
        });
        return DynValue.NewString(result);
    }

    public DynValue prompt()
    {
        return prompt(DynValue.NewTable(ParentBot.script));
    }
}

public static class NarratorPrompts
{
    public static string GET_API_DOCUMENTATION => File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(),"Documentation.md")).ToString();
    public static string EXAMPLES = "";
    public static string PROMPT = $"Create a lua program to create a narration in the style specified. That uses the voice specified. Only return the lua code. Do not use ``` to make it a code block. As if you returned anything else, it will break.";

    /// Replacing: Dictionary<object,object> with object[]
    public static string prompt(Dictionary<string,object> args) // let the ai use any object as a "name" for another object, edit them to store vectors.
    {
        if (args["userargs"] is not Dictionary<string, string> userargs && args["maxtokens"] is not int MaxTokens) // sneaky iniitilization
            return("<rationality> when prompting, make sure to have the </rationality>"); // read NarrAItor/Prompt Engineering.md#idea-6 for context
        userargs = (Dictionary<string, string>) args["userargs"];
        MaxTokens = (int)args["maxtokens"];
        
        return $"Using the following variables within uservars: {String.Join(",",args.Keys.Select(p=>p.ToString()))},\n{PROMPT}\nThe response must be within {MaxTokens.ToString()} number of Tokens.\nDo NOT make up API endpoints. Only use the avaiable API below\n{GET_API_DOCUMENTATION}";
    }
    //  int MaxTokens
}
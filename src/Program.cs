using System.Collections.Generic;
using System.Collections;
using NarrAItor.Narrator;
using NarrAItor.Utils;
using NarrAItor.Utils.Datatypes;
using NarrAItor.Configuration;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using NarrAItor.Narrator.Modding;

using Anthropic.SDK.Messaging;
using Anthropic.SDK.Common;
using Anthropic.SDK.Extensions;
using Anthropic.SDK;
using Anthropic.SDK.Constants;

namespace NarrAItor;

public class Program
{
    /*
    KeyValuePair being a Struct caused a NullReferenceException.
    
    "The problem with structs is that their constructor is not guaranteed to run.
    E.g., when you create an array var array = new directories[10];.
    Therefore structs cannot have initializers and explicit parameterless constructors."
    source: https://stackoverflow.com/questions/54607279/list-object-of-struct-is-never-assigned-to-and-will-always-have-its-default-valu#:~:text=The%20problem%20with%20structs%20is%20that%20their%20constructor%20is%20not%20guaranteed%20to%20run.%20E.g.%2C%20when%20you%20create%20an%20array%20var%20array%20%3D%20new%20directories%5B10%5D%3B.%20Therefore%20structs%20cannot%20have%20initializers%20and%20explicit%20parameterless%20constructors.
    */
    
    class KeyValuePair(string key = "")
    {
        public string Key = key;
        public List<string> Value = [];
    }
    
    static bool GetKeywordArguments(out Dictionary<string,List<string>> kwargs, string[] args)
    {
        KeyValuePair kvpair = new();
        kwargs = [];
        var argstoLower = args.Select(arg => arg.ToLower()).ToArray();
        foreach(var arg in argstoLower)
        {
            if(arg.Length >= 2 && arg[..2].Equals("--"))
            {
                if(kvpair.Key != null)
                {
                    kwargs.Add(kvpair.Key, kvpair.Value);
                    kvpair = new();
                }
                kvpair.Key = arg[2..];
                continue;
            }
            if(kvpair.Key == null)
                throw new Exception($"Argument {arg} has no keyword flag (e.g --help)");
            kvpair.Value.Add(arg);
        }
        if(kvpair.Key != null)
        {
            kwargs.Add(kvpair.Key, kvpair.Value);
            kvpair = new();
        }
        return kwargs.Count != 0;
    }
    static async Task Main(string[] args)
    {
        SetEnvironmentFromSecret();

        if (GetKeywordArguments(out Dictionary<string, List<string>> kwargs, args))
        {
            foreach (var arg in kwargs)
            {
                if (Commands.CommandList.TryGetCommand(arg.Key, out var action))
                {
                    if (action is CommandMap.AsyncCommandAction asyncAction)
                    {
                        await asyncAction?.Invoke(arg.Value.ToArray());
                    }
                    else if (action is Action<string[]> syncAction)
                    {
                        syncAction?.Invoke(arg.Value.ToArray());
                    }
                }
            }

            // Check mods folder for installed mods
            
        }
    }

    private static void SetEnvironmentFromConfig(string path)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile(path, false, true);
        IConfigurationRoot root = builder.Build();
        throw new NotImplementedException();
    }

    private static void SetEnvironmentFromSecret()
    {
        List<string> SecretConfigNames = Config.SecretConfiguration();
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        foreach(var secret in SecretConfigNames)
        {
            if(string.IsNullOrEmpty(config[secret]))
                Console.WriteLine($"Warning: No {secret} in secrets.json\nFalling back on CLI flag");
            else
                Environment.SetEnvironmentVariable(secret,config[secret]);
        }
    }

}

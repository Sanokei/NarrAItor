using System.Collections.Generic;
using System.Collections;
using NarrAItor.Narrator;

namespace NarrAItor
{
    public class Program
    {
        struct KeyValuePair(string Key, List<string> Value)
        {
            public string Key = Key;
            public List<string> Value = Value;
        };
        static void Main(string[] args)
        {
            // break the arg list into flags and parameters
            Dictionary<string,List<string>> kwargs = [];
            KeyValuePair kvpair = new();

            Array.ForEach(args, (arg) => {
                if(arg[..2].Equals("--"))
                {
                    if(kvpair.Key != null)
                    {
                        kwargs.Add(kvpair.Key, kvpair.Value ?? []);
                        kvpair = new();
                    }
                    kvpair.Key = arg[2..^(-1)];
                }
                else
                    if(kvpair.Key == null)
                        throw new Exception($"Argument {arg} has no keyword flag (e.g --help)");
                    else
                        kvpair.Value?.Add(arg);
            });

            

            // Environment.SetEnvironmentVariable("api_token", );
            // Create Lua bindings
            // Request the LLM Provider
            ChirpingNarrator Jeff = new ChirpingNarrator();
        }
    }
}

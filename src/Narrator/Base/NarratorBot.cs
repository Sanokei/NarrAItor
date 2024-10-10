using System.IO;
using Newtonsoft.Json;
using NarrAItor.Narrator.NarratorExceptions;
using NarrAItor.Narrator.Modding;
using NarrAItor.Narrator.Modding.Base;
using MoonSharp.Interpreter;

namespace NarrAItor.Narrator;
public class NarratorBot : INarratorBot
{
    // Narrator Bot Defaults
    const string DEFAULT_DOCUMENTATION_PATH = "./Narrators/DefaultNarrator/docs/";
    const string DEFAULT_DOCUMENTATION = "";

    // Interface :sob:
    Script _script;
    public Script script{get => _script; set => _script = value;}
    string _Name = "DefaultNarrator";
    public string Name { get => _Name; set => _Name = value;}

    string _Version = "0.0.0";
    public string Version { get => _Version; set => _Version = value; }

    string _Objective = "You are a Narrator";
    public string Objective { get => _Objective; set => _Objective = value; }

    string _Personality = "You act like the Narrator from every movie intro";
    public string Personality { get => _Personality; set => _Personality = value; }

    string _DocumentationPath = DEFAULT_DOCUMENTATION_PATH;
    public string DocumentationPath { get => _DocumentationPath; set => _DocumentationPath = value; }

    Dictionary<string, NarratorMod> _ModsDirectory = [];
    public Dictionary<string, NarratorMod> ModsDirectory { get;set; }

    Dictionary<string, NarratorMod> _InstalledMods = [];
    public Dictionary<string, NarratorMod> InstalledMods { get => _InstalledMods; set => _InstalledMods = value; }

    int _MaxTokens = 500;
    public int MaxTokens { get => _MaxTokens; set => _MaxTokens = value; } // cant have anything lower than 1: value < 1 ? 5000 : value; fix?

    // Gets the Documentation from the path
    public string Documentation = "";
    public string GetDocumentation(string path = DEFAULT_DOCUMENTATION_PATH, bool useCachedDocumentation = true)
    {
        string DirPath = Path.Combine(Directory.GetCurrentDirectory(), path.Equals(DEFAULT_DOCUMENTATION_PATH) ? path : DocumentationPath);
        if(!Directory.Exists(DirPath))
        {
            Console.WriteLine(Warnings.DOCUMENTATION_PATH_DOES_NOT_EXIST);
            return DEFAULT_DOCUMENTATION;
        }    

        if(!string.IsNullOrEmpty(Documentation) && useCachedDocumentation)
            return Documentation;

        Dictionary<string, string> DocFiles = [];
        foreach (string fileName in Directory.GetFiles(DirPath, "*.md"))
            DocFiles.Add(fileName,File.ReadAllText(fileName));
        Documentation = JsonConvert.SerializeObject(DocFiles);
        return Documentation;
    }
}
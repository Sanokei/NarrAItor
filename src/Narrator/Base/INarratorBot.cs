using MoonSharp.Interpreter;

namespace NarrAItor.Narrator.Modding.Base;
public interface INarratorBot
{
    public string Name{get;set;}
    public string Version{get;set;}
    /// <summary>
    /// The overall objective of the narrator, not current state.
    /// </summary>
    public string Objective{get;set;}
    /// <summary>
    /// Personality of the Narrator
    /// </summary>
    public string Personality{get;set;}
    /// <summary>
    ///  The relative path of the narrator's documentation.
    /// </summary>
    public string DocumentationPath{get;set;}
    public Script script{get;set;}
    public int MaxTokens {get;set;}
    public Dictionary<string, NarratorMod> ModsDirectory{get;set;}
    public Dictionary<string, NarratorMod> InstalledMods{get;set;}

}
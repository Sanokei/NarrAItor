using System.Dynamic;
using MoonSharp.Interpreter;

namespace NarrAItor.Narrator.Modding.Base;
public interface INarratorBot
{
    // Methods
    public string Name{get;set;}
    public string Version{get;set;}
    /*
        Objectives
        The different Objective states. The order of objective state is determined by bot.
    */
    /// <summary>
    /// The overall objective of the narrator, not current state.
    /// </summary>
    public string Objective{get;set;}

    /// <summary>
    /// Current state.
    /// </summary>
    public string CurrentObjective{get;set;}

    /// <summary>
    /// User Objective
    /// </summary>
    public string UserObjective{get;set;}


    /// <summary>
    /// Personality of the Narrator
    /// </summary>
    public string Personality{get;set;}

    public Script script{get;set;}
    
    // Token Lengths

    public Dictionary<string, NarratorMod> ModsDirectory{get;set;}
    public Dictionary<string, NarratorMod> RequiredMods{get;set;}

    public int MaxTotalTokens { get; set; } // Default max total tokens

    // Delegates
    public delegate void NarratorModDelegate(INarratorBot sender);
    // Let's other mods know that a mod has been added or removed from a bot
    public static NarratorModDelegate OnAddedEvent;
    public static NarratorModDelegate OnRemovedEvent;

    public void Initialize();
    public Task Run();

}
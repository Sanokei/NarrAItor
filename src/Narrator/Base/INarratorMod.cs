namespace NarrAItor.Narrator.Modding.Base;
public interface INarratorMod
{   
    public string LuaFileData {get; set;}
    
    public void OnEnable(INarratorBot ParentBot);

    public void OnDisable(INarratorBot ParentBot);
    
    public delegate void NarratorModDelegate(INarratorBot sender);
    // Let's other mods know that a new mod has been added to the bot
    public static NarratorModDelegate OnAddedEvent;
    public static NarratorModDelegate OnRemovedEvent;

    // I hate that it has come to this.
    // Constructors are build implementation, not behaviour, so I can't enforce it's signature. 
    // Which it good, but it means I have to create a build order.

    // So the NarratorMod constructor can try to take in whatever `data` it wants from the NarratorBot  
    // However we still need a way to actually
    public void Initialize();
    public Task Run();
    public INarratorBot ParentBot{get;set;}
}
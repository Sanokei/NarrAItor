namespace NarrAItor.Narrator.Modding.Base;
using MoonSharp.Interpreter;
public interface INarratorMod
{
    public string LuaFileData {get; set;}
    public Script script { get; set; }
    public Dictionary<string, NarratorMod> RequiredMods { get; set; } 

    // I hate that it has come to this.
    // Constructors are build implementation, not behaviour, so I can't enforce it's signature. 
    // Which it good, but it means I have to create a build order.

    // So the NarratorMod constructor can try to take in whatever `data` it wants from the NarratorBot  
    // However we still need a way to actually
    public void Initialize();
    public Task Run();
}
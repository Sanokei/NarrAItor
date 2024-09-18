using NarrAItor.Utils.Datatypes;
namespace NarrAItor.Configuration;

public static class Config
{
    public struct Names
    {
        public struct Secrets
        {
            public const string ANTHROPIC_API_KEY = "Anthropic__BearerToken";
        }
    }

    static List<string> GetLeafNodesAsStringConfiguration(ConfigurationSections sections) {return sections.GetLeafNodes().Select(x => x.Configuration).ToList();}
    public static List<string> SecretConfiguration()
    {
        // Create root
        var SecretConfig = ConfigurationSections.Create();

        // Create branch
        var Anthropic = SecretConfig.Create("Anthropic");
        // Create leaf
        var Secret_Key = Anthropic.Create("BearerToken");

        // gets all the leaf nodes.
        return GetLeafNodesAsStringConfiguration(SecretConfig);
    }

    public static List<string> AppConfiguration()
    {
        var AppConfig = ConfigurationSections.Create();
        var NarratorName = AppConfig.Create("NarratorName");
        
        return GetLeafNodesAsStringConfiguration(AppConfig);
    }

    // public static List<string> NarratorConfiguration()
    // {
    //     var NarratorConfig = ConfigurationSections.Create();

    //     var Narrator = NarratorConfig.Create(null);
    //     var Name = Narrator.Create("Name");
    //     var Version = Narrator.Create("Version");
    //     var Objective = Narrator.Create("Objective");
    //     var Personality = Narrator.Create("Personality");
    //     var Documentation = Narrator.Create("Documentation");

    //     return GetLeafNodesAsStringConfiguration(Narrator);
    // }
}
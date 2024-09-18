using NarrAItor.Utils.Datatypes;
using NarrAItor.Configuration;

namespace NarrAItor;

public static class Commands
{
    public static CommandMap CommandList { get; } = new();
    public static List<CommandMap.CommandAction> CommandActions{ get; } = [Help, Version, BearerToken];

    static Commands()
    {
        CommandList.AddCommand(Help, "help", "h", "?");
        CommandList.AddCommand(Version, "version", "v");
        CommandList.AddCommand(BearerToken, "bearertoken");
        CommandList.AddCommand(LoadNarrator, "loadnarrator", "loadmod", "load");
        CommandList.AddCommand(UnitTest, "unittest", "test", "t");
    }

    public static void Help(string[] args)
    {
        Console.WriteLine("Available commands:\n");
        foreach (var command in CommandActions)
        {
            Console.WriteLine(command.ToString());
            foreach (var alias in CommandList.GetAliases(command))
            {
                Console.WriteLine($"{alias}\t");
            }
            Console.WriteLine("\n");
        }
    }

    public static void Version(string[] args)
    {
        Console.WriteLine("Version 1.0.0");
    }

    public static void BearerToken(string[] args)
    {
        Environment.SetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY, args[0]);
    }

    public static void LoadNarrator(string[] args)
    {
        // Use config instead.
        // Environment.SetEnvironmentVariable("NarratorType", args[0]);
    }
/// <summary>
/// Not so UnitTesting.
/// </summary>
/// <param name="args"></param>
    public static void UnitTest(string[] args)
    {
        Console.WriteLine("Conducting Tests\n");
        Dictionary<string, Action> UnitTests = [];
        UnitTests.Add("luabinding", () => {
            Console.WriteLine("\nLua binding with Narrator `narrator.print(\"Test Working\")`");
            // Create Lua bindings
            NarrAItor.Narrator.Modding.NarratorMod test = new("","narrator.print(\"Test Working\")");
            // Request the LLM Provider
            test.Initialize();
            test.Run();
            Console.WriteLine("\nexpected result: Test Working\n");
        });

        UnitTests.Add("configsection", () => {
            Console.WriteLine("\nConfigurationSections. GetLeafNodesAsStringConfiguration");
            var Root = ConfigurationSections.Create();

            // Create branch
            var Branch = Root.Create("Branch");
            // Create leaf
            var leaf_1 = Branch.Create("Leaf_1");
            var leaf_2 = Branch.Create("Leaf_2");

            // gets all the leaf nodes.
            var leafNodes = Config.GetLeafNodesAsStringConfiguration(Root);
            Console.WriteLine($"{leafNodes[0]}, {leafNodes[1]}");
            Console.WriteLine("\nexpected result: Branch__Leaf_1, Branch__Leaf_2\n");
        });

        foreach(var arg in args)
            if(UnitTests.TryGetValue(arg, out var test))
                test?.Invoke();

        if(args.Count() == 0)
            foreach(var test in UnitTests.Values)
                test?.Invoke();
    }
}
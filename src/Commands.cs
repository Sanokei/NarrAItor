using NarrAItor.Utils.Datatypes;
using NarrAItor.Configuration;

namespace NarrAItor;

public static class Commands
{
    public static CommandMap CommandList { get; } = new();
    public static List<Delegate> CommandActions{ get; } = new();

    static Commands()
    {
        CommandList.AddCommand(new Action<string[]>(Help), "help", "h", "?");
        CommandList.AddCommand(new Action<string[]>(Version), "version", "v");
        CommandList.AddCommand(new Action<string[]>(BearerToken), "bearertoken");
        CommandList.AddCommand(new Action<string[]>(LoadNarrator), "loadnarrator", "loadmod", "load");
        CommandList.AddCommand(new CommandMap.AsyncCommandAction(UnitTest), "unittest", "test", "t");

        CommandActions.AddRange(new Delegate[] { Help, Version, BearerToken, LoadNarrator, UnitTest });
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
    public static async Task UnitTest(string[] args)
    {
        Console.WriteLine("Conducting Tests\n");
        Dictionary<string, Action> UnitTests = [];
        UnitTests.Add("luabinding", async () => {
            Console.WriteLine("\nLua binding with Narrator `narrator.print(\"Test Working\")`");
            // Create Lua bindings
            NarrAItor.Narrator.Modding.NarratorMod test = new("","narrator.print(\"Test Working\")");
            // Request the LLM Provider
            test.Initialize();
            await test.Run();
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

        UnitTests.Add("askanthropic", async () =>
        {
            string testing = @"
            local response = narrator:think({
                ""What's the weather like today?"",
                AsAssistantMessage(""Sure! Could you please provide me with your location?""),
                ""Dubai, UAE""
            })
            print(""Response from Anthropic: "" .. response.Message)
            ";
            try
            {
                NarrAItor.Narrator.Modding.NarratorMod test = new("", testing);
                test.Initialize();
                await test.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in askanthropic test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        });

        UnitTests.Add("luaasync", async () =>
        {
            MoonSharp.Interpreter.UserData.RegisterType<Demo>();
            string testing = @"
            print(""Hello World! This is Lua code script."");

            print(""Before delay..."")
            demo.delay(3);
            print(""After delay"")

            local content = demo.read(""TestScript.lua"");

            print(content);

            return 0;
            ";
            try
            {
                NarrAItor.Narrator.Modding.NarratorMod test = new("", testing);
                var script = test.Initialize();
                script.Globals["demo"] = typeof(Demo);
                await test.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in askanthropic test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        });

        UnitTests.Add("secret", () => {
            Console.WriteLine("\nEnviormental Variable Secret. `Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY);`");
            Console.WriteLine($"\nx-api-key: { (String.IsNullOrEmpty(Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY)) ? "Not Set" : Environment.GetEnvironmentVariable(Config.Names.Secrets.ANTHROPIC_API_KEY))}\n");
        });

        foreach(var arg in args)
            if(UnitTests.TryGetValue(arg, out var test))
                test?.Invoke();

        if(args.Count() == 0)
            foreach(var test in UnitTests.Values)
                test?.Invoke();
    }
}
public class Demo
{
    public static TaskDescriptor Delay(int seconds) {
        // wrapper the Task by TaskDescriptor
        return TaskDescriptor.Build(async () => await Task.Delay(seconds * 1000));
    }

    public static TaskDescriptor Read(string path)
    {
        // wrapper the Task<T> by TaskDescriptor
        return TaskDescriptor.Build(async () => await File.ReadAllTextAsync(path));
    }
}
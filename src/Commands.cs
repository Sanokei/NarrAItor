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
        CommandList.AddCommand(new CommandMap.AsyncCommandAction(UnitTest), "unittest", "test", "t");

        CommandActions.AddRange(new Delegate[] { Help, Version, BearerToken, UnitTest });
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

    /// <summary>
    /// Not so UnitTesting.
    /// </summary>
    /// <param name="args"></param>
    public static async Task UnitTest(string[] args)
    {
        Console.WriteLine("Conducting Tests\n");
        Dictionary<string, Delegate> UnitTests = [];
        UnitTests.Add("luabinding", () => {
            Console.WriteLine("\nLua binding with Narrator `narrator.print(\"Test Working\")`");
            // Create Lua bindings
            // NarrAItor.Narrator.Modding.NarratorMod test = new("","narrator.print(\"Test Working\")");
            // // Request the LLM Provider
            // test.Initialize();
            // test.Run();
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
            print(""Response from Anthropic: "" .. response.content)
            ";
            try
            {
                // NarrAItor.Narrator.Modding.NarratorMod test = new("", testing);
                // test.Initialize();
                // await test.Run();
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
            demo.Delay(3);
            print(""After delay"")

            -- local content = demo.read(""TestScript.lua"");

            -- print(content);

            return 0;
            ";
            try
            {
                // NarrAItor.Narrator.NarratorBot bot = new();
                // NarrAItor.Narrator.Modding.NarratorMod test = new("", testing);
                // var script = test.Initialize();
                // script.Globals["demo"] = typeof(Demo);
                // await test.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in lua test: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        });

        UnitTests.Add("promptanthropic", async () =>
        {
            // string testing = @"
            // local response = narrator:think(narrator:prompt(""in the stlye of the skylord of the sea, drawf of stone."",""voice like Gordon from HL1""))
            // print(""Response from Anthropic: "" .. response.content)
            // ";
            try
            {
                // Create the Bot
                NarrAItor.Narrator.NarratorBot NarratorBot = new(
                    "DefaultNarrator",
                    "0.0.0",
                    0,
                    "Become the best narrator you can be.",
                    "",
                    "You create modules for Narrator Bots.",
                    "You act like a Narrator.",
                    null,
                    null,
                    null
                );
                // NarrAItor.Narrator.Modding.NarratorMod test = new("", testing);
                
                await NarratorBot.Run();
                // await test.Run();
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

        UnitTests.Add("narrator", async () => {
            Console.WriteLine("\nCreating Stanley Parable-inspired Narrator Bot\n");

            // Lua script for the narrator
            string narratorScript = @"
            function Awake()
                print('The narrator system is initializing...')
            end

            function Start()
                print('The story begins...')
            end

            function Update()
                -- Periodic narrative comments
                if math.random() < 0.1 then
                    local comments = {
                        'Stanley wondered why he was here.',
                        'The room was quiet, too quiet.',
                        'Stanley knew something was different today.',
                        'But Stanley was not so sure...'
                    }
                    print(comments[math.random(#comments)])
                end
            end
            ";

            // Create the Narrator Bot
            NarrAItor.Narrator.NarratorBot StanleyNarratorBot = new(
                Name: "StanleyNarratorBot",
                Version: "1.0.0",
                MaxTotalToken: 100000,
                Objective: "Narrate the story of Stanley with wit and philosophical undertones",
                UserObjective: "Guide the listener through an existential narrative",
                Personality: "Omniscient, slightly sardonic, meta-aware narrator",
                CurrentObjective: "Begin the story of Stanley"
            );

            // Create a mod for the narrator
            NarrAItor.Narrator.Modding.NarratorMod narratorMod = new()
            {
                LuaFileData = narratorScript
            };

            // Add the mod to the bot's installed mods
            StanleyNarratorBot.RequiredMods["StanleyNarrativeMod"] = narratorMod;

            // Initialize the mod
            narratorMod.Initialize();

            // Run the mod
            await narratorMod.Run();

            // Print out bot details
            Console.WriteLine("\nNarrator Bot Details:");
            Console.WriteLine($"Name: {StanleyNarratorBot.Name}");
            Console.WriteLine($"Version: {StanleyNarratorBot.Version}");
            Console.WriteLine($"Objective: {StanleyNarratorBot.Objective}");
            Console.WriteLine($"Personality: {StanleyNarratorBot.Personality}");
            Console.WriteLine($"Max Total Tokens: {StanleyNarratorBot.MaxTotalTokens}");
            Console.WriteLine($"Installed Mods: {StanleyNarratorBot.RequiredMods.Count}");
        });

        if (args.Length == 0)
        {
            foreach (var test in UnitTests.Values)
            {
                await InvokeTestAsync(test);
            }
        }
        else
        {
            foreach (var arg in args)
            {
                if (UnitTests.TryGetValue(arg, out var test))
                {
                    await InvokeTestAsync(test);
                }
            }
        }
    }
    
    private static async Task InvokeTestAsync(Delegate test)
    {
        if (test is Func<Task> asyncTest)
        {
            await asyncTest();
        }
        else if (test is Action syncTest)
        {
            syncTest();
        }
        else
        {
            Console.WriteLine($"Unsupported test type: {test.GetType()}");
        }
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
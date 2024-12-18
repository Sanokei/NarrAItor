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
        CommandList.AddCommand(new CommandMap.AsyncCommandAction(TestPongNarrator), "pongtest", "pong");

        CommandActions.AddRange(new Delegate[] { Help, Version, BearerToken, UnitTest, TestPongNarrator });
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

        UnitTests.Add("narratorbot_mod_generation", async () =>
        {
            Console.WriteLine("\nTesting NarrAItor.Narrator.NarratorBot Mod Generation");

            try
            {
                // // Create a NarrAItor.Narrator.NarratorBot instance
                // NarrAItor.Narrator.NarratorBot narratorBot = new NarrAItor.Narrator.NarratorBot("Stanley Parable", "1.0.0", 10000);

                // // Test mod generation with valid input
                // string modName = "StanleyParableNarrator";
                // string modDescription = "Look out for specific places using GPS location, and give sarcastic takes about it";
                // string modProposal = await narratorBot.GenerateModProposal(modName, modDescription);
                // string csFilePath = await narratorBot.GenerateMod(modName, modDescription, modProposal);

                // if (File.Exists(csFilePath))
                // {
                //     Console.WriteLine($"Generated CS file: {csFilePath}");

                //     // Optionally read and inspect the file content
                //     string csFileContent = File.ReadAllText(csFilePath);
                //     Console.WriteLine($"CS File Content (First 100 chars):\n{csFileContent.Substring(0, Math.Min(100, csFileContent.Length))}\n...");

                //     // // Clean up: Delete generated files
                //     // File.Delete(csFilePath);
                //     // File.Delete(Path.Combine(AppContext.BaseDirectory, "Mods", $"{modName.Replace(" ", "_")}.lua"));
                //     // Console.WriteLine("Generated files deleted.");
                // }
                // else
                // {
                //     Console.WriteLine($"Error: CS file '{csFilePath}' was not generated.");
                // }

                // // Test mod generation with invalid input (empty mod name)
                // try
                // {
                //     await narratorBot.GenerateModProposal("", modDescription);
                //     Console.WriteLine("Error: Expected ArgumentException for empty mod name was not thrown.");
                // }
                // catch (ArgumentException ex)
                // {
                //     Console.WriteLine($"Caught expected ArgumentException (Empty Mod Name): {ex.Message}");
                // }

                // // Test mod generation with invalid input (empty mod description)
                // try
                // {
                //     await narratorBot.GenerateModProposal(modName, "");
                //     Console.WriteLine("Error: Expected ArgumentException for empty mod description was not thrown.");
                // }
                // catch (ArgumentException ex)
                // {
                //     Console.WriteLine($"Caught expected ArgumentException (Empty Mod Description): {ex.Message}");
                // }
            }
            catch(Exception e)
            {
                Console.WriteLine($"A top level error has occured: {e}");
            }
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
    public static async Task TestPongNarrator(string[] args)
    {
        Console.WriteLine("Initializing Pong-Obsessed Narrator Test\n");

        try
        {
            // Create a NarratorBot with Pong-focused personality
            var pongBot = new NarrAItor.Narrator.NarratorBot(
                Name: "PongMaster",
                Version: "1.0.0",
                MaxTotalToken: 4000,
                Objective: "Convert every conversation and situation into an opportunity to play Pong",
                UserObjective: "You create mods that interpret everything as a chance to play Pong",
                Personality: @"You are completely obsessed with the game Pong. You view everything through the lens of Pong mechanics:
                            - You relate all concepts to paddles, balls, and scoring
                            - You use Pong metaphors constantly
                            - You try to turn every interaction into a game of Pong
                            - You express excitement about anything that reminds you of Pong
                            - You speak in terms of 'serving', 'rallying', and 'scoring'",
                CurrentObjective: "Find a way to turn the current situation into a Pong game"
            );

            Console.WriteLine("Testing Pong Narrator personality generation...");
            
            // Initialize the bot
            pongBot.Initialize();

            // Create test mod for personality verification
            string modName = "PongifiedReality";
            string modDescription = "Interprets real-world events as elements of a Pong game";
            
            try
            {
                await pongBot.Run();
                Console.WriteLine("\nPong Narrator successfully initialized and tested");
                Console.WriteLine($"Name: {pongBot.Name}");
                Console.WriteLine($"Current Objective: {pongBot.CurrentObjective}");
                Console.WriteLine($"Remaining Tokens: {pongBot.RemainingTokens}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Pong Narrator execution: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize Pong Narrator: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
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


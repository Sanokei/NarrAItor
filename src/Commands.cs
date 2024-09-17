using NarrAItor.Utils;

namespace NarrAItor
{
    public static class Commands
    {
        public static CommandMap CommandList { get; } = new();
        public static List<CommandMap.CommandAction> CommandActions{ get; } = [Help, Version, BearerToken];

        static Commands()
        {
            CommandList.AddCommand(Help, "help", "h", "?");
            CommandList.AddCommand(Version, "version", "v");
            CommandList.AddCommand(BearerToken, "bearertoken");
            CommandList.AddCommand(NarratorType, "narratortype", "type");
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
            Environment.SetEnvironmentVariable("BearerToken", args[0]);
        }

        public static void NarratorType(string[] args)
        {
            Environment.SetEnvironmentVariable("NarratorType", args[0]);
        }
    }
}
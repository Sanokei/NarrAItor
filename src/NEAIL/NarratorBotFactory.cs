using System;
using System.Collections.Generic;
using NarrAItor.Narrator.Modding;
using NarrAItor.Narrator.Modding.Base;

namespace NarrAItor.Narrator.NEAIL;

/// <summary>
/// 
/// </summary>
/// <example>
/// // Create a default NarratorBot
/// var defaultBot = NarratorBotFactory.CreateDefaultNarratorBot();

/// // Create a custom NarratorBot
/// var customBot = NarratorBotFactory.CreateCustomNarratorBot(
///     "CustomNarrator",
///     "1.0.0",
///     "Narrate epic stories",
///     "Dramatic and mysterious",
///     "./CustomNarrator/docs/",
///     new Dictionary<string, NarratorMod>()
/// );

/// // Create a NarratorBot from a config object
/// var config = new NarratorBotConfig
/// {
///     Name = "ConfigNarrator",
///     Version = "2.0.0",
///     Objective = "Provide insightful narration",
///     Personality = "Wise and calm",
///     DocumentationPath = "./ConfigNarrator/docs/",
///     InstalledMods = new Dictionary<string, NarratorMod>()
/// };
/// var configBot = NarratorBotFactory.CreateNarratorBotFromConfig(config);
/// </example>
public class NarratorBotFactory
{
    public static NarratorBot CreateDefaultNarratorBot()
    {
        return new NarratorBot();
    }

    public static NarratorBot CreateCustomNarratorBot(
        string name,
        string version,
        string objective,
        string personality = "",
        Dictionary<string, NarratorMod> installedMods = null)
    {
        var bot = new NarratorBot
        {
            Name = name,
            Version = version,
            Objective = objective,
            Personality = personality,
        };

        if (installedMods != null)
        {
            bot.RequiredMods = installedMods;
        }

        return bot;
    }

    public static NarratorBot CreateNarratorBotFromConfig(INarratorBot config)
    {
        return CreateCustomNarratorBot(
            config.Name,
            config.Version,
            config.Objective,
            config.Personality,
            config.RequiredMods
        );
    }
}
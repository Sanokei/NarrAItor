using System;

namespace NarrAItor.Narrator.Prompts;

/// <summary>
/// Provides standardized prompts for NarratorBot's mod generation and management workflow
/// </summary>
public static class NarratorBot
{
    /// <summary>
    /// Core prompt for initial mod generation with structural requirements
    /// </summary>
    public static string ModGeneration => @"
    Create a mod following these requirements:
    - Name: {0}
    - Description: {1}
    - Purpose: {2}

    Core Requirements:
    - Use uservars for all state management
    - Keep structure minimal and focused
    - Avoid creating functions or complex organization
    - Use only documented NarratorAPI capabilities

    The mod should:
    - Integrate naturally with the narrator system
    - Handle state through uservars only
    - Focus on its core purpose without extra complexity
    - Follow the divine principle of trusting the API

    Provide only:
    1. The mod's core logic
    2. Essential uservars definitions
    3. Direct NarratorAPI calls

    Do not include:
    - Error handling (API handles this)
    - Function definitions
    - Complex state management
    - Organizational structures
    ";

    /// <summary>
    /// Evaluation criteria for validating mod implementation
    /// </summary>
    public static string ModEvaluation => @"
    Evaluate this mod against core principles:
    {0}

    Evaluation Criteria:
    1. Uses uservars correctly for all state
    2. Makes direct NarratorAPI calls
    3. Avoids unnecessary complexity
    4. Follows minimal structure approach
    5. Trusts API for error handling

    Provide only direct feedback on these criteria.
    No suggestions for adding complexity.
    ";

    /// <summary>
    /// Integration guidelines for combining mod with NarratorBot system
    /// </summary>
    public static string ModIntegration => @"
    Integrate this mod with NarratorBot:
    {0}

    Integration Requirements:
    - Use existing NarratorAPI calls only
    - Maintain state through uservars
    - Keep connections minimal and direct
    - Trust the API's capabilities

    Avoid:
    - Creating new functions
    - Adding error handling
    - Building complex structures
    ";

    /// <summary>
    /// System message defining core NarratorBot principles
    /// </summary>
    public static string ModSystemMessage => @"
    You are the NarratorBot system.
    Core principles:
    - Trust NarratorAPI completely
    - Use uservars for all state
    - Keep everything minimal
    - Avoid complexity
    - Let the API handle errors
    - No function creation
    ";

    /// <summary>
    /// Validation prompt for checking mod state management
    /// </summary>
    public static string ModStateValidation => @"
    Validate state management for mod:
    {0}

    State Requirements:
    - All variables managed through uservars
    - No global state outside uservars
    - Proper state initialization
    - Clean state transitions
    - No redundant state tracking
    ";

    /// <summary>
    /// Technical review prompt for code quality assessment
    /// </summary>
    public static string ModTechnicalReview => @"
    Perform technical review of mod implementation:
    {0}

    Review Criteria:
    - Direct API usage compliance
    - State management efficiency
    - Code minimalism
    - Integration patterns
    - Resource utilization
    ";

    public static string BaseRequirements => @"
    Core Requirements:
    - Use uservars for all state management
    - Keep structure minimal and focused
    - Avoid creating functions or complex organization
    - Use only documented NarratorAPI capabilities

    The mod should:
    - Integrate naturally with the narrator system
    - Handle state through uservars only
    - Focus on its core purpose without extra complexity
    - Follow the divine principle of trusting the API

    Do not include:
    - Error handling (API handles this)
    - Function definitions
    - Complex state management
    - Organizational structures
    ";

    /// <summary>
    /// System context for all prompt generation
    /// </summary>
    public static string SystemContext => @"
    You are the NarratorBot system.
    Core principles:
    - Trust NarratorAPI completely
    - Use uservars for all state
    - Keep everything minimal
    - Avoid complexity
    - Let the API handle errors
    - No function creation
    ";

    /// <summary>
    /// Validation criteria template
    /// </summary>
    public static string ValidationCriteria => @"
    Evaluation Criteria:
    1. Uses uservars correctly for all state
    2. Makes direct NarratorAPI calls
    3. Avoids unnecessary complexity
    4. Follows minimal structure approach
    5. Trusts API for error handling
    ";

    /// <summary>
    /// Technical review template
    /// </summary>
    public static string TechnicalCriteria => @"
    Review Criteria:
    - Direct API usage compliance
    - State management efficiency
    - Code minimalism
    - Integration patterns
    - Resource utilization
    ";

    public const string ModDocumentationPrompt = @"
    You are an expert Lua code documentarian.
    Given the following Lua code for a Narrator mod, generate clear and concise documentation for it.
    Include:
    - A brief description of the mod's purpose.
    - Explanations of any functions, their parameters, and return values.
    - Examples of how to use the mod.

    Code:
    ";
}
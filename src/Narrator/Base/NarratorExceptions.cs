namespace NarrAItor.Narrator.NarratorExceptions;

/// <summary>
/// Use Exceptions very carefully. Within the lua binding instead of errors, there will be
/// tags to give the LLM more chances to get something right.
/// </summary>
public struct Errors
{

}
public struct Warnings
{
    public static string DOCUMENTATION_PATH_DOES_NOT_EXIST = "Warning: Documentation directory does not exist. Using default documentation prompt.";
}
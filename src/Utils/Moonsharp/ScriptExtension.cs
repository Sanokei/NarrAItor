
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

public static class ScriptExtension
{
    public static async Task<DynValue> DoStringAsync(this Script script, string codeScript, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();

        try
        {
            var code = script.LoadString(codeScript);
            var coRoutine = script.CreateCoroutine(code);
            coRoutine.Coroutine.AutoYieldCounter = 1000;

            DynValue scriptResult;
            DynValue? resumeArg = null;

            while (true)
            {
                try
                {
                    scriptResult = resumeArg == null ? coRoutine.Coroutine.Resume() : coRoutine.Coroutine.Resume(resumeArg);
                }
                catch (ScriptRuntimeException ex)
                {
                    Console.WriteLine($"Lua runtime error: {ex.DecoratedMessage}");
                    throw;
                }

                resumeArg = null;

                if (scriptResult.Type == DataType.YieldRequest)
                {
                    cancellation.ThrowIfCancellationRequested();
                }
                else if (scriptResult.Type == DataType.UserData)
                {
                    if (scriptResult.UserData.Descriptor.Type != typeof(AnonWrapper))
                    {
                        break;
                    }

                    var userData = scriptResult.UserData.Object;

                    if (userData is not AnonWrapper<TaskDescriptor> wrapper)
                    {
                        break;
                    }

                    var taskDescriptor = wrapper.Value;

                    var taskResult = await taskDescriptor.Task;

                    if (taskResult is Exception ex)
                    {
                        throw new ScriptRuntimeException($"Async operation failed: {ex.Message}", ex);
                    }

                    if (taskDescriptor.HasResult)
                    {
                        resumeArg = DynValue.FromObject(script, taskResult);
                    }
                }
                else
                {
                    break;
                }
            }

            return scriptResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing Lua script: {ex.Message}");
            throw;
        }
    }
}
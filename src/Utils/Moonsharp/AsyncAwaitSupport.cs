public class TaskDescriptor
{
    public Task<object?> Task { get; private set; }
    public bool HasResult { get; private set; }

    public static TaskDescriptor Build(Func<Task> taskAction)
    {
        return new TaskDescriptor
        {
            Task = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await taskAction();
                    return (object?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }),
            HasResult = false
        };
    }

    public static TaskDescriptor Build<T>(Func<Task<T>> taskAction)
    {
        return new TaskDescriptor
        {
            Task = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    return (object?)await taskAction();
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }),
            HasResult = true
        };
    }
}
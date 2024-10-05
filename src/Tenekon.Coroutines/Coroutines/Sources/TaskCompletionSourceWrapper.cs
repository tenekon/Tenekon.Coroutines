namespace Tenekon.Coroutines.Sources;

internal class TaskCompletionSourceWrapper<TResult> : TaskCompletionSource<TResult>, ICompletionSource<TResult>
{
    public TaskCompletionSourceWrapper()
    {
    }

    public TaskCompletionSourceWrapper(object? state) : base(state)
    {
    }

    public TaskCompletionSourceWrapper(TaskCreationOptions creationOptions) : base(creationOptions)
    {
    }

    public TaskCompletionSourceWrapper(object? state, TaskCreationOptions creationOptions) : base(state, creationOptions)
    {
    }
}

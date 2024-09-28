namespace Tenekon.Coroutines;

internal interface ICoroutineAwaiter<TResult>
{
    bool IsCompleted { get; }
    TResult GetResult();
}

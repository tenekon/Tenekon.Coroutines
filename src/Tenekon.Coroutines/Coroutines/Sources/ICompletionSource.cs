namespace Tenekon.Coroutines.Sources;

internal interface ICompletionSource<TResult>
{
    void SetResult(TResult result);
    void SetException(Exception e);
}

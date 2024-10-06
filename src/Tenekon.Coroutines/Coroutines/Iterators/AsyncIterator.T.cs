using System.Text;

namespace Tenekon.Coroutines.Iterators;

partial class AsyncIteratorImpl<TResult>
{
    internal static readonly Key s_asyncIteratorKey = new(Encoding.ASCII.GetBytes(nameof(AsyncIterator)));

    object IAsyncIterator<TResult>.Current { get => Current; set => Current = value; }

    ValueTask<bool> IAsyncIterator<TResult>.MoveNextAsync() => MoveNextAsync();

    void IAsyncIterator<TResult>.YieldAssign<TYieldResult>(TYieldResult result) => YieldAssign(result);

    void IAsyncIterator<TResult>.YieldAssign() => YieldAssign();

    void IAsyncIterator<TResult>.Return(TResult result) => Return(result);

    void IAsyncIterator<TResult>.Throw(Exception e) => Throw(e);

    TResult IAsyncIterator<TResult>.GetResult() => GetResult();

    Coroutine<TResult> IAsyncIterator<TResult>.GetResultAsync() => GetResultAsync();
}

using System.Text;

namespace Tenekon.Coroutines.Iterators;

public static class AsyncIterator
{
    internal static readonly Key s_asyncIteratorKey = new(Encoding.ASCII.GetBytes(nameof(AsyncIterator)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAsyncIterator Create(Func<Coroutine> provider, in CoroutineContext additiveContext = default, bool isCloneable = false) =>
        new AsyncIteratorImpl<VoidCoroutineResult>(provider, in additiveContext, isCloneable: isCloneable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAsyncIterator Create(Coroutine coroutine, in CoroutineContext additiveContext = default, bool isCloneable = false) =>
        new AsyncIteratorImpl<VoidCoroutineResult>(coroutine, in additiveContext, isCloneable: isCloneable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAsyncIterator<TResult> Create<TResult>(Func<Coroutine<TResult>> provider, in CoroutineContext additiveContext = default, bool isCloneable = false) =>
        new AsyncIteratorImpl<TResult>(provider, in additiveContext, isCloneable: isCloneable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAsyncIterator<TResult> Create<TResult>(Coroutine<TResult> coroutine, in CoroutineContext additiveContext = default, bool isCloneable = false) =>
        new AsyncIteratorImpl<TResult>(coroutine, in additiveContext, isCloneable: isCloneable);
}

partial class AsyncIteratorImpl<TResult>
{
    object IAsyncIterator.Current { get => Current; set => Current = value; }

    ValueTask<bool> IAsyncIterator.MoveNextAsync() => MoveNextAsync();

    void IAsyncIterator.YieldAssign<TYieldResult>(TYieldResult result) => YieldAssign(result);

    void IAsyncIterator.YieldAssign() => YieldAssign();

    void IAsyncIterator.Return() => Return(default!);

    void IAsyncIterator.Throw(Exception e) => Throw(e);

    void IAsyncIterator.GetResult() => _ = GetResult();

    Coroutine IAsyncIterator.GetResultAsync()
    {
        var coroutine = GetResultAsync();
        return Unsafe.As<Coroutine<TResult>, Coroutine>(ref coroutine);
    }
}

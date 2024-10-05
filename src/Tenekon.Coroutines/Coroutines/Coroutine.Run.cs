using Tenekon.Coroutines.CompilerServices;
using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial struct Coroutine
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<CoroutineAwaitable> RunInternal<TClosure>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, CancellationToken cancellationToken)
    {
        var completionSource = ManualResetCoroutineCompletionSource<CoroutineAwaitable>.RentFromCache();
        var argument = new RunArgument<TClosure>(provider, closure, providerFlags, cancellationToken, completionSource);
        return new(completionSource, argument);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<CoroutineAwaitable<TResult>> RunInternal<TClosure, TResult>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, CancellationToken cancellationToken)
    {
        var completionSource = ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>.RentFromCache();
        var argument = new RunArgument<TClosure, TResult>(provider, closure, providerFlags, cancellationToken, completionSource);
        return new(completionSource, argument);
    }

    public static Coroutine<CoroutineAwaitable> Run(Func<Coroutine> provider, CancellationToken cancellationToken) =>
        RunInternal<object?>(provider, closure: null, CoroutineProviderFlags.None, cancellationToken);

    public static Coroutine<CoroutineAwaitable> Run(Func<Coroutine> provider) =>
        RunInternal<object?>(provider, closure: null, CoroutineProviderFlags.None, CancellationToken.None);

    public static Coroutine<CoroutineAwaitable> Run<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, CancellationToken cancellationToken) =>
        RunInternal<object?>(provider, closure, CoroutineProviderFlags.None, cancellationToken);

    public static Coroutine<CoroutineAwaitable> Run<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure) =>
        RunInternal<object?>(provider, closure, CoroutineProviderFlags.None, CancellationToken.None);

    public static Coroutine<CoroutineAwaitable<TResult>> Run<TResult>(Func<Coroutine<TResult>> provider, CancellationToken cancellationToken) =>
        RunInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None, cancellationToken);

    public static Coroutine<CoroutineAwaitable<TResult>> Run<TResult>(Func<Coroutine<TResult>> provider) =>
        RunInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None, CancellationToken.None);

    public static Coroutine<CoroutineAwaitable<TResult>> Run<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, CancellationToken cancellationToken) =>
        RunInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.None, cancellationToken);

    public static Coroutine<CoroutineAwaitable<TResult>> Run<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure) =>
        RunInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.None, CancellationToken.None);
}

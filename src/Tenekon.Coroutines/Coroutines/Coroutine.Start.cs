using Tenekon.Coroutines.CompilerServices;

namespace Tenekon.Coroutines;

partial struct Coroutine
{
    private static Coroutine StartInternal<TClosure>(Delegate provider, TClosure closure, in CoroutineContext contextToBequest, bool isProviderWithClosure)
    {
        if (provider is null) {
            throw new ArgumentNullException(nameof(provider));
        }
        Coroutine coroutine;
        if (isProviderWithClosure) {
            coroutine = Unsafe.As<Delegate, Func<TClosure, Coroutine>>(ref provider)(closure);
        } else {
            coroutine = Unsafe.As<Delegate, Func<Coroutine>>(ref provider)();
        }
        CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutine, in contextToBequest);
        return coroutine;
    }

    private static Coroutine<TResult> StartInternal<TClosure, TResult>(Delegate provider, TClosure closure, in CoroutineContext contextToBequest, bool isProviderWithClosure)
    {
        if (provider is null) {
            throw new ArgumentNullException(nameof(provider));
        }
        Coroutine<TResult> coroutine;
        if (isProviderWithClosure) {
            coroutine = Unsafe.As<Delegate, Func<TClosure, Coroutine<TResult>>>(ref provider)(closure);
        } else {
            coroutine = Unsafe.As<Delegate, Func<Coroutine<TResult>>>(ref provider)();
        }
        CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutine, in contextToBequest);
        return coroutine;
    }

    public static CoroutineAwaitable Start(Func<Coroutine> provider, CoroutineContext context = default) =>
        new(StartInternal<object?>(provider, null, in context, isProviderWithClosure: false));

    public static CoroutineAwaitable Start<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, CoroutineContext context = default) =>
        new(StartInternal(provider, closure, in context, isProviderWithClosure: true));

    public static CoroutineAwaitable<TResult> Start<TResult>(Func<Coroutine<TResult>> provider, CoroutineContext context = default) =>
        new(StartInternal<object?, TResult>(provider, null, in context, isProviderWithClosure: false));

    public static CoroutineAwaitable<TResult> Start<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, CoroutineContext context = default) =>
        new(StartInternal<TClosure, TResult>(provider, closure, in context, isProviderWithClosure: true));
}

using Tenekon.Coroutines.CompilerServices;
using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial struct Coroutine
{
    partial class Factory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Coroutine<CoroutineAwaitable> StartNewInternal<TClosure>(
        Delegate provider,
        TClosure closure,
        CoroutineProviderFlags providerFlags,
        CancellationToken cancellationToken,
        TaskCreationOptions creationOptions,
        TaskScheduler? scheduler,
        TaskFactoryExecutionPath taskFactoryExecutionPath)
        {
            var completionSource = ManualResetCoroutineCompletionSource<CoroutineAwaitable>.RentFromCache();
            var argument = new StartNewArgument<TClosure>(provider, closure, providerFlags, cancellationToken, creationOptions, scheduler, taskFactoryExecutionPath, completionSource);
            return new(completionSource, argument);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Coroutine<CoroutineAwaitable<TResult>> StartNewInternal<TClosure, TResult>(
            Delegate provider,
            TClosure closure,
            CoroutineProviderFlags providerFlags,
            CancellationToken cancellationToken,
            TaskCreationOptions creationOptions,
            TaskScheduler? scheduler,
            TaskFactoryExecutionPath taskFactoryExecutionPath)
        {
            var completionSource = ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>.RentFromCache();
            var argument = new StartNewArgument<TClosure, TResult>(provider, closure, providerFlags, cancellationToken, creationOptions, scheduler, taskFactoryExecutionPath, completionSource);
            return new(completionSource, argument);
        }

        public static Coroutine<CoroutineAwaitable> StartNew(Func<Coroutine> provider, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) =>
            StartNewInternal<object?>(provider, closure: null, CoroutineProviderFlags.None, cancellationToken, creationOptions, scheduler, TaskFactoryExecutionPath.Scheduler);

        public static Coroutine<CoroutineAwaitable> StartNew(Func<Coroutine> provider, CancellationToken cancellationToken) =>
            StartNewInternal<object?>(provider, closure: null, CoroutineProviderFlags.None, cancellationToken, TaskCreationOptions.None, scheduler: null, TaskFactoryExecutionPath.CancellationToken);

        public static Coroutine<CoroutineAwaitable> StartNew(Func<Coroutine> provider, TaskCreationOptions creationOptions) =>
            StartNewInternal<object?>(provider, closure: null, CoroutineProviderFlags.None, CancellationToken.None, creationOptions, scheduler: null, TaskFactoryExecutionPath.CreationOptions);

        public static Coroutine<CoroutineAwaitable> StartNew(Func<Coroutine> provider) =>
            StartNewInternal<object?>(provider, closure: null, CoroutineProviderFlags.None, CancellationToken.None, TaskCreationOptions.None, scheduler: null, TaskFactoryExecutionPath.None);

        public static Coroutine<CoroutineAwaitable> StartNew<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) =>
            StartNewInternal<object?>(provider, closure, CoroutineProviderFlags.None, cancellationToken, creationOptions, scheduler, TaskFactoryExecutionPath.Scheduler);

        public static Coroutine<CoroutineAwaitable> StartNew<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, CancellationToken cancellationToken) =>
            StartNewInternal<object?>(provider, closure, CoroutineProviderFlags.None, cancellationToken, TaskCreationOptions.None, scheduler: null, TaskFactoryExecutionPath.CancellationToken);

        public static Coroutine<CoroutineAwaitable> StartNew<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, TaskCreationOptions creationOptions) =>
            StartNewInternal<object?>(provider, closure, CoroutineProviderFlags.None, CancellationToken.None, creationOptions, scheduler: null, TaskFactoryExecutionPath.CreationOptions);

        public static Coroutine<CoroutineAwaitable> StartNew<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure) =>
            StartNewInternal<object?>(provider, closure, CoroutineProviderFlags.None, CancellationToken.None, TaskCreationOptions.None, scheduler: null, TaskFactoryExecutionPath.None);

        public static Coroutine<CoroutineAwaitable<TResult>> StartNew<TResult>(Func<Coroutine<TResult>> provider, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) =>
            StartNewInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None, cancellationToken, creationOptions, scheduler, TaskFactoryExecutionPath.Scheduler);

        public static Coroutine<CoroutineAwaitable<TResult>> StartNew<TResult>(Func<Coroutine<TResult>> provider, CancellationToken cancellationToken) =>
            StartNewInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None, cancellationToken, TaskCreationOptions.None, scheduler: null, TaskFactoryExecutionPath.CancellationToken);

        public static Coroutine<CoroutineAwaitable<TResult>> StartNew<TResult>(Func<Coroutine<TResult>> provider, TaskCreationOptions creationOptions) =>
            StartNewInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None, CancellationToken.None, creationOptions, scheduler: null, TaskFactoryExecutionPath.CreationOptions);

        public static Coroutine<CoroutineAwaitable<TResult>> StartNew<TResult>(Func<Coroutine<TResult>> provider) =>
            StartNewInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None, CancellationToken.None, TaskCreationOptions.None, scheduler: null, TaskFactoryExecutionPath.None);

        public static Coroutine<CoroutineAwaitable<TResult>> StartNew<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) =>
            StartNewInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.None, cancellationToken, creationOptions, scheduler, TaskFactoryExecutionPath.Scheduler);

        public static Coroutine<CoroutineAwaitable<TResult>> StartNew<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, CancellationToken cancellationToken) =>
            StartNewInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.None, cancellationToken, TaskCreationOptions.None, scheduler: null, TaskFactoryExecutionPath.CancellationToken);

        public static Coroutine<CoroutineAwaitable<TResult>> StartNew<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, TaskCreationOptions creationOptions) =>
            StartNewInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.None, CancellationToken.None, creationOptions, scheduler: null, TaskFactoryExecutionPath.CreationOptions);

        public static Coroutine<CoroutineAwaitable<TResult>> StartNew<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure) =>
            StartNewInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.None, CancellationToken.None, TaskCreationOptions.None, scheduler: null, TaskFactoryExecutionPath.None);

    }
}

using Tenekon.Coroutines.CompilerServices;
using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<CoroutineAwaitable> LaunchInternal<TClosure>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<CoroutineAwaitable>.RentFromCache();
        var argument = new LaunchArgument<TClosure>(provider, closure, providerFlags, completionSource);
        return new(completionSource, argument);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<CoroutineAwaitable<TResult>> LaunchInternal<TClosure, TResult>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>.RentFromCache();
        var argument = new LaunchArgument<TClosure, TResult>(provider, closure, providerFlags, completionSource);
        return new(completionSource, argument);
    }

    public static Coroutine<CoroutineAwaitable> Launch(Func<Coroutine> provider) => LaunchInternal<object?>(provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine<CoroutineAwaitable> Launch<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure) => LaunchInternal(provider, closure, CoroutineProviderFlags.RequiresClosure);

    public static Coroutine<CoroutineAwaitable<TResult>> Launch<TResult>(Func<Coroutine<TResult>> provider) => LaunchInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine<CoroutineAwaitable<TResult>> Launch<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure) => LaunchInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.RequiresClosure);

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct LaunchArgumentCore<TClosure, TResult>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<TResult>? completionSource)
        {
            internal readonly ManualResetCoroutineCompletionSource<TResult>? _completionSource = completionSource;

            public readonly Delegate Provider = provider;
            public readonly TClosure Closure = closure;
            public readonly CoroutineProviderFlags ProviderFlags = providerFlags;

            public bool Equals(in LaunchArgumentCore<TClosure, TResult> other) =>
                ReferenceEquals(Provider, other.Provider)
                && Equals(Closure, other.Closure)
                && ProviderFlags == other.ProviderFlags;

            public override bool Equals([AllowNull] object obj) => throw new NotImplementedException();

            public override int GetHashCode() => HashCode.Combine(Provider, Closure, ProviderFlags);
        }

        public class LaunchArgument<TClosure> : ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable>>, ISiblingCoroutine
        {
            internal readonly LaunchArgumentCore<TClosure, CoroutineAwaitable> _core;

            public Delegate Provider {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Provider;
            }

            public TClosure Closure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Closure;
            }

            public CoroutineProviderFlags ProviderFlags {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.ProviderFlags;
            }

            internal LaunchArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<CoroutineAwaitable> completionSource) =>
                _core = new LaunchArgumentCore<TClosure, CoroutineAwaitable>(provider, closure, providerFlags, completionSource);

            public LaunchArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new LaunchArgumentCore<TClosure, CoroutineAwaitable>(provider, closure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<CoroutineAwaitable> completionSource)
            {
                Coroutine coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine>>(Provider)(Closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine>>(Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();
                var completionSourceProxy = ManualResetCoroutineCompletionSource<object?>.RentFromCache();
                coroutine._task = completionSourceProxy.CreateValueTask();

                var contextToBequest = default(CoroutineContext);
                contextToBequest.TreatAsNewSibling();
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, in contextToBequest);
                context.ResultStateMachine.RegisterCriticalBackgroundTaskAndNotifyOnCompletion(ref coroutineAwaiter, () => {
                    try {
                        coroutineAwaiter.GetResult();
                        completionSourceProxy.SetResult(default);
                    } catch (Exception error) {
                        completionSourceProxy.SetException(error);
                        throw; // Must bubble up
                    }
                });
                coroutine.MarkCoroutineAsActedOn();
                completionSource.SetResult(new(in coroutine));
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in LaunchKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is LaunchArgument<TClosure> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }

        public class LaunchArgument<TClosure, TResult> : ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>>, ISiblingCoroutine
        {
            private readonly LaunchArgumentCore<TClosure, CoroutineAwaitable<TResult>> _core;

            public Delegate Provider {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Provider;
            }

            public TClosure Closure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Closure;
            }

            public CoroutineProviderFlags ProviderFlags {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.ProviderFlags;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal LaunchArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>> completionSource) =>
                _core = new LaunchArgumentCore<TClosure, CoroutineAwaitable<TResult>>(provider, closure, providerFlags, completionSource);

            public LaunchArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new LaunchArgumentCore<TClosure, CoroutineAwaitable<TResult>>(provider, closure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>> completionSource)
            {
                Coroutine<TResult> coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine<TResult>>>(Provider)(Closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine<TResult>>>(Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();
                var completionSourceProxy = ManualResetCoroutineCompletionSource<TResult>.RentFromCache();
                coroutine._task = completionSourceProxy.CreateGenericValueTask();

                var contextToBequest = default(CoroutineContext);
                contextToBequest.TreatAsNewSibling();
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, in contextToBequest);
                context.ResultStateMachine.RegisterCriticalBackgroundTaskAndNotifyOnCompletion(ref coroutineAwaiter, () => {
                    try {
                        var result = coroutineAwaiter.GetResult();
                        completionSourceProxy.SetResult(result);
                    } catch (Exception error) {
                        completionSourceProxy.SetException(error);
                        throw; // Must bubble up
                    }
                });
                coroutine.MarkCoroutineAsActedOn();
                completionSource.SetResult(new(in coroutine));
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in LaunchKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is LaunchArgument<TClosure, TResult> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

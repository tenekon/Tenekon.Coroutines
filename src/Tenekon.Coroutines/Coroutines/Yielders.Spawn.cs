using Tenekon.Coroutines.CompilerServices;
using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<CoroutineAwaitable> SpawnInternal<TClosure>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<CoroutineAwaitable>.RentFromCache();
        var argument = new SpawnArgument<TClosure>(provider, closure, providerFlags, completionSource);
        return new(completionSource, argument);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<CoroutineAwaitable<TResult>> SpawnInternal<TClosure, TResult>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>.RentFromCache();
        var argument = new SpawnArgument<TClosure, TResult>(provider, closure, providerFlags, completionSource);
        return new(completionSource, argument);
    }

    public static Coroutine<CoroutineAwaitable> Spawn(Func<Coroutine> provider) => SpawnInternal<object?>(provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine<CoroutineAwaitable> Spawn<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure) => SpawnInternal(provider, closure, CoroutineProviderFlags.RequiresClosure);

    public static Coroutine<CoroutineAwaitable<TResult>> Spawn<TResult>(Func<Coroutine<TResult>> provider) => SpawnInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine<CoroutineAwaitable<TResult>> Spawn<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure) => SpawnInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.RequiresClosure);

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct SpawnArgumentCore<TClosure, TResult>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<TResult>? completionSource)
        {
            internal readonly ManualResetCoroutineCompletionSource<TResult>? _completionSource = completionSource;

            public readonly Delegate Provider = provider;
            public readonly TClosure Closure = closure;
            public readonly CoroutineProviderFlags ProviderFlags = providerFlags;

            public override int GetHashCode() => HashCode.Combine(Provider, Closure, ProviderFlags);

            public override bool Equals([AllowNull] object obj) => throw new NotImplementedException();

            public bool Equals(in SpawnArgumentCore<TClosure, TResult> other) =>
                ReferenceEquals(Provider, other.Provider)
                && Equals(Closure, other.Closure)
                && ProviderFlags == other.ProviderFlags;
        }

        public class SpawnArgument<TClosure> : ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable>>, ISiblingCoroutine
        {
            internal readonly SpawnArgumentCore<TClosure, CoroutineAwaitable> _core;

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

            internal SpawnArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<CoroutineAwaitable> completionSource) =>
                _core = new SpawnArgumentCore<TClosure, CoroutineAwaitable>(provider, closure, providerFlags, completionSource);

            public SpawnArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new SpawnArgumentCore<TClosure, CoroutineAwaitable>(provider, closure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<CoroutineAwaitable> completionSource)
            {
                Coroutine coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine>>(Provider)(Closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine>>(Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                var contextToBequest = default(CoroutineContext);
                contextToBequest.TreatAsNewChild();
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                Coroutine childCoroutine;
                if (coroutine._coroutineAction != CoroutineAction.Child) {
                    childCoroutine = CoroutineMethodBuilderCore.MakeChildCoroutine(ref coroutineAwaiter, ref contextToBequest);
                } else {
                    childCoroutine = coroutine;
                }
                var childCoroutineAwaiter = childCoroutine.ConfigureAwait(false).GetAwaiter();

                var completionSourceProxy = ManualResetCoroutineCompletionSource<VoidCoroutineResult>.RentFromCache();
                childCoroutine._task = completionSourceProxy.CreateValueTask();
                CoroutineMethodBuilderCore.ActOnCoroutine(ref childCoroutineAwaiter, in contextToBequest);
                childCoroutineAwaiter.DelegateCoroutineCompletion(completionSourceProxy);
                childCoroutine.MarkCoroutineAsActedOn();
                completionSource.SetResult(new(in childCoroutine));
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in SpawnKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is SpawnArgument<TClosure> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }

        public class SpawnArgument<TClosure, TResult> : ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>>, ISiblingCoroutine
        {
            private readonly SpawnArgumentCore<TClosure, CoroutineAwaitable<TResult>> _core;

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
            internal SpawnArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>> completionSource) =>
                _core = new SpawnArgumentCore<TClosure, CoroutineAwaitable<TResult>>(provider, closure, providerFlags, completionSource);

            public SpawnArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new SpawnArgumentCore<TClosure, CoroutineAwaitable<TResult>>(provider, closure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<CoroutineAwaitable<TResult>> completionSource)
            {
                Coroutine<TResult> coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine<TResult>>>(Provider)(Closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine<TResult>>>(Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                var contextToBequest = default(CoroutineContext);
                contextToBequest.TreatAsNewChild();
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                Coroutine<TResult> childCoroutine;
                if (coroutine._coroutineAction != CoroutineAction.Child) {
                    childCoroutine = CoroutineMethodBuilderCore.MakeChildCoroutine<ConfiguredCoroutineAwaiter<TResult>, TResult>(ref coroutineAwaiter, ref contextToBequest);
                } else {
                    childCoroutine = coroutine;
                }
                var childCoroutineAwaiter = childCoroutine.ConfigureAwait(false).GetAwaiter();

                var completionSourceProxy = ManualResetCoroutineCompletionSource<TResult>.RentFromCache();
                childCoroutine._task = completionSourceProxy.CreateGenericValueTask();
                CoroutineMethodBuilderCore.ActOnCoroutine(ref childCoroutineAwaiter, in contextToBequest);
                childCoroutineAwaiter.DelegateCoroutineCompletion(completionSourceProxy);
                childCoroutine.MarkCoroutineAsActedOn();
                completionSource.SetResult(new(in childCoroutine));
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in SpawnKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is SpawnArgument<TClosure, TResult> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

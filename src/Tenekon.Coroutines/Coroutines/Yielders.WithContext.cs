using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine WithContextInternal<TClosure>(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<VoidCoroutineResult>.RentFromCache();
        var argument = new WithContextArgument<TClosure>(in additiveContext, provider, closure, providerFlags, completionSource);
        return new(completionSource, argument);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<TResult> WithContextInternal<TClosure, TResult>(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<TResult>.RentFromCache();
        var argument = new WithContextArgument<TClosure, TResult>(in additiveContext, provider, closure, providerFlags, completionSource);
        return new(completionSource, argument);
    }

    public static Coroutine WithContext(in CoroutineContext additiveContext, CoroutineProviderDelegate provider) => WithContextInternal<object?>(in additiveContext, provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine WithContext<TClosure>(in CoroutineContext additiveContext, CoroutineProviderWithClosureDelegate<TClosure> provider, TClosure closure) => WithContextInternal(in additiveContext, provider, closure, CoroutineProviderFlags.RequiresClosure);

    public static Coroutine<TResult> WithContext<TResult>(in CoroutineContext additiveContext, CoroutineProviderDelegate<TResult> provider) => WithContextInternal<object?, TResult>(in additiveContext, provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine<TResult> WithContext<TClosure, TResult>(in CoroutineContext additiveContext, CoroutineProviderWithClosureDelegate<TClosure, TResult> provider, TClosure closure) => WithContextInternal<TClosure, TResult>(in additiveContext, provider, closure, CoroutineProviderFlags.RequiresClosure);

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct WithContextArgumentCore<TClosure, TResult>(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<TResult>? completionSource)
        {
            internal readonly ManualResetCoroutineCompletionSource<TResult>? _completionSource = completionSource;

            public readonly CoroutineContext AdditiveContext = additiveContext;
            public readonly Delegate Provider = provider;
            public readonly TClosure Closure = closure;
            public readonly CoroutineProviderFlags ProviderFlags = providerFlags;

            public bool Equals(in WithContextArgumentCore<TClosure, TResult> other) =>
                CoroutineContextEqualityComparer.Equals(in AdditiveContext, other.AdditiveContext)
                && ReferenceEquals(Provider, other.Provider)
                && Equals(Closure, other.Closure)
                && ProviderFlags == other.ProviderFlags;

            public override bool Equals([AllowNull] object obj) => throw new NotImplementedException();

            public override int GetHashCode() => HashCode.Combine(AdditiveContext, Provider, Closure, ProviderFlags);
        }

        public class WithContextArgument<TClosure> : ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>, ISiblingCoroutine
        {
            internal readonly WithContextArgumentCore<TClosure, VoidCoroutineResult> _core;

            public ref readonly CoroutineContext AdditiveContext {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _core.AdditiveContext;
            }

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

            internal WithContextArgument(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource) =>
                _core = new WithContextArgumentCore<TClosure, VoidCoroutineResult>(in additiveContext, provider, closure, providerFlags, completionSource);

            public WithContextArgument(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new WithContextArgumentCore<TClosure, VoidCoroutineResult>(in additiveContext, provider, closure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource)
            {
                Coroutine coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<CoroutineProviderWithClosureDelegate<TClosure>>(_core.Provider)(_core.Closure);
                } else {
                    coroutine = Unsafe.As<CoroutineProviderDelegate>(_core.Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                var contextToBequest = _core.AdditiveContext;
                contextToBequest.TreatAsNewSibling(additionalBequesterOrigin: CoroutineContextBequesterOrigin.ContextBequester);
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, in contextToBequest);
                coroutineAwaiter.DelegateCoroutineCompletion(completionSource);
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in WithContextKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is WithContextArgument<TClosure> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }

        public class WithContextArgument<TClosure, TResult> : ICallableArgument<ManualResetCoroutineCompletionSource<TResult>>, ISiblingCoroutine
        {
            private readonly WithContextArgumentCore<TClosure, TResult> _core;

            public ref readonly CoroutineContext AdditiveContext {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _core.AdditiveContext;
            }

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
            internal WithContextArgument(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<TResult> completionSource) =>
                _core = new WithContextArgumentCore<TClosure, TResult>(in additiveContext, provider, closure, providerFlags, completionSource);

            public WithContextArgument(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new WithContextArgumentCore<TClosure, TResult>(in additiveContext, provider, closure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<TResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<TResult> completionSource)
            {
                Coroutine<TResult> coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<CoroutineProviderWithClosureDelegate<TClosure, TResult>>(_core.Provider)(_core.Closure);
                } else {
                    coroutine = Unsafe.As<CoroutineProviderDelegate<TResult>>(_core.Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                var contextToBequest = _core.AdditiveContext;
                contextToBequest.TreatAsNewSibling(additionalBequesterOrigin: CoroutineContextBequesterOrigin.ContextBequester);
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, in contextToBequest);
                coroutineAwaiter.DelegateCoroutineCompletion(completionSource);
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in WithContextKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is WithContextArgument<TClosure, TResult> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

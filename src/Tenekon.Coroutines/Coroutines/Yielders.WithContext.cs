using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine WithContextInternal<TClosure>(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<Nothing>.RentFromCache();
        var argument = new WithContextArgument<TClosure>(in additiveContext, provider, closure, providerFlags, completionSource);
        return new Coroutine(completionSource.CreateValueTask(), argument);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<TResult> WithContextInternal<TClosure, TResult>(in CoroutineContext additiveContext, Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<TResult>.RentFromCache();
        var argument = new WithContextArgument<TClosure, TResult>(in additiveContext, provider, closure, providerFlags, completionSource);
        return new Coroutine<TResult>(completionSource.CreateGenericValueTask(), argument);
    }

    public static Coroutine WithContext(in CoroutineContext additiveContext, CoroutineProviderDelegate provider) => WithContextInternal<object?>(in additiveContext, provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine WithContext<TClosure>(in CoroutineContext additiveContext, CoroutineProviderWithClosureDelegate<TClosure> provider, TClosure closure) => WithContextInternal(in additiveContext, provider, closure, CoroutineProviderFlags.RequiresClosure);

    public static Coroutine<TResult> WithContext<TResult>(in CoroutineContext additiveContext, CoroutineProviderDelegate<TResult> provider) => WithContextInternal<object?, TResult>(in additiveContext, provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine<TResult> WithContext<TClosure, TResult>(in CoroutineContext additiveContext, CoroutineProviderWithClosureDelegate<TClosure, TResult> provider, TClosure closure) => WithContextInternal<TClosure, TResult>(in additiveContext, provider, closure, CoroutineProviderFlags.RequiresClosure);

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct WithContextArgumentCore<TClosure, TResult>(in CoroutineContext additiveContext, Delegate provider, TClosure providerClosure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<TResult>? completionSource)
        {
            public static bool Equals(in WithContextArgumentCore<TClosure, TResult> left, in WithContextArgumentCore<TClosure, TResult> right) =>
                CoroutineContextEqualityComparer.Equals(in left.AdditiveContext, right.AdditiveContext)
                && ReferenceEquals(left.Provider, right.Provider)
                && Equals(left.ProviderClosure, right.ProviderClosure)
                && left.ProviderFlags == right.ProviderFlags;

            internal readonly ManualResetCoroutineCompletionSource<TResult>? _completionSource = completionSource;

            public readonly CoroutineContext AdditiveContext = additiveContext;
            public readonly Delegate Provider = provider;
            public readonly TClosure ProviderClosure = providerClosure;
            public readonly CoroutineProviderFlags ProviderFlags = providerFlags;

            public override int GetHashCode()
            {
                var code = new HashCode();
                code.Add(AdditiveContext);
                code.Add(Provider);
                code.Add(ProviderClosure);
                code.Add(ProviderFlags);
                return code.ToHashCode();
            }
        }

        public class WithContextArgument<TClosure> : ICallableArgument<ManualResetCoroutineCompletionSource<Nothing>>, ISiblingCoroutine
        {
            internal readonly WithContextArgumentCore<TClosure, Nothing> _core;

            public ref readonly CoroutineContext AdditiveContext {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _core.AdditiveContext;
            }

            public Delegate Provider {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Provider;
            }

            public ref readonly TClosure ProviderClosure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _core.ProviderClosure;
            }

            public CoroutineProviderFlags ProviderFlags {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.ProviderFlags;
            }

            internal WithContextArgument(in CoroutineContext additiveContext, Delegate provider, TClosure providerClosure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<Nothing> completionSource) =>
                _core = new WithContextArgumentCore<TClosure, Nothing>(in additiveContext, provider, providerClosure, providerFlags, completionSource);

            internal WithContextArgument(in CoroutineContext additiveContext, Delegate provider, TClosure providerClosure, CoroutineProviderFlags providerFlags) =>
                _core = new WithContextArgumentCore<TClosure, Nothing>(in additiveContext, provider, providerClosure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<Nothing>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<Nothing> completionSource)
            {
                Coroutine coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<CoroutineProviderWithClosureDelegate<TClosure>>(_core.Provider)(_core.ProviderClosure);
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

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver)
            {
                if (_core._completionSource is null) {
                    throw new InvalidOperationException();
                }
                argumentReceiver.ReceiveCallableArgument(in WithContextKey, this, _core._completionSource);
            }

            public override bool Equals([AllowNull] object obj) => obj is WithContextArgument<TClosure> argument && WithContextArgumentCore<TClosure, Nothing>.Equals(in _core, in argument._core);

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

            public ref readonly TClosure ProviderClosure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _core.ProviderClosure;
            }

            public CoroutineProviderFlags ProviderFlags {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.ProviderFlags;
            }

            internal WithContextArgument(in CoroutineContext additiveContext, Delegate provider, TClosure providerClosure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<TResult> completionSource) =>
                _core = new WithContextArgumentCore<TClosure, TResult>(in additiveContext, provider, providerClosure, providerFlags, completionSource);

            internal WithContextArgument(in CoroutineContext additiveContext, Delegate provider, TClosure providerClosure, CoroutineProviderFlags providerFlags) =>
                _core = new WithContextArgumentCore<TClosure, TResult>(in additiveContext, provider, providerClosure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<TResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<TResult> completionSource)
            {
                Coroutine<TResult> coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<CoroutineProviderWithClosureDelegate<TClosure, TResult>>(_core.Provider)(_core.ProviderClosure);
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

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver)
            {
                if (_core._completionSource is null) {
                    throw new InvalidOperationException();
                }
                argumentReceiver.ReceiveCallableArgument(in WithContextKey, this, _core._completionSource);
            }

            public override bool Equals([AllowNull] object obj) => obj is WithContextArgument<TClosure, TResult> argument && WithContextArgumentCore<TClosure, TResult>.Equals(in _core, in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<VoidCoroutineResult> CallInternal<TClosure>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<VoidCoroutineResult>.RentFromCache();
        var argument = new CallArgument<TClosure>(provider, closure, providerFlags, completionSource);
        return new(completionSource, argument);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<TResult> CallInternal<TClosure, TResult>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags)
    {
        var completionSource = ManualResetCoroutineCompletionSource<TResult>.RentFromCache();
        var argument = new CallArgument<TClosure, TResult>(provider, closure, providerFlags, completionSource);
        return new(completionSource, argument);
    }

    public static Coroutine<VoidCoroutineResult> Call(Func<Coroutine> provider) => CallInternal<object?>(provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine<VoidCoroutineResult> Call<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure) => CallInternal(provider, closure, CoroutineProviderFlags.RequiresClosure);

    public static Coroutine<TResult> Call<TResult>(Func<Coroutine<TResult>> provider) => CallInternal<object?, TResult>(provider, closure: null, CoroutineProviderFlags.None);

    public static Coroutine<TResult> Call<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure) => CallInternal<TClosure, TResult>(provider, closure, CoroutineProviderFlags.RequiresClosure);

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct CallArgumentCore<TClosure, TResult>(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<TResult>? completionSource)
        {
            internal readonly ManualResetCoroutineCompletionSource<TResult>? _completionSource = completionSource;

            public readonly Delegate Provider = provider;
            public readonly TClosure Closure = closure;
            public readonly CoroutineProviderFlags ProviderFlags = providerFlags;

            public bool Equals(in CallArgumentCore<TClosure, TResult> other) =>
                ReferenceEquals(Provider, other.Provider)
                && Equals(Closure, other.Closure)
                && ProviderFlags == other.ProviderFlags;

            public override bool Equals([AllowNull] object obj) => throw new NotImplementedException();

            public override int GetHashCode() => HashCode.Combine(Provider, Closure, ProviderFlags);
        }

        public class CallArgument<TClosure> : ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>, ISiblingCoroutine
        {
            internal readonly CallArgumentCore<TClosure, VoidCoroutineResult> _core;

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

            internal CallArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource) =>
                _core = new CallArgumentCore<TClosure, VoidCoroutineResult>(provider, closure, providerFlags, completionSource);

            public CallArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new CallArgumentCore<TClosure, VoidCoroutineResult>(provider, closure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource)
            {
                Coroutine coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine>>(Provider)(Closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine>>(Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                var contextToBequest = default(CoroutineContext);
                contextToBequest.TreatAsNewSibling();
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, in contextToBequest);
                coroutineAwaiter.DelegateCoroutineCompletion(completionSource);
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in CallKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is CallArgument<TClosure> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }

        public class CallArgument<TClosure, TResult> : ICallableArgument<ManualResetCoroutineCompletionSource<TResult>>, ISiblingCoroutine
        {
            private readonly CallArgumentCore<TClosure, TResult> _core;

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
            internal CallArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags, ManualResetCoroutineCompletionSource<TResult> completionSource) =>
                _core = new CallArgumentCore<TClosure, TResult>(provider, closure, providerFlags, completionSource);

            public CallArgument(Delegate provider, TClosure closure, CoroutineProviderFlags providerFlags) =>
                _core = new CallArgumentCore<TClosure, TResult>(provider, closure, providerFlags, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<TResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<TResult> completionSource)
            {
                Coroutine<TResult> coroutine;
                if ((_core.ProviderFlags & CoroutineProviderFlags.RequiresClosure) != 0) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine<TResult>>>(Provider)(Closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine<TResult>>>(Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                var contextToBequest = default(CoroutineContext);
                contextToBequest.TreatAsNewSibling();
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, in contextToBequest);
                coroutineAwaiter.DelegateCoroutineCompletion(completionSource);
            }

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in CallKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is CallArgument<TClosure, TResult> argument && _core.Equals(in argument._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

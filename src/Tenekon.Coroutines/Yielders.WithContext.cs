﻿using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

file class CoroutineArgumentReceiverAcceptor<TClosure>(Delegate provider, TClosure closure, bool isProviderWithClosure, ManualResetCoroutineCompletionSource<Nothing> completionSource) : AbstractCoroutineArgumentReceiverAcceptor
{
    protected override void AcceptCoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver)
    {
        var argument = new WithContextArgument<TClosure>(provider, closure, isProviderWithClosure);
        argumentReceiver.ReceiveCallableArgument(in WithContextKey, in argument, completionSource);
    }
}

file class CoroutineArgumentReceiverAcceptor<TClosure, TResult>(Delegate provider, TClosure closure, bool isProviderWithClosure, ManualResetCoroutineCompletionSource<TResult> completionSource) : AbstractCoroutineArgumentReceiverAcceptor
{
    protected override void AcceptCoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver)
    {
        var argument = new WithContextArgument<TClosure, TResult>(provider, closure, isProviderWithClosure);
        argumentReceiver.ReceiveCallableArgument(in WithContextKey, in argument, completionSource);
    }
}

partial class Yielders
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine WithContextInternal<TClosure>(CoroutineContext additiveContext, Delegate provider, TClosure closure, bool isProviderWithClosure)
    {
        var completionSource = ManualResetCoroutineCompletionSource<Nothing>.RentFromCache();
        return new Coroutine(completionSource.CreateValueTask(), new CoroutineArgumentReceiverAcceptor<TClosure>(provider, closure, isProviderWithClosure, completionSource));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<TResult> WithContextInternal<TClosure, TResult>(CoroutineContext additiveContext, Delegate provider, TClosure closure, bool isProviderWithClosure)
    {
        var completionSource = ManualResetCoroutineCompletionSource<TResult>.RentFromCache();
        return new Coroutine<TResult>(completionSource.CreateGenericValueTask(), new CoroutineArgumentReceiverAcceptor<TClosure, TResult>(provider, closure, isProviderWithClosure, completionSource));
    }

    public static Coroutine WithContext(CoroutineContext additiveContext, Func<Coroutine> provider) => WithContextInternal<object?>(additiveContext, provider, closure: null, isProviderWithClosure: false);

    public static Coroutine WithContext<TClosure>(CoroutineContext additiveContext, Func<TClosure, Coroutine> provider, TClosure closure) => WithContextInternal(additiveContext, provider, closure, isProviderWithClosure: true);

    public static Coroutine<TResult> WithContext<TResult>(CoroutineContext additiveContext, Func<Coroutine<TResult>> provider) => WithContextInternal<object?, TResult>(additiveContext, provider, closure: null, isProviderWithClosure: false);

    public static Coroutine<TResult> WithContext<TClosure, TResult>(CoroutineContext additiveContext, Func<TClosure, Coroutine<TResult>> provider, TClosure closure) => WithContextInternal<TClosure, TResult>(additiveContext, provider, closure, isProviderWithClosure: true);

    partial class Arguments
    {
        public readonly struct WithContextArgument<TClosure>(Delegate provider, TClosure providerClosure, bool isProviderWithClosure) : ICallableArgument<ManualResetCoroutineCompletionSource<Nothing>>
        {
            public Delegate Provider {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => provider;
            }

            public TClosure ProviderClosure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => providerClosure;
            }

            public bool IsProviderWithClosure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => isProviderWithClosure;
            }

            readonly void ICallableArgument<ManualResetCoroutineCompletionSource<Nothing>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<Nothing> completionSource)
            {
                Coroutine
                    coroutine;
                if (IsProviderWithClosure) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine>>(Provider)(ProviderClosure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine>>(Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                var contextToBequest = default(CoroutineContext);
                contextToBequest.TreatAsNewSibling(additionalBequesterOrigin: CoroutineContextBequesterOrigin.ContextBequester);
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, in contextToBequest);
                coroutineAwaiter.DelegateCoroutineCompletion(completionSource);
            }
        }

        public readonly struct WithContextArgument<TClosure, TResult>(Delegate provider, TClosure providerClosure, bool isProviderWithClosure) : ICallableArgument<ManualResetCoroutineCompletionSource<TResult>>
        {
            public Delegate Provider {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => provider;
            }

            public TClosure ProviderClosure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => providerClosure;
            }

            public bool IsProviderWithClosure {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => isProviderWithClosure;
            }

            readonly void ICallableArgument<ManualResetCoroutineCompletionSource<TResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<TResult> completionSource)
            {
                Coroutine<TResult> coroutine;
                if (IsProviderWithClosure) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine<TResult>>>(Provider)(ProviderClosure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine<TResult>>>(Provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                var contextToBequest = default(CoroutineContext);
                contextToBequest.TreatAsNewSibling(additionalBequesterOrigin: CoroutineContextBequesterOrigin.ContextBequester);
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, in contextToBequest);
                coroutineAwaiter.DelegateCoroutineCompletion(completionSource);
            }
        }
    }
}

﻿using System.Runtime.CompilerServices;
using static Vernuntii.Coroutines.Yielders.Arguments;

namespace Vernuntii.Coroutines;

file class CoroutineargumentReceiverAcceptor<TClosure>(Delegate provider, TClosure closure, bool isProviderWithClosure, ManualResetCoroutineCompletionSource<Nothing> completionSource) : AbstractCoroutineArgumentReceiverAcceptor
{
    protected override void AcceptCoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver)
    {
        var argument = new CallArgument<TClosure>(provider, closure, isProviderWithClosure, completionSource);
        argumentReceiver.ReceiveCallableArgument(in CallKey, in argument, completionSource);
    }
}

file class CoroutineargumentReceiverAcceptor<TClosure, TResult>(Delegate provider, TClosure closure, bool isProviderWithClosure, ManualResetCoroutineCompletionSource<TResult> completionSource) : AbstractCoroutineArgumentReceiverAcceptor
{
    protected override void AcceptCoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver)
    {
        var argument = new CallArgument<TClosure, TResult>(provider, closure, isProviderWithClosure, completionSource);
        argumentReceiver.ReceiveCallableArgument(in CallKey, in argument, completionSource);
    }
}

partial class Yielders
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine CallInternal<TClosure>(Delegate provider, TClosure closure, bool isProviderWithClosure)
    {
        var completionSource = ManualResetCoroutineCompletionSource<Nothing>.RentFromCache();
        return new Coroutine(completionSource.CreateValueTask(), new CoroutineargumentReceiverAcceptor<TClosure>(provider, closure, isProviderWithClosure, completionSource));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Coroutine<TResult> CallInternal<TClosure, TResult>(Delegate provider, TClosure closure, bool isProviderWithClosure)
    {
        var completionSource = ManualResetCoroutineCompletionSource<TResult>.RentFromCache();
        return new Coroutine<TResult>(completionSource.CreateGenericValueTask(), new CoroutineargumentReceiverAcceptor<TClosure, TResult>(provider, closure, isProviderWithClosure, completionSource));
    }

    public static Coroutine Call(Func<Coroutine> provider) => CallInternal<object?>(provider, closure: null, isProviderWithClosure: false);

    public static Coroutine Call<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure) => CallInternal(provider, closure, isProviderWithClosure: true);

    public static Coroutine<TResult> Call<TResult>(Func<Coroutine<TResult>> provider) => CallInternal<object?, TResult>(provider, closure: null, isProviderWithClosure: false);

    public static Coroutine<TResult> Call<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure) => CallInternal<TClosure, TResult>(provider, closure, isProviderWithClosure: true);

    partial class Arguments
    {
        public readonly struct CallArgument<TClosure> : ICallableArgument
        {
            private readonly Delegate _provider;
            private readonly TClosure _closure;
            private readonly bool _isProviderWithClosure;
            private readonly ManualResetCoroutineCompletionSource<Nothing> _completionSource;

            public readonly Delegate Provider => _provider;
            public readonly TClosure Closure => _closure;

            internal CallArgument(
                Delegate provider,
                TClosure closure,
                bool isProviderWithClosure,
                ManualResetCoroutineCompletionSource<Nothing> completionSource)
            {
                _provider = provider;
                _closure = closure;
                _isProviderWithClosure = isProviderWithClosure;
                _completionSource = completionSource;
            }

            readonly void ICallableArgument.Callback(in CoroutineContext context)
            {
                Coroutine coroutine;
                if (_isProviderWithClosure) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine>>(_provider)(_closure!);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine>>(_provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                ref var contextToBequest = ref _completionSource._coroutineContext;
                contextToBequest.TreatAsNewSibling();
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, ref contextToBequest);
                coroutineAwaiter.DelegateCoroutineCompletion(_completionSource);
            }
        }

        public readonly struct CallArgument<TClosure, TResult> : ICallableArgument
        {
            private readonly Delegate _provider;
            private readonly TClosure _closure;
            private readonly bool _isProviderWithClosure;
            private readonly ManualResetCoroutineCompletionSource<TResult> _completionSource;

            public readonly Delegate Provider => _provider;
            public readonly TClosure Closure => _closure;

            internal CallArgument(
                Delegate provider,
                TClosure closure,
                bool isProviderWithClosure,
                ManualResetCoroutineCompletionSource<TResult> completionSource)
            {
                _provider = provider;
                _closure = closure;
                _isProviderWithClosure = isProviderWithClosure;
                _completionSource = completionSource;
            }

            readonly void ICallableArgument.Callback(in CoroutineContext context)
            {
                Coroutine<TResult> coroutine;
                if (_isProviderWithClosure) {
                    coroutine = Unsafe.As<Func<TClosure, Coroutine<TResult>>>(_provider)(_closure);
                } else {
                    coroutine = Unsafe.As<Func<Coroutine<TResult>>>(_provider)();
                }
                var coroutineAwaiter = coroutine.ConfigureAwait(false).GetAwaiter();

                ref var contextToBequest = ref _completionSource._coroutineContext;
                contextToBequest.TreatAsNewSibling();
                CoroutineContext.InheritOrBequestCoroutineContext(ref contextToBequest, in context);

                CoroutineMethodBuilderCore.ActOnCoroutine(ref coroutineAwaiter, ref contextToBequest);
                coroutineAwaiter.DelegateCoroutineCompletion(_completionSource);
            }
        }
    }
}

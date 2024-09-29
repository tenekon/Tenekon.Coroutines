﻿using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

file class CoroutineArgumentReceiverAcceptor<T>(T value, ManualResetCoroutineCompletionSource<T> completionSource) : AbstractCoroutineArgumentReceiverAcceptor
{
    protected override void AcceptCoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver)
    {
        var argument = new YieldReturnArgument<T>(value);
        argumentReceiver.ReceiveCallableArgument(in YieldReturnKey, in argument, completionSource);
    }
}

partial class Yielders
{
    public static Coroutine YieldReturn<T>(T value)
    {
        var completionSource = ManualResetCoroutineCompletionSource<T>.RentFromCache();
        return new Coroutine(completionSource.CreateValueTask(), new CoroutineArgumentReceiverAcceptor<T>(value, completionSource));
    }

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly struct YieldReturnArgument<T>(T value) : ICallableArgument<ManualResetCoroutineCompletionSource<T>>
        {
            public T Value {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => value;
            }

            void ICallableArgument<ManualResetCoroutineCompletionSource<T>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<T> completionSource) =>
                completionSource.SetResult(Value);
        }
    }
}

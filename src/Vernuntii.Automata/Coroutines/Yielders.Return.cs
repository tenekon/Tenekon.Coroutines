﻿namespace Vernuntii.Coroutines;

partial class Yielders
{
    public static Coroutine<TResult> Return<TResult>(TResult result)
    {
        var completionSource = ValueTaskCompletionSource<TResult>.RentFromCache();
        return new Coroutine<TResult>(completionSource.CreateGenericValueTask(), CoroutineArgumentReceiver);

        void CoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver)
        {
            var argument = new Arguments.ReturnArgument<TResult>(result, completionSource);
            argumentReceiver.ReceiveCallableArgument(in Arguments.s_returnArgumentType, in argument, completionSource);
        }
    }

    partial class Arguments
    {
        internal readonly struct ReturnArgument<TResult> : ICallableArgument
        {
            private readonly TResult _result;
            private readonly ValueTaskCompletionSource<TResult> _completionSource;

            public readonly TResult Result => _result;

            internal ReturnArgument(
                TResult result,
                ValueTaskCompletionSource<TResult> completionSource)
            {
                _result = result;
                _completionSource = completionSource;
            }

            void ICallableArgument.Callback(in CoroutineContext context) => _completionSource.SetResult(_result);
        }
    }
}

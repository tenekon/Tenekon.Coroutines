using static Tenekon.Coroutines.Iterators.Yielders.Arguments;

namespace Tenekon.Coroutines.Iterators;

file class CoroutineArgumentReceiverAcceptor<T>(T value, ManualResetCoroutineCompletionSource<T> completionSource) : AbstractCoroutineArgumentReceiverAcceptor
{
    protected override void AcceptCoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver)
    {
        var argument = new ExchangeArgument<T>(value);
        argumentReceiver.ReceiveCallableArgument(in ExchangeKey, in argument, completionSource);
    }
}

partial class Yielders
{
    public static Coroutine<T> Exchange<T>(T value)
    {
        var completionSource = ManualResetCoroutineCompletionSource<T>.RentFromCache();
        return new Coroutine<T>(completionSource.CreateGenericValueTask(), new CoroutineArgumentReceiverAcceptor<T>(value, completionSource));
    }

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly struct ExchangeArgument<T>(T value) : ICallableArgument<ManualResetCoroutineCompletionSource<T>>
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

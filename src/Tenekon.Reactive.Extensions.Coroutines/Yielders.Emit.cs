using Tenekon.Coroutines;
using Tenekon.Reactive.Broker;
using Tenekon.Reactive.Extensions.Coroutines;
using static Tenekon.Reactive.Extensions.Coroutines.Yielders.Arguments;

namespace Tenekon.Reactive.Extensions.Coroutines;

file class CoroutineArgumentReceiverAcceptor<T>(IEventDiscriminator<T> eventDiscriminator, T eventData, ManualResetCoroutineCompletionSource<Nothing> completionSource) : AbstractCoroutineArgumentReceiverAcceptor
{
    protected override void AcceptCoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver)
    {
        var argument = new EmitArgument<T>(eventDiscriminator, eventData);
        argumentReceiver.ReceiveCallableArgument(in EmitKey, in argument, completionSource);
    }
}

partial class Yielders
{
    public static Coroutine Emit<T>(IEventDiscriminator<T> eventDiscriminator, T eventData)
    {
        var completionSource = ManualResetCoroutineCompletionSource<Nothing>.RentFromCache();
        return new Coroutine(completionSource.CreateValueTask(), new CoroutineArgumentReceiverAcceptor<T>(eventDiscriminator, eventData, completionSource));
    }

    partial class Arguments
    {
        public readonly struct EmitArgument<T>(IEventDiscriminator<T> eventDiscriminator, T eventData) : ICallableArgument<ManualResetCoroutineCompletionSource<Nothing>>
        {
            public IEventDiscriminator<T> EventDiscriminator {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => eventDiscriminator;
            }

            public T EventData {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => eventData;
            }

            void ICallableArgument<ManualResetCoroutineCompletionSource<Nothing>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<Nothing> completionSource)
            {
                var eventBroker = context.GetBequestedEventBroker(ServiceKeys.EventBrokerKey);
                eventBroker.EmitAsync(EventDiscriminator, EventData).DelegateCompletion(completionSource);
            }
        }
    }
}

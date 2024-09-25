using Tenekon.Coroutines;
using Tenekon.Reactive.Broker;

namespace Tenekon.Reactive.Extensions.Coroutines;

partial class YieldersExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coroutine<EventChannel<T>> Channel<T>(this __co_ext _, Func<IReadOnlyEventBroker, IObservableEvent<T>> eventSelector) =>
        Yielders.Channel(eventSelector);
}

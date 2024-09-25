using Tenekon.Coroutines;
using Tenekon.Reactive.Broker;

namespace Tenekon.Reactive.Extensions.Coroutines;

internal static class CoroutineContextExtensions
{
    internal static IEventBroker GetBequestedEventBroker(this in CoroutineContext coroutineContext, Key serviceKey) =>
        serviceKey.SchemaVersion switch {
            1 => (IEventBroker)coroutineContext.KeyedServicesToBequest[serviceKey],
            _ => throw KeyThrowHelper.SchemaVersionNotSupported(serviceKey)
        };
}

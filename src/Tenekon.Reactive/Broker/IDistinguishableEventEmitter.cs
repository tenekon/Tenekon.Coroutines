namespace Tenekon.Reactive.Broker;

public interface IDistinguishableEventEmitter
{
    Task EmitAsync<T>(EventId eventId, T eventData);
}

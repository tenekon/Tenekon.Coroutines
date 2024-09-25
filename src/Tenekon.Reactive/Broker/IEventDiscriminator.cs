namespace Tenekon.Reactive.Broker;

public interface IEventDiscriminator<TPayload>
{
    /// <summary>
    /// The event id.
    /// </summary>
    EventId EventId { get; }
}

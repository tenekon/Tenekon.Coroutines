﻿namespace Tenekon.Reactive.Broker;

/// <summary>
/// Extension methods for <see cref="IReadOnlyEventBroker"/>.
/// </summary>
public static class EventChainabilityExtensions
{
    /// <summary>
    /// The event chains that are created with the help of this returning factory will inherit <paramref name="unsubscriptionRegistrar"/>.
    /// The event that build on an event chain inherits the registrar from said event chain.
    /// </summary>
    /// <param name="chainFactory"></param>
    /// <param name="unsubscriptionRegistrar">The bequeathable unsubscription registar. Specifiying <see langword="null"/> breaks the inheritance for new event chains.</param>
    /// <returns>
    /// A new instance with the capability to bequeath <paramref name="unsubscriptionRegistrar"/> to created event chains.
    /// </returns>
    public static IReadOnlyEventBroker UseUnsubscriptionRegistrar(this IReadOnlyEventBroker chainFactory, IDisposableRegistrar? unsubscriptionRegistrar) =>
        new AutoUnsubscribableEventChainFactory(chainFactory) { UnsubscriptionRegistrar = unsubscriptionRegistrar };

    internal static EventChain<T> Chain<T>(this IReadOnlyEventBroker chainFactory, IObservableEvent<T> observableEvent, IBacklogBackedEventObserver<T> eventObserver, EventId eventId) =>
        chainFactory.Chain(EventChainFragment.Create(observableEvent, eventObserver, eventId));

    internal static EventChain<T> Chain<T>(this IReadOnlyEventBroker chainFactory, IObservableEvent<T> observableEvent) =>
        chainFactory.Chain(EventChainFragment.Create(observableEvent));

    public static EventChain<T> Every<T>(this IReadOnlyEventBroker chainFactory, IEventDiscriminator<T> discriminator) =>
        EventChainabilities.Every(chainFactory, discriminator);

    public static EventChain<T> Latest<T>(this IReadOnlyEventBroker chainFactory, IEventDiscriminator<T> discriminator) =>
        EventChainabilities.Latest(chainFactory, discriminator);

    public static EventChain<T> Earliest<T>(this IReadOnlyEventBroker chainFactory, IEventDiscriminator<T> discriminator) =>
        EventChainabilities.Earliest(chainFactory, discriminator);

    public static EventChain<T> One<T>(this IReadOnlyEventBroker chainFactory, IEventDiscriminator<T> discriminator) =>
        EventChainabilities.One(chainFactory, discriminator);

    public static EventChain<(TWhenever, TResubscribe)> WheneverThenResubscribe<TWhenever, TResubscribe>(this IReadOnlyEventBroker chainFactory, EventChainTemplate<TWhenever> whenever, EventChainTemplate<TResubscribe> resubscribe) =>
        chainFactory.Chain(new WheneverThenResubscribeEvent<TWhenever, TResubscribe>(whenever.GetOrCreateChain(chainFactory), resubscribe.GetOrCreateChain(chainFactory)));
}

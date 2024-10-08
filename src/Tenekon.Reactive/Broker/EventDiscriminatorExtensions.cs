﻿namespace Tenekon.Reactive.Broker;

public static class EventDiscriminatorExtensions
{
    public static EventChainTemplate<T> Every<T>(this IEventDiscriminator<T> eventDiscriminator) =>
        new EventChainTemplate<T>(eventDiscriminator, EventChainabilityExtensions.Every);

    public static EventChainTemplate<T> Latest<T>(this IEventDiscriminator<T> eventDiscriminator) =>
        new EventChainTemplate<T>(eventDiscriminator, EventChainabilityExtensions.Latest);

    public static EventChainTemplate<T> Earliest<T>(this IEventDiscriminator<T> eventDiscriminator) =>
        new EventChainTemplate<T>(eventDiscriminator, EventChainabilityExtensions.Earliest);

    public static EventChainTemplate<T> One<T>(this IEventDiscriminator<T> eventDiscriminator) =>
        new EventChainTemplate<T>(eventDiscriminator, EventChainabilityExtensions.One);
}

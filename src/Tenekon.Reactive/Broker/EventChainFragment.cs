﻿using System.Diagnostics.CodeAnalysis;

namespace Tenekon.Reactive.Broker;

internal static class EventChainFragment
{
    public static EventChainFragment<T> Create<T>(IObservableEvent<T> observableEvent, IBacklogBackedEventObserver<T> eventObserver, EventId eventId) =>
        new(observableEvent, eventObserver, eventId);

    public static EventChainFragment<T> Create<T>(IObservableEvent<T> observableEvent) =>
        new(observableEvent);
}

internal record EventChainFragment<T>
{
    internal IObservableEvent<T> Event { get; }

    internal IBacklogBackedEventObserver<T>? EventObserver =>
        _eventObserver ?? throw new InvalidOperationException();

    internal EventId EventId { get; }

    /// <summary>
    /// If true, then all events of <see cref="EventObserver"/> can be bridged (e.g. to go over store)
    /// </summary>
    [MemberNotNullWhen(true,
        nameof(EventId),
        nameof(EventObserver))]
    internal bool IsEventAllowingBridging => EventId.IsInitialized;

    private readonly IBacklogBackedEventObserver<T>? _eventObserver;

    public EventChainFragment(IObservableEvent<T> observableEvent, IBacklogBackedEventObserver<T> eventObserver, EventId eventId)
    {
        Event = observableEvent;
        _eventObserver = eventObserver;
        EventId = eventId;
    }

    public EventChainFragment(IObservableEvent<T> observableEvent) =>
        Event = observableEvent;
}

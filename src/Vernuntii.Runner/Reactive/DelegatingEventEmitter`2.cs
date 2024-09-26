﻿namespace Vernuntii.Reactive;

internal class DelegatingEventEmitter<T, TState> : IEventEmitter<T>
{
    internal delegate Task HandleEventDelegate(in Tuple tuple);

    private readonly HandleEventDelegate _eventHandler;
    private readonly TState _state;

    public DelegatingEventEmitter(HandleEventDelegate eventHandler, TState state)
    {
        _eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
        _state = state;
    }

    public Task EmitAsync(T eventData) =>
        _eventHandler(new Tuple(eventData, _state));

    internal readonly struct Tuple
    {
        public T EventData { get; }
        public TState State { get; }

        internal Tuple(T eventData, TState state)
        {
            EventData = eventData;
            State = state;
        }
    }
}

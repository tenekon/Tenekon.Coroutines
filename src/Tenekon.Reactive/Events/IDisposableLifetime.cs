﻿namespace Tenekon.Reactive.Events;

public interface IDisposableLifetime : IDisposable
{
    bool IsDisposed { get; }
}

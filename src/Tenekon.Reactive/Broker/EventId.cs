using System.Diagnostics.CodeAnalysis;

namespace Tenekon.Reactive.Broker;

public readonly struct EventId(object identity)
{
    public object? Value { get; } = identity;

    [MemberNotNullWhen(true, nameof(Value))]
    internal bool IsInitialized => Value is not null;
}

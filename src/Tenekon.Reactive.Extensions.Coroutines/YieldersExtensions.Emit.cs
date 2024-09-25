using System.Runtime.CompilerServices;
using Tenekon;
using Tenekon.Coroutines;
using Tenekon.Reactive.Broker;

namespace Tenekon.Reactive.Extensions.Coroutines;

partial class YieldersExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coroutine Emit<T>(this __co_ext _, IEventDiscriminator<T> eventDiscriminator, T eventData) =>
        Yielders.Emit(eventDiscriminator, eventData);
}

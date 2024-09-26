using Tenekon.Coroutines;
using Tenekon.Reactive.Broker;
using Tenekon.Reactive.Extensions.Coroutines;
using static Tenekon.Reactive.Extensions.Coroutines.Yielders;

namespace PingPong;

internal class PingPongExample
{
    public static IEventDiscriminator<Pong> Pinged = EventDiscriminator.New<Pong>();
    public static IEventDiscriminator<Pong> Ponged = EventDiscriminator.New<Pong>();

    public async Task PingPongAsync()
    {
        var eventBroker = new EventBroker();

        var context = new CoroutineContext()
        {
            KeyedServicesToBequest = { { ServiceKeys.EventBrokerKey, eventBroker } }
        };

        var ponging = Coroutine.Start(PingWhenPonged, context);
        var pinging = Coroutine.Start(PongWhenPinged, context);
        await eventBroker.EmitAsync(Ponged, new Pong(1));
        await Task.WhenAll(ponging.AsTask(), pinging.AsTask());
    }

    public static async Coroutine PingWhenPonged()
    {
        using var pongedChannel = await Channel(broker => broker.Every(Ponged));

        while (true)
        {
            var ponged = await Take(pongedChannel);
            Console.WriteLine(ponged);
            await Emit(Pinged, new Pong(ponged.Counter)).ConfigureAwait(false);
        }
    }

    public static async Coroutine PongWhenPinged()
    {
        using var pingedChannel = await Channel(broker => broker.Every(Pinged));

        while (true)
        {
            var pinged = await Take(pingedChannel);
            await Emit(Ponged, new Pong(pinged.Counter + 1)).ConfigureAwait(false);
        }
    }

    public record Pong(int Counter);
    public record Ping(int Counter);
}

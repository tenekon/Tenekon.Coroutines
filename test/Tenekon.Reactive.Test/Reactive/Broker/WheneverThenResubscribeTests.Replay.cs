using Tenekon.Reactive.Events;

namespace Tenekon.Reactive.Broker;

public partial class WheneverThenResubscribeTests
{
    public static IEnumerable<TestCaseData> Emitting_whenever_then_replay_succeeds_generator()
    {
        yield return new(
            new (object, object)[] { (1, 1), (2, 2) },
            new (object, object)[] { (1, 1), (2, 1) }
        );
    }

    [TestCaseSource(nameof(Emitting_whenever_then_replay_succeeds_generator))]
    public async Task Emitting_whenever_then_replay_succeeds((object, object)[] inputValues, (object, object)[] expectedValues)
    {
        var eventBroker = new EventBroker();
        var whenevertDiscriminator = EventDiscriminator.New<object>();
        var resubscribeDiscriminator = EventDiscriminator.New<object>();
        var actualEventDatas = new List<(object, object)>();

        using var _ = eventBroker.WheneverThenResubscribe(whenevertDiscriminator.Every(), resubscribeDiscriminator.Earliest()).Subscribe(actualEventDatas.Add);

        foreach (var input in inputValues) {
            await eventBroker.EmitAsync(whenevertDiscriminator, input.Item1);
            await eventBroker.EmitAsync(resubscribeDiscriminator, input.Item2);
        }

        actualEventDatas.Should().Equal(expectedValues);
    }
}

namespace Tenekon.Coroutines;

partial class YieldersTests
{
    public class SpawnTests
    {
        [Test]
        public async Task AsyncSpawn_FlowsCorretly()
        {
            const int expectedResult = 1;
            var awaitableResult = await Coroutine.Start(() => Spawn(async () => expectedResult));
            var result = await awaitableResult;
            result.Should().Be(expectedResult);
        }

        [Test]
        public async Task AsyncCallAwaitingAsyncSpawn_FlowsCorrectly()
        {
            int[] expectedRecords = [1];
            var records = new List<int>();

            await Coroutine.Start(() => Call(async () => {
                await Spawn(async () => {
                    await Task.Delay(CancellationTimeInMs);
                    records.Add(2);
                });

                records.Add(1);
            }));

            records.Should().Equal(expectedRecords);
        }

        [Test]
        public async Task AsyncCallReturningAsyncSpawn_FlowsCorretly()
        {
            int[] expectedRecords = [1];
            var records = new List<int>();

            await Coroutine.Start(() => Call(() => {
                var spawn = Spawn(async () => {
                    await Task.Delay(CancellationTimeInMs);
                    records.Add(2);
                });

                records.Add(1);
                return spawn;
            }));

            records.Should().Equal(expectedRecords);
        }
    }
}

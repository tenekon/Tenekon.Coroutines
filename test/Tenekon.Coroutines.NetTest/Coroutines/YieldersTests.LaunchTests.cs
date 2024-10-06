namespace Tenekon.Coroutines;

partial class YieldersTests
{
    /* FYI: Given `Coroutine.Start(() => Call(..))`, then the Calls's coroutine of Start's provider waits for coroutine of Call's provider. */
    public class LaunchTests
    {
        [Fact]
        public async Task AsyncLaunch_FlowsCorretly()
        {
            const int expectedResult = 1;
            var awaitableResult = await Coroutine.Start(() => Launch(async () => expectedResult));
            var result = await awaitableResult;
            result.Should().Be(expectedResult);
        }

        [UIFact]
        public async Task AsyncCallAwaitingAsyncLaunch_FlowsCorrectly()
        {
            int[] expectedRecords = [1, 2];
            var records = new List<int>();

            var asyncResult = Coroutine.Start(() => Call(async () => {
                var launch = await Launch(async () => {
                    await Task.Delay(ContinueAfterTimeInMs);
                    records.Add(2);
                });

                records.Add(1);
            }));

            await asyncResult;

            records.Should().Equal(expectedRecords);
        }

        [Fact]
        public async Task AsyncCallReturningAsyncLaunch_FlowsCorretly()
        {
            int[] expectedRecords = [1, 2];
            var records = new List<int>();

            await Coroutine.Start(() => Call(() => {
                var launch = Launch(async () => {
                    records.Add(2);
                });

                records.Add(1);
                return launch;
            }));

            records.Should().Equal(expectedRecords);
        }
    }
}

namespace Tenekon.Coroutines;

partial class YieldersTests
{
    public class CallTests
    {
        const int ExpectedResult = 1;

        public static IEnumerable<TestCaseData> AsyncCallReturningConstant_RunsThrough_Generator()
        {
            yield return new(Coroutine.Start(() => Call(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            }))._coroutine);
            yield return new(Call(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            }));
        }

        [TestCaseSource(nameof(AsyncCallReturningConstant_RunsThrough_Generator))]
        public async Task AsyncCallReturningConstant_RunsThrough(Coroutine<int> coroutine)
        {
            var result = await coroutine;
            result.Should().Be(ExpectedResult);
        }

        [Test]
        public async Task AwaitingAsyncCallReturningConstant_Suceeds()
        {
            var result = await Coroutine.Start(async () => await Call(async () => ExpectedResult));
            result.Should().Be(ExpectedResult);
        }

        [Test]
        public async Task AwaitingAsyncCallWithClosure_Suceeds()
        {
            var result = await Coroutine.Start(async () => await Call(async (expectedResult) => expectedResult, ExpectedResult));
            result.Should().Be(ExpectedResult);
        }
    }
}

namespace Tenekon.Coroutines;

partial class YieldersTests
{
    public class CallTests
    {
        const int ExpectedResult = 1;

        public static IEnumerable<object[]> AsyncCallReturningConstant_RunsThrough_Generator()
        {
            yield return [Coroutine.Start(() => Call(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            }))._coroutine];
            yield return [Call(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            })];
        }

        [Theory]
        [MemberData(nameof(AsyncCallReturningConstant_RunsThrough_Generator))]
        public async Task AsyncCallReturningConstant_RunsThrough(Coroutine<int> coroutine)
        {
            var result = await coroutine;
            Assert.Equal(ExpectedResult, result);
        }

        [Fact]
        public async Task AwaitingAsyncCallReturningConstant_Suceeds()
        {
            var result = await Coroutine.Start(async () => await Call(async () => ExpectedResult));
            Assert.Equal(ExpectedResult, result);
        }

        [Fact]
        public async Task AwaitingAsyncCallWithClosure_Suceeds()
        {
            var result = await Coroutine.Start(async () => await Call(async (expectedResult) => expectedResult, ExpectedResult));
            Assert.Equal(ExpectedResult, result);
        }
    }
}

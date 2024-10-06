namespace Tenekon.Coroutines.Iterators;
partial class AsyncIteratorTests
{
    public class WithContext
    {
        static CoroutineContext _defaultContext = new();

        public class SyncCoroutineWithSyncResult() : AbstractSyncCoroutineWithSyncResult<int, int>(One)
        {
            protected override Coroutine<int> CreateCoroutine() => WithContext(_defaultContext, () => new Coroutine<int>(ExpectedResult));
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }

        public class SyncCoroutineWithAsyncResult() : AbstractSyncCoroutineWithAsyncResult<int, int>(One)
        {
            protected override Coroutine<int> CreateCoroutine() => WithContext(_defaultContext, async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> x) => new(x);
        }

        public class AsyncCoroutineWithSyncResult() : AbstractAsyncCoroutineWithSyncResult<int, int>(expectedResult: One, expectedYieldResult: Two)
        {
            protected override Coroutine<int> CreateCoroutine() => WithContext(default, async () => await Exchange(ExpectedResult));
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }
    }
}

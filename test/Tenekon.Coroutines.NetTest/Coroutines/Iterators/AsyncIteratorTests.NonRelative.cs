namespace Tenekon.Coroutines.Iterators;

partial class AsyncIteratorTests
{
    public class NonRelative
    {
        public class SyncCoroutineWithSyncResult : AbstractSyncCoroutineWithSyncResult<int, int>
        {
            public override int ExpectedResult => One;

            protected override Coroutine<int> CreateCoroutine() => Coroutine.FromResult(ExpectedResult);
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }

        public class SyncCoroutineWithAsyncResult : AbstractSyncCoroutineWithAsyncResult<int, int>
        {
            public override int ExpectedResult => One;

            protected override async Coroutine<int> CreateCoroutine()
            {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            }

            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> x) => new(x);
        }
    }
}

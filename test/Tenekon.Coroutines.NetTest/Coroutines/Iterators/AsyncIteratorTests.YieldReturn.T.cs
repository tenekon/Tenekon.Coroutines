namespace Tenekon.Coroutines.Iterators;

partial class AsyncIteratorTests
{
    public class YieldReturnVariant
    {
        public class SyncCoroutineWithSyncResult() : AbstractSyncCoroutineWithSyncResult<int, int>(expectedResult: 0)
        {
            protected override Coroutine<int> CreateCoroutine() => Yield(int.MaxValue).Return<int>();
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }

        public class AsyncCoroutineWithSyncResult() : AbstractAsyncCoroutineWithSyncResult<int, int>(expectedResult: 0, expectedYieldResult: Two)
        {
            protected override async Coroutine<int> CreateCoroutine() => await Yield(int.MaxValue).Return<int>();
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }
    }
}

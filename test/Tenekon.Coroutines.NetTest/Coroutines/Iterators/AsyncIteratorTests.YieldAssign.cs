namespace Tenekon.Coroutines.Iterators;

partial class AsyncIteratorTests
{
    public class YieldAssign
    {
        public class SyncCoroutineWithSyncResult : AbstractSyncCoroutineWithSyncResult<int, int>
        {
            public override int ExpectedResult => 0;

            protected override Coroutine<int> CreateCoroutine() => Yield(int.MaxValue).Assign<int>();
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }

        public class AsyncCoroutineWithSyncResult : AbstractAsyncCoroutineWithSyncResult<int, int>
        {
            public override int ExpectedResult => 0;
            public override int ExpectedYieldResult => Two;
            public override int ExpectedYieldResultOfClone => 0;

            protected override async Coroutine<int> CreateCoroutine() => await Yield(int.MaxValue).Assign<int>();
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }
    }
}

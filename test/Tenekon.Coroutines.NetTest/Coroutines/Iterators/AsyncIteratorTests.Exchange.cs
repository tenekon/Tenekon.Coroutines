﻿namespace Tenekon.Coroutines.Iterators;

partial class AsyncIteratorTests
{
    public class Exchange
    {
        public class SyncCoroutineWithSyncResult() : AbstractSyncCoroutineWithSyncResult<int, int>(One)
        {
            protected override Coroutine<int> CreateCoroutine() => Exchange(ExpectedResult);
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }

        public class AsyncCoroutineWithSyncResult() : AbstractAsyncCoroutineWithSyncResult<int, int>(expectedResult: One, expectedYieldResult: Two)
        {
            protected override async Coroutine<int> CreateCoroutine() => await Exchange(ExpectedResult);
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }
    }
}

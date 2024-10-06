﻿using Tenekon.Coroutines.CompilerServices;

namespace Tenekon.Coroutines.Iterators;
partial class AsyncIteratorTests
{
    public class StartNew
    {
        public class SyncCoroutineWithSyncResult() : AbstractSyncCoroutineWithSyncResult<CoroutineAwaitable<int>, int>(One)
        {
            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Coroutine.Factory.StartNew(() => Coroutine.FromResult(ExpectedResult));
            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> coroutine) => await coroutine;
        }

        public class SyncCoroutineWithCoroutineWrappedResult() : AbstractSyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>(One)
        {
            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Coroutine.Factory.StartNew(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;
        }

        public class AsyncCoroutineWithCoroutineWrappedResult() : AbstractAsyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>(One, new(new(Two)))
        {
            protected override async Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => await Coroutine.Factory.StartNew(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;
        }
    }
}

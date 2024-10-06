using Tenekon.Coroutines.CompilerServices;

namespace Tenekon.Coroutines.Iterators;
partial class AsyncIteratorTests
{
    public class Run
    {
        public class SyncCoroutineWithSyncResult() : AbstractSyncCoroutineWithSyncResult<CoroutineAwaitable<int>, int>(One)
        {
            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Coroutine.Run(() => Coroutine.FromResult(ExpectedResult));
            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> coroutine) => await coroutine;
        }

        public class SyncCoroutineWithAsyncResult() : AbstractSyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>(One)
        {
            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Coroutine.Run(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;

            [Fact]
            public override Task GetResult_Returns() => base.GetResult_Returns();
        }

        public class AsyncCoroutineWithAsyncResult() : AbstractAsyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>(One, Two)
        {
            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Coroutine.Run(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;

            [Fact]
            public override Task GetResult_Returns() => base.GetResult_Returns();
        }
    }
}

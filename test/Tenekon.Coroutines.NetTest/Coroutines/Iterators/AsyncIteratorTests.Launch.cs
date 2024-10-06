using Tenekon.Coroutines.CompilerServices;

namespace Tenekon.Coroutines.Iterators;
partial class AsyncIteratorTests
{
    public class Launch
    {
        public class SyncCoroutineWithSyncResult : AbstractSyncCoroutineWithSyncResult<CoroutineAwaitable<int>, int>
        {
            public override int ExpectedResult => One;

            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Launch(() => Coroutine.FromResult(ExpectedResult));
            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> coroutine) => await coroutine;
        }

        public class SyncCoroutineWithCoroutineWrappedResult : AbstractSyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>
        {
            public override int ExpectedResult => One;

            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Launch(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;
        }

        public class AsyncCoroutineWithCoroutineWrappedResult : AbstractAsyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>
        {
            public override int ExpectedResult => One;
            public override CoroutineAwaitable<int> ExpectedYieldResult => new(new(Two));
            public override CoroutineAwaitable<int> ExpectedYieldResultOfClone => new(new(Two));

            protected async override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => await Launch(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;
        }
    }
}

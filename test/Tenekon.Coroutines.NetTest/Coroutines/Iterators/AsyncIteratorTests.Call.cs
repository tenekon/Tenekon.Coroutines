namespace Tenekon.Coroutines.Iterators;

partial class AsyncIteratorTests
{
    public class Call
    {
        public static IEnumerable<TestCaseData> NonGeneric_IsSiblingCoroutine_Generator()
        {
            yield return new(new ReleativeCoroutineHolder(Call(async () => { })));
            yield return new(new ReleativeCoroutineHolder(Call(() => Call(async () => { }))));
        }

        [TestCaseSource(nameof(NonGeneric_IsSiblingCoroutine_Generator))]
        public void NonGeneric_IsSiblingCoroutine(ReleativeCoroutineHolder holder)
        {
            holder.Coroutine.CoroutineAction.Should().Be(CoroutineAction.Sibling);
        }

        public class SyncCoroutineWithSyncResult() : AbstractSyncCoroutineWithSyncResult<int, int>(One)
        {
            protected override Coroutine<int> CreateCoroutine() => Call(() => new Coroutine<int>(ExpectedResult));
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> coroutine) => new(coroutine);
        }

        public class SyncCoroutineWithAsyncResult() : AbstractSyncCoroutineWithAsyncResult<int, int>(One)
        {
            protected override Coroutine<int> CreateCoroutine() => Call(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> x) => ValueTask.FromResult(x);
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
        }

        public class AsyncCoroutineWithCoroutineWrappedResult() : AbstractAsyncCoroutineWithCoroutineWrappedResult<int, int>(One, Two)
        {
            protected override async Coroutine<int> CreateCoroutine() => await Call(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<Coroutine<int>> Unwrap(Coroutine<int> x) => ValueTask.FromResult(x);
            protected override ValueTask<int> Unwrap(int resultWrapper) => new(resultWrapper);
        }
    }
}

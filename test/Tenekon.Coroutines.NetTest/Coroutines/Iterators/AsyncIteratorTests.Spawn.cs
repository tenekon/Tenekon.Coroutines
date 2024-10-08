﻿using Tenekon.Coroutines.CompilerServices;

namespace Tenekon.Coroutines.Iterators;
partial class AsyncIteratorTests
{
    public class Spawn
    {
        public static IEnumerable<TestCaseData> NonGeneric_IsSibling_Generator()
        {
            yield return new(new ReleativeCoroutineHolder(Spawn(async () => { })));
            yield return new(new ReleativeCoroutineHolder(Spawn(() => Call(async () => { }))));
        }

        [Theory]
        [TestCaseSource(nameof(NonGeneric_IsSibling_Generator))]
        public void NonGeneric_IsSibling(ReleativeCoroutineHolder holder)
        {
            holder.Coroutine.CoroutineAction.Should().Be(CoroutineAction.Sibling);
        }

        public class SyncCoroutineWithSyncResult : AbstractSyncCoroutineWithSyncResult<CoroutineAwaitable<int>, int>
        {
            public override int ExpectedResult => One;

            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Spawn(() => Coroutine.FromResult(ExpectedResult));
            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> coroutine) => await coroutine;
        }

        public class SyncCoroutineWithCoroutineWrappedResult : AbstractSyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>
        {
            public override int ExpectedResult => One;

            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Spawn(async () => {
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

            protected override async Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => await Spawn(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;
        }
    }
}

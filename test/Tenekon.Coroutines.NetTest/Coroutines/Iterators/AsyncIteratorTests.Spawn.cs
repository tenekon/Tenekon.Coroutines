﻿using Tenekon.Coroutines.CompilerServices;

namespace Tenekon.Coroutines.Iterators;
partial class AsyncIteratorTests
{
    public class Spawn
    {
        public static IEnumerable<object[]> NonGeneric_IsSibling_Generator()
        {
            yield return [new ReleativeCoroutineHolder(Spawn(async () => { }))];
            yield return [new ReleativeCoroutineHolder(Spawn(() => Call(async () => { })))];
        }

        [Theory]
        [MemberData(nameof(NonGeneric_IsSibling_Generator))]
        public void NonGeneric_IsSibling(ReleativeCoroutineHolder holder)
        {
            holder.Coroutine.CoroutineAction.Should().Be(CoroutineAction.Sibling);
        }

        public class SyncCoroutineWithSyncResult() : AbstractSyncCoroutineWithSyncResult<CoroutineAwaitable<int>, int>(One)
        {
            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Spawn(() => Coroutine.FromResult(ExpectedResult));
            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> coroutine) => await coroutine;
        }

        public class SyncCoroutineWithAsyncResult() : AbstractSyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>(One)
        {
            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Spawn(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;
        }

        public class AsyncCoroutineWithAsyncResult() : AbstractAsyncCoroutineWithCoroutineWrappedResult<CoroutineAwaitable<int>, int>(One, Two)
        {
            protected override Coroutine<CoroutineAwaitable<int>> CreateCoroutine() => Spawn(async () => {
                await Task.Delay(ContinueAfterTimeInMs);
                return ExpectedResult;
            });

            protected override ValueTask<int> Unwrap(CoroutineAwaitable<int> resultWrapper) => resultWrapper;
            protected override async ValueTask<Coroutine<int>> Unwrap(Coroutine<CoroutineAwaitable<int>> x) => await x;
        }
    }
}

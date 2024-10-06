﻿namespace Tenekon.Coroutines.Iterators;

public partial class AsyncIteratorTests
{
    internal const int One = 1;
    internal const int Two = One + 1;

    [Test]
    public async Task Clone_DoesNotConsumesOriginalIterator()
    {
        const int OurResult = 1;
        const int TheirResult = 2;
        var our = AsyncIterator.Create(provider: new Func<Coroutine<int>>(async () => {
            var one = await Exchange(OurResult);
            return one;
        }), isCloneable: true);
        _ = await our.MoveNextAsync();
        var their = our.Clone();
        _ = await their.MoveNextAsync();
        their.YieldReturn(TheirResult);
        var ourResult = await our.GetResultAsync();
        var theirResult = await their.GetResultAsync();
        ourResult.Should().Be(OurResult);
        theirResult.Should().Be(TheirResult);
    }

    public abstract class AbstractCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult)
    {
        public TUnwrappedResult ExpectedResult { get; } = expectedResult;

        protected abstract Coroutine<TCoroutineResult> CreateCoroutine();
        protected abstract ValueTask<TUnwrappedResult> Unwrap(TCoroutineResult resultWrapper);
        protected abstract ValueTask<Coroutine<TUnwrappedResult>> Unwrap(Coroutine<TCoroutineResult> coroutine);

        public virtual async Task MoveNext_ReturnsFalse()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            var canMoveNext = await iterator.MoveNextAsync();
            canMoveNext.Should().BeFalse();
        }

        public virtual async Task MoveNext_ReturnsTrue()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            var canMoveNext = await iterator.MoveNextAsync();
            canMoveNext.Should().BeTrue();
        }

        public virtual async Task MoveNextThenMoveNext_ReturnsFalse()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            _ = await iterator.MoveNextAsync();
            var canMoveNext = await iterator.MoveNextAsync();
            canMoveNext.Should().BeFalse();
        }

        public virtual void GetResult_Throws()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();

            var result = iterator
                .Invoking(x => x.GetResult())
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*not finished yet*");
        }

        public virtual async Task GetResult_Returns()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            var asyncResult = Unwrap(iterator.GetResult());
            var result = await asyncResult;
            result.Should().Be(ExpectedResult);
        }

        public virtual async Task GetResultAsync_Awaits()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            var asyncResult = await Unwrap(iterator.GetResultAsync());
            var result = await asyncResult;
            result.Should().Be(ExpectedResult);
        }

        public virtual void Throw_Fails()
        {
            var iterator = AsyncIterator.Create(CreateCoroutine);
            iterator
                .Invoking(x => x.Throw(new Exception1()))
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("*not started*already finished*not suspended*");
        }
    }

    public abstract class AbstractAsyncCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult, TCoroutineResult expectedYieldResult)
        : AbstractCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult)
    {
        public TCoroutineResult ExpectedYieldResult { get; } = expectedYieldResult;


        public virtual async Task MoveNextThenYieldReturnThenGetResult_Returns()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            _ = await iterator.MoveNextAsync();
            iterator.YieldReturn(ExpectedYieldResult);
            var asyncResult = await iterator.GetResultAsync();
            var result = await Unwrap(asyncResult);
            result.Should().Be(await Unwrap(ExpectedYieldResult));
        }

        public virtual async Task MoveNextThenMoveNextThenGetResult_Returns()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            _ = await iterator.MoveNextAsync();
            _ = await iterator.MoveNextAsync();
            var asyncResult = iterator.GetResult();
            var result = await Unwrap(asyncResult);
            result.Should().Be(ExpectedResult);
        }

        public virtual async Task MoveNextThenGetResultAsync_Awaits()
        {
            var iterator = AsyncIterator.Create(CreateCoroutine());
            _ = await iterator.MoveNextAsync();
            var asyncResult = await iterator.GetResultAsync();
            var result = await Unwrap(asyncResult);
            result.Should().Be(ExpectedResult);
        }

        public virtual async Task MoveNextThenThrow_Succeeds()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            _ = await iterator.MoveNextAsync();
            iterator.Throw(new Exception1());
            await iterator.Awaiting(new Func<IAsyncIterator<TCoroutineResult>, Task>(async x => await x.GetResultAsync()))
                .Should()
                .ThrowExactlyAsync<Exception1>();
        }
    }

    public abstract class AbstractSyncCoroutineWithSyncResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult)
        : AbstractCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult)
    {
        [Test]
        public override Task MoveNext_ReturnsFalse() => base.MoveNext_ReturnsFalse();

        [Test]
        public override Task GetResult_Returns() => base.GetResult_Returns();

        [Test]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Test]
        public override void Throw_Fails() => base.Throw_Fails();
    }

    public abstract class AbstractSyncCoroutineWithAsyncResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult)
        : AbstractCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult)
    {
        [Test]
        public override void GetResult_Throws() => base.GetResult_Throws();

        [Test]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Test]
        public override Task MoveNext_ReturnsFalse() => base.MoveNext_ReturnsFalse();
    }

    public abstract class AbstractSyncCoroutineWithCoroutineWrappedResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult)
        : AbstractCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult)
    {
        [Test]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Test]
        public override Task GetResult_Returns() => base.GetResult_Returns();

        [Test]
        public override Task MoveNext_ReturnsFalse() => base.MoveNext_ReturnsFalse();
    }

    public abstract class AbstractAsyncCoroutineWithSyncResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult, TCoroutineResult expectedYieldResult)
        : AbstractAsyncCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult, expectedYieldResult)
    {
        [Test]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Test]
        public override void GetResult_Throws() => base.GetResult_Throws();

        [Test]
        public override Task MoveNext_ReturnsTrue() => base.MoveNext_ReturnsTrue();

        [Test]
        public override Task MoveNextThenMoveNext_ReturnsFalse() => base.MoveNextThenMoveNext_ReturnsFalse();

        [Test]
        public override void Throw_Fails() => base.Throw_Fails();

        [Test]
        public override Task MoveNextThenYieldReturnThenGetResult_Returns() => base.MoveNextThenYieldReturnThenGetResult_Returns();

        [Test]
        public override Task MoveNextThenMoveNextThenGetResult_Returns() => base.MoveNextThenMoveNextThenGetResult_Returns();

        [Test]
        public override Task MoveNextThenGetResultAsync_Awaits() => base.MoveNextThenGetResultAsync_Awaits();

        [Test]
        public override Task MoveNextThenThrow_Succeeds() => base.MoveNextThenThrow_Succeeds();
    }

    public abstract class AbstractAsyncCoroutineWithCoroutineWrappedResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult, TCoroutineResult expectedYieldResult)
        : AbstractAsyncCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult, expectedYieldResult)
    {
        [Test]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Test]
        public override void GetResult_Throws() => base.GetResult_Throws();

        [Test]
        public override Task MoveNext_ReturnsTrue() => base.MoveNext_ReturnsTrue();

        [Test]
        public override Task MoveNextThenMoveNext_ReturnsFalse() => base.MoveNextThenMoveNext_ReturnsFalse();

        [Test]
        public override void Throw_Fails() => base.Throw_Fails();

        [Test]
        public override Task MoveNextThenYieldReturnThenGetResult_Returns() => base.MoveNextThenYieldReturnThenGetResult_Returns();

        [Test]
        public override Task MoveNextThenMoveNextThenGetResult_Returns() => base.MoveNextThenMoveNextThenGetResult_Returns();

        [Test]
        public override Task MoveNextThenGetResultAsync_Awaits() => base.MoveNextThenGetResultAsync_Awaits();

        [Test]
        public override Task MoveNextThenThrow_Succeeds() => base.MoveNextThenThrow_Succeeds();
    }
}

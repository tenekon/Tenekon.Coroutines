namespace Tenekon.Coroutines.Iterators;

public partial class AsyncIteratorTests
{
    internal const int One = 1;
    internal const int Two = One + 1;

    [Fact]
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

    public abstract class AbstractSyncCoroutineWithSyncResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult)
    {
        public TUnwrappedResult ExpectedResult { get; } = expectedResult;

        protected abstract Coroutine<TCoroutineResult> CreateCoroutine();
        protected abstract ValueTask<TUnwrappedResult> Unwrap(TCoroutineResult resultWrapper);
        protected abstract ValueTask<Coroutine<TUnwrappedResult>> Unwrap(Coroutine<TCoroutineResult> coroutine);

        [Fact]
        public virtual async Task MoveNext_ReturnsFalse()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            var canMoveNext = await iterator.MoveNextAsync();
            canMoveNext.Should().BeFalse();
        }

        [Fact]
        public virtual async Task GetResult_Returns()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            var result = await Unwrap(iterator.GetResult());
            result.Should().Be(ExpectedResult);
        }

        [Fact]
        public virtual async Task GetResultAsync_Awaits()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            var asyncResult = await Unwrap(iterator.GetResultAsync());
            var result = await asyncResult;
            result.Should().Be(ExpectedResult);
        }

        [Fact]
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
    }

    public abstract class AbstractAsyncCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult, TUnwrappedResult expectedYieldResult)
        : AbstractCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult)
    {
        public TUnwrappedResult ExpectedYieldResult { get; } = expectedYieldResult;

        public virtual void Throw_Fails()
        {
            var iterator = AsyncIterator.Create(CreateCoroutine);
            iterator
                .Invoking(x => x.Throw(new Exception1()))
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("*not started*already finished*not suspended*");
        }


        public virtual async Task MoveNextThenYieldReturnThenGetResult_Returns()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            _ = await iterator.MoveNextAsync();
            iterator.YieldReturn(ExpectedYieldResult);
            var asyncResult = iterator.GetResultAsync();
            var result = await asyncResult;
            result.Should().Be(ExpectedYieldResult);
        }

        public virtual async Task MoveNextThenMoveNextThenGetResult_Returns()
        {
            var iterator = CreateCoroutine().GetAsyncIterator();
            _ = await iterator.MoveNextAsync();
            _ = await iterator.MoveNextAsync();
            var result = iterator.GetResult();
            result.Should().Be(ExpectedResult);
        }

        public virtual async Task MoveNextThenGetResultAsync_Awaits()
        {
            var iterator = AsyncIterator.Create(CreateCoroutine());
            _ = await iterator.MoveNextAsync();
            var result = await iterator.GetResultAsync();
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

    public abstract class AbstractSyncCoroutineWithAsyncResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult)
        : AbstractCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult)
    {
        [Fact]
        public override void GetResult_Throws() => base.GetResult_Throws();

        [Fact]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Fact]
        public override Task MoveNext_ReturnsFalse() => base.MoveNext_ReturnsFalse();
    }

    public abstract class AbstractSyncCoroutineWithCoroutineWrappedResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult)
        : AbstractCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult)
    {
        [Fact]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Fact]
        public override Task GetResult_Returns() => base.GetResult_Returns();

        [Fact]
        public override Task MoveNext_ReturnsFalse() => base.MoveNext_ReturnsFalse();
    }

    public abstract class AbstractAsyncCoroutineWithSyncResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult, TUnwrappedResult expectedYieldResult)
        : AbstractAsyncCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult, expectedYieldResult)
    {
        [Fact]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Fact]
        public override void GetResult_Throws() => base.GetResult_Throws();

        [Fact]
        public override Task MoveNext_ReturnsTrue() => base.MoveNext_ReturnsTrue();

        [Fact]
        public override void Throw_Fails() => base.Throw_Fails();

        [Fact]
        public override Task MoveNextThenYieldReturnThenGetResult_Returns() => base.MoveNextThenYieldReturnThenGetResult_Returns();

        [Fact]
        public override Task MoveNextThenMoveNextThenGetResult_Returns() => base.MoveNextThenMoveNextThenGetResult_Returns();

        [Fact]
        public override Task MoveNextThenGetResultAsync_Awaits() => base.MoveNextThenGetResultAsync_Awaits();

        [Fact]
        public override Task MoveNextThenThrow_Succeeds() => base.MoveNextThenThrow_Succeeds();
    }

    public abstract class AbstractAsyncCoroutineWithCoroutineWrappedResult<TCoroutineResult, TUnwrappedResult>(TUnwrappedResult expectedResult, TUnwrappedResult expectedYieldResult)
        : AbstractAsyncCoroutineWithResultBase<TCoroutineResult, TUnwrappedResult>(expectedResult, expectedYieldResult)
    {
        [Fact]
        public override Task GetResultAsync_Awaits() => base.GetResultAsync_Awaits();

        [Fact]
        public override void GetResult_Throws() => base.GetResult_Throws();

        [Fact]
        public override Task MoveNext_ReturnsTrue() => base.MoveNext_ReturnsTrue();

        [Fact]
        public override void Throw_Fails() => base.Throw_Fails();

        [Fact]
        public override Task MoveNextThenYieldReturnThenGetResult_Returns() => base.MoveNextThenYieldReturnThenGetResult_Returns();

        [Fact]
        public override Task MoveNextThenMoveNextThenGetResult_Returns() => base.MoveNextThenMoveNextThenGetResult_Returns();

        [Fact]
        public override Task MoveNextThenGetResultAsync_Awaits() => base.MoveNextThenGetResultAsync_Awaits();

        [Fact]
        public override Task MoveNextThenThrow_Succeeds() => base.MoveNextThenThrow_Succeeds();
    }

    public abstract class AbstractYieldReturnSynchronously
    {
        protected const int ExpectedResult = 2;

        protected abstract Coroutine<int> CreateCoroutineReturningConstant();

        [Fact]
        public async Task MoveNext_ReturnsTrue()
        {
            var iterator = CreateCoroutineReturningConstant().GetAsyncIterator();
            var canMoveNext = await iterator.MoveNextAsync();
            canMoveNext.Should().BeTrue();
        }

        [Fact]
        public async Task MoveNextThenMoveNext_ReturnsFalse()
        {
            var iterator = CreateCoroutineReturningConstant().GetAsyncIterator();
            _ = await iterator.MoveNextAsync();
            var canMoveNext = await iterator.MoveNextAsync();
            canMoveNext.Should().BeFalse();
        }

        [Fact]
        public void GetResult_Throws()
        {
            var iterator = CreateCoroutineReturningConstant().GetAsyncIterator();

            var result = iterator
                .Invoking(x => x.GetResult())
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*not finished yet*");
        }

        [Fact]
        public async Task GetResultAsync_Awaits()
        {
            var iterator = AsyncIterator.Create(CreateCoroutineReturningConstant());
            var result = await iterator.GetResultAsync();
            result.Should().Be(ExpectedResult);
        }

        //[Fact]
        //public void Throw_Fails()
        //{
        //    var iterator = AsyncIterator.Create(CreateCoroutineReturningConstant);
        //    iterator
        //        .Invoking(x => x.Throw(new Exception1()))
        //        .Should()
        //        .ThrowExactly<InvalidOperationException>()
        //        .WithMessage("*not started*already finished*not suspended*");
        //}

        //[Fact]
        //public async Task MoveNextThenYieldReturnThenGetResult_Returns()
        //{
        //    const int expectedYieldResult = ExpectedResult + 1;
        //    var iterator = CreateCoroutineReturningConstant().GetAsyncIterator();
        //    _ = await iterator.MoveNextAsync();
        //    iterator.YieldReturn(expectedYieldResult);
        //    var asyncResult = iterator.GetResultAsync();
        //    var result = await asyncResult;
        //    result.Should().Be(expectedYieldResult);
        //}

        //[Fact]
        //public async Task MoveNextThenMoveNextThenGetResult_Returns()
        //{
        //    var iterator = CreateCoroutineReturningConstant().GetAsyncIterator();
        //    _ = await iterator.MoveNextAsync();
        //    _ = await iterator.MoveNextAsync();
        //    var result = iterator.GetResult();
        //    result.Should().Be(ExpectedResult);
        //}

        //[Fact]
        //public async Task MoveNextThenGetResultAsync_Awaits()
        //{
        //    var iterator = AsyncIterator.Create(CreateCoroutineReturningConstant());
        //    _ = await iterator.MoveNextAsync();
        //    var result = await iterator.GetResultAsync();
        //    result.Should().Be(ExpectedResult);
        //}

        //[Fact]
        //public async Task MoveNextThenThrow_Succeeds()
        //{
        //    var iterator = CreateCoroutineReturningConstant().GetAsyncIterator();
        //    _ = await iterator.MoveNextAsync();
        //    iterator.Throw(new Exception1());
        //    await iterator.Awaiting(new Func<IAsyncIterator<int>, Task>(async x => await x.GetResultAsync()))
        //        .Should()
        //        .ThrowExactlyAsync<Exception1>();
        //}
    }
}

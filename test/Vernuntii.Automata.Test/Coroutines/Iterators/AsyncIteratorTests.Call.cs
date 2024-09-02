﻿namespace Vernuntii.Coroutines.Iterators;
partial class AsyncIteratorTests
{
    public class Call
    {
        public class ReturnSynchronously
        {
            private const int ExpectedResult = 2;

            private Coroutine<int> Constant() => Call(() => Coroutine.FromResult(2));

            [Fact]
            public async Task MoveNext_ReturnsFalse()
            {
                var iterator = new AsyncIterator<int>(Constant());
                var canMoveNext = await iterator.MoveNextAsync().ConfigureAwait(false);
                canMoveNext.Should().BeFalse();
            }

            [Fact]
            public async Task GetResult_Returns()
            {
                var iterator = new AsyncIterator<int>(Constant());
                var result = iterator.GetResult();
                result.Should().Be(ExpectedResult);
            }

            [Fact]
            public async Task GetResultAsync_Awaits()
            {
                var iterator = new AsyncIterator<int>(Constant());
                var result = await iterator.GetResultAsync().ConfigureAwait(false);
                result.Should().Be(ExpectedResult);
            }

            [Fact]
            public async Task Throw_Fails()
            {
                var iterator = AsyncIterator.Create(Constant);
                iterator
                    .Invoking(x => x.Throw(new Exception1()))
                    .Should()
                    .ThrowExactly<InvalidOperationException>()
                    .WithMessage("*not started*already finished*not suspended*");
            }
        }

        public class ReturnAfterDelay
        {
            private const int ExpectedResult = 2;

            private Coroutine<int> ConstantAfterDelay() => Call(async () => {
                await Task.Delay(ContinueAfterTimeInMs).ConfigureAwait(false);
                return ExpectedResult;
            });

            [Fact]
            public async Task MoveNext_ReturnsFalse()
            {
                var iterator = new AsyncIterator<int>(ConstantAfterDelay());
                var canMoveNext = await iterator.MoveNextAsync().ConfigureAwait(false);
                canMoveNext.Should().Be(false);
            }

            [Fact]
            public async Task GetResult_Throws()
            {
                var iterator = new AsyncIterator<int>(ConstantAfterDelay());

                var result = iterator
                    .Invoking(x => x.GetResult())
                    .Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("*not finished yet*");
            }

            [Fact]
            public async Task GetResultAsync_Awaits()
            {
                var iterator = AsyncIterator.Create(ConstantAfterDelay());
                var result = await iterator.GetResultAsync();
                result.Should().Be(ExpectedResult);
            }
        }
    }
}

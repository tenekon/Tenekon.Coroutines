﻿using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Tenekon.Coroutines.Benchmark
{
    [MemoryDiagnoser]
    [MediumRunJob]
    public class AsyncIteratorBenchmark
    {
        private const int RunLess = 9;
        private const int RunMore = 999;
        private const int RunMost = 99999;
        private const int Constant = int.MaxValue;

        [Benchmark]
        [Arguments(RunLess)]
        [Arguments(RunMore)]
        [Arguments(RunMost)]
        public async Task AsyncIterator(int runs)
        {
            var generator = Generator(runs).GetAsyncIterator();
            var results = new List<int>();

            while (await generator.MoveNextAsync()) {
                results.Add(((YieldReturnArgument<int>)generator.Current).Result);
            }

            static async Coroutine Generator(int runs)
            {
                var run = runs;
                while (run-- > 0) {
                    await YieldReturn(run);
                    await Task.Yield();
                }
            }
        }

        [Benchmark]
        [Arguments(RunLess)]
        [Arguments(RunMore)]
        [Arguments(RunMost)]
        public async Task AsyncEnumerable(int runs)
        {
            var generator = Generator(runs).GetAsyncEnumerator();
            var results = new List<int>();

            while (await generator.MoveNextAsync()) {
                results.Add(generator.Current);
            }

            static async IAsyncEnumerable<int> Generator(int runs)
            {
                var run = runs;
                while (run-- > 0) {
                    yield return run;
                    await Task.Yield();
                }
            }
        }
    }
}

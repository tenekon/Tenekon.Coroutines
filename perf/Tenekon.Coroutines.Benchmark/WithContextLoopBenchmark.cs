using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Tenekon.Coroutines.Benchmark
{
    [MemoryDiagnoser]
    [MediumRunJob]
    public class WithContextLoopBenchmark
    {
        private const int RunLess = 9;
        private const int RunMore = 999;
        private const int RunMost = 99999;
        private const int RunMostMost = 999999;
        private const long Constant = long.MaxValue;

        [Benchmark]
        public async Task WithContextWithClosure()
        {
            var list = new List<long>();
            await Coroutine.Start(static x => Generator(RunMostMost, x.list, Constant), (RunMostMost, list));

            static async Coroutine Generator(int runs, List<long> list, long constant)
            {
                var run = runs;
                while (run-- > 0) {
                    list.Add(await WithContext(default, static x => {
                        return new Coroutine<long>(x);
                    }, constant));
                }
            }
        }
    }
}

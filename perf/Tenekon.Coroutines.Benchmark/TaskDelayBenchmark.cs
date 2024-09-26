﻿using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Tenekon.Coroutines.Benchmark
{
    [MemoryDiagnoser]
    [ShortRunJob]
    public class TaskDelayBenchmark
    {
        [Benchmark]
        public async Task AwaitTaskDelay()
        {
            await Task.Delay(1).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task AwaitCoroutineTaskDelay()
        {
            await Coroutine.Start(() => Task.Delay(1)).ConfigureAwait(false);
        }
    }
}

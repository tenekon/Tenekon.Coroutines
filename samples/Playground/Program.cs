using System.Diagnostics;
using Tenekon.Coroutines;

await Coroutine.Start(async () =>
{
    var t1 = await Coroutine.Factory.StartNew(async () =>
    {
        Thread.Sleep(3000);
        Console.WriteLine(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime() + ": Finished");
    });

    var t2 = await Coroutine.Factory.StartNew(async () =>
    {
        Thread.Sleep(3000);
        Console.WriteLine(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime() + ": Finished");
    });

    await Task.WhenAll(t1.AsTask(), t2.AsTask());
});
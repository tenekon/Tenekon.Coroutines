using Tenekon.Coroutines;

await Coroutine.Start(async () =>
{
    Console.WriteLine(Environment.CurrentManagedThreadId);

    var t1 = await Coroutine.Factory.StartNew(async () =>
    {
        Console.WriteLine("Hello World " + Environment.CurrentManagedThreadId);
        Thread.Sleep(3000);
        Console.WriteLine("Finished");
    });

    var t2 = await Coroutine.Factory.StartNew(async () =>
    {
        Console.WriteLine("Hello World " + Environment.CurrentManagedThreadId);
        Thread.Sleep(3000);
        Console.WriteLine("Finished");
    });

    await Task.WhenAll(t1.AsTask(), t2.AsTask());
});
# Tenekon.Coroutines [![NuGet](https://img.shields.io/nuget/v/Tenekon.Coroutines)](https://www.nuget.org/packages/Tenekon.Coroutines) [![Discord](https://img.shields.io/discord/:1288602831095468157?logo=discord&label=Tenekon&#32;Community)](https://discord.gg/VCa8ePSAqD)

_Write C# async-await coroutines in a Redux-Saga fashion and let them behave like JavaScript generators_

You found a bug, have ideas or just want to talk? [Join the Tenekon Community](https://discord.gg/VCa8ePSAqD), [start a discussion](https://github.com/tenekon/Tenekon.Coroutines/discussions/new/choose) or [open an issue](https://github.com/tenekon/Tenekon.Coroutines/discussions).

### Tenekon.Coroutines key facts on a glance

- 100% compatible to `Task`/`ValueTask`
  - Almost as fast as `ValueTask`
- Async iterators: make coroutines iterable
  - Each suspension point allows to clone the async iterator
- Fully AOT-compatible
- Highly extendible via new yielders and/or custom coroutine context
- Child coroutines inherit context of their parents: reduce explicit dependency injection

## Installation

**Minimum requirements**

- Target framework that at least supports NET Standard 2.1 or .NET 6.0

**Package Manager**

```
Install-Package Tenekon.Coroutines -Version <type version here>
```

**.NET CLI**

```
dotnet add package Tenekon.Coroutines --version <type version here>
```

## Quick start guide

**Simple Coroutine: Hello World**

```csharp
using static Tenekon.Coroutines.Yielders;

await Coroutine.Start(async () => {
  await Call(Console.WriteLine, "Hello world");
});

// or

await Func<Coroutine>(async () => {
  await Call(Console.WriteLine, "Hello world");
});

// Outputs:
// Hello world
```

**Advanced Coroutine: Parallelism**

```csharp
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

// 00:00:03.0317613: Finished
// 00:00:03.0317757: Finished
```

**Simple AsyncIterator: Iterator**

```csharp
using static Tenekon.Coroutines.Yielders;
using static Tenekon.Coroutines.Yielders.Arguments;

var iterator = AsyncIterator.Create(async () => {
  await Call(Console.WriteLine, "Hello world");
});

while (await iterator.MoveNextAsync()) {
  Console.WriteLine(((CallArgument<string>)iterator.Current).Closure);
}

// Outputs:
// Hello world
// Hello world
```

**Advanced AsyncIterator: Iterator, Replace Current**

```csharp
using static Tenekon.Coroutines.Yielders;
using static Tenekon.Coroutines.Yielders.Arguments;

var iterator = AsyncIterator.Create(async () => {
  await Call(Console.WriteLine, "Hello world");
});

while (await iterator.MoveNextAsync()) {
  Console.WriteLine(((CallArgument<string>)iterator.Current).Closure);
  iterator.Current = new CallArgument<string>(Console.WriteLine, "Hello iterator")
}

// Outputs:
// Hello world
// Hello iterator
```

**Advanced AsyncIterator: Iterator, Yield Assign**

```csharp
using static Tenekon.Coroutines.Iterator.Yielders;
using static Tenekon.Coroutines.Iterator.Yielders.Arguments;

var iterator = AsyncIterator.Create(async () => {
  Console.WriteLine(await Exchange("Hello world"));
});

while (await iterator.MoveNextAsync()) {
  Console.WriteLine(((ExchangeArgument<string>)iterator.Current).Value);
  iterator.YieldAssign("Hello iterator");
}

// Outputs:
// Hello world
// Hello iterator
```

## Quick references

Yielders are the equivalent to effects in Redux-Saga.

In the following are all available `Yielders` classes and their contained yielders listed.

### Tenekon.Coroutines.Coroutine

The yielding components of `Coroutine` are designed to align with the functionality of `Task`.

#### Tenekon.Coroutines.Coroutine.Yield

Use `Yield` to instruct the coroutine to suspend and resume immediatelly. In the underlying code, `Task.Yield()` is used.

| Yielder | Signature                     |
| ------- | ----------------------------- |
| `Yield` | `Coroutine Coroutine Yield()` |

#### Tenekon.Coroutines.Coroutine.Run

The `Coroutine.Run` yielder is the equivalent to `Task.Run`

| Yielder         | Signature                                                                                                                                                           |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Coroutine.Run` | `Coroutine<CoroutineAwaitable> Run(Func<Coroutine> provider, CancellationToken cancellationToken)`                                                                  |
| `Coroutine.Run` | `Coroutine<CoroutineAwaitable> Run(Func<Coroutine> provider)`                                                                                                       |
| `Coroutine.Run` | `Coroutine<CoroutineAwaitable> Run<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, CancellationToken cancellationToken)`                            |
| `Coroutine.Run` | `Coroutine<CoroutineAwaitable> Run<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure)`                                                                 |
| `Coroutine.Run` | `Coroutine<CoroutineAwaitable<TResult>> Run<TResult>(Func<Coroutine<TResult>> provider, CancellationToken cancellationToken)`                                       |
| `Coroutine.Run` | `Coroutine<CoroutineAwaitable<TResult>> Run<TResult>(Func<Coroutine<TResult>> provider)`                                                                            |
| `Coroutine.Run` | `Coroutine<CoroutineAwaitable<TResult>> Run<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, CancellationToken cancellationToken)` |
| `Coroutine.Run` | `Coroutine<CoroutineAwaitable<TResult>> Run<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure)`                                      |

#### Tenekon.Coroutines.Coroutine.Run

The `Coroutine.Factory.StartNew` yielder is the equivalent to `Task.Factory.StartNew`

| Yielder                      | Signature                                                                                                                                                                                                                              |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable> StartNew(Func<Coroutine> provider, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)`                                                                  |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable> StartNew(Func<Coroutine> provider, CancellationToken cancellationToken)`                                                                                                                                |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable> StartNew(Func<Coroutine> provider, TaskCreationOptions creationOptions)`                                                                                                                                |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable> StartNew(Func<Coroutine> provider)`                                                                                                                                                                     |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable> StartNew<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)`                            |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable> StartNew<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, CancellationToken cancellationToken)`                                                                                          |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable> StartNew<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure, TaskCreationOptions creationOptions)`                                                                                          |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable> StartNew<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure)`                                                                                                                               |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable<TResult>> StartNew<TResult>(Func<Coroutine<TResult>> provider, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)`                                       |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable<TResult>> StartNew<TResult>(Func<Coroutine<TResult>> provider, CancellationToken cancellationToken)`                                                                                                     |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable<TResult>> StartNew<TResult>(Func<Coroutine<TResult>> provider, TaskCreationOptions creationOptions)`                                                                                                     |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable<TResult>> StartNew<TResult>(Func<Coroutine<TResult>> provider)`                                                                                                                                          |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable<TResult>> StartNew<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)` |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable<TResult>> StartNew<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, CancellationToken cancellationToken)`                                                               |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable<TResult>> StartNew<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure, TaskCreationOptions creationOptions)`                                                               |
| `Coroutine.Factory.StartNew` | `Coroutine<CoroutineAwaitable<TResult>> StartNew<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure)`                                                                                                    |

### Tenekon.Coroutines.Yielders

We recommend to make use of `using static`, e.g. `using static Tenekon.Coroutines.Yielders`.

#### Tenekon.Coroutines.Yielders.Call

The `Call` yielder is the equivalent to the `call` effect in Redux-Saga.

Use it to instruct the coroutine to suspend and invoke `provider` with optional `closure`. The coroutine resumes as soon as `provider` completed.

| Yielder | Signature                                                                                                   |
| ------- | ----------------------------------------------------------------------------------------------------------- |
| `Call`  | `Coroutine Call(Func<Coroutine> provider)`                                                                  |
| `Call`  | `Coroutine Call<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure)`                            |
| `Call`  | `Coroutine<TResult> Call<TResult>(Func<Coroutine<TResult>> provider)`                                       |
| `Call`  | `Coroutine<TResult> Call<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure)` |

#### Tenekon.Coroutines.Yielders.Launch

The `Launch` yielder is the equivalent to the `fork` effect in Redux-Saga.

Use it to instruct the coroutine to suspend and invoke `provider` with optional `closure`. The coroutine resumes immediatelly, but in case coroutine returns before `provider` finished it will suspend as long as `provider` won't have completed.

| Yielder  | Signature                                                                                                                         |
| -------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `Launch` | `Coroutine<CoroutineAwaitable> Launch(Func<Coroutine> provider)`                                                                  |
| `Launch` | `Coroutine<CoroutineAwaitable> Launch<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure)`                            |
| `Launch` | `Coroutine<CoroutineAwaitable<TResult>> Launch<TResult>(Func<Coroutine<TResult>> provider)`                                       |
| `Launch` | `Coroutine<CoroutineAwaitable<TResult>> Launch<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure)` |

#### Tenekon.Coroutines.Yielders.Spawn

The `Spawn` yielder is the equivalent to the `spawn` effect in Redux-Saga.

Use it to instruct the coroutine to suspend and invoke `provider` with optional `closure`. The coroutine resumes immediatelly, but in case coroutine returns before `provider` finished it will just return and `provider` will just continue to operate independently.

| Yielder | Signature                                                                                                                        |
| ------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `Spawn` | `Coroutine<CoroutineAwaitable> Spawn(Func<Coroutine> provider)`                                                                  |
| `Spawn` | `Coroutine<CoroutineAwaitable> Spawn<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure)`                            |
| `Spawn` | `Coroutine<CoroutineAwaitable<TResult>> Spawn<TResult>(Func<Coroutine<TResult>> provider)`                                       |
| `Spawn` | `Coroutine<CoroutineAwaitable<TResult>> Spawn<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure)` |

#### Tenekon.Coroutines.Yielders.Spawn

Use `Throw` to instruct the coroutine to suspend and let the coroutine throw `exception`. The coroutine resumes immediatelly and throws `excpetion`.

| Yielder | Signature                              |
| ------- | -------------------------------------- |
| `Throw` | `Coroutine Throw(Exception exception)` |

#### Tenekon.Coroutines.Yielders.WithContext

Use `WithContext` to instruct the coroutine to suspend and invoke `provider` with `additiveContext` and optional `closure`.

| Yielder       | Signature                                                                                                                                            |
| ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `WithContext` | `Coroutine WithContext(CoroutineContext additiveContext, Func<Coroutine> provider)`                                                                  |
| `WithContext` | `Coroutine WithContext<TClosure>(CoroutineContext additiveContext, Func<TClosure, Coroutine> provider, TClosure closure)`                            |
| `WithContext` | `Coroutine<TResult> WithContext<TResult>(CoroutineContext additiveContext, Func<Coroutine<TResult>> provider)`                                       |
| `WithContext` | `Coroutine<TResult> WithContext<TClosure, TResult>(CoroutineContext additiveContext, Func<TClosure, Coroutine<TResult>> provider, TClosure closure)` |

#### Tenekon.Coroutines.Yielders.YieldReturn

Use `YieldReturn` to instruct the coroutine to suspend and resume immediatelly. Allows the coroutine middleware to get the hands on `value`.

| Yielder       | Signature                           |
| ------------- | ----------------------------------- |
| `YieldReturn` | `Coroutine YieldReturn<T>(T value)` |

#### Tenekon.Coroutines.Yielders.YieldAssign

Use `YieldAssign` to instruct the coroutine to suspend and resume immediatelly.
Allows the coroutine middleware to get the hands on `value` and also to yield assign a custom value of type `TAssign`.
If you do not yield assign a custom value of type `TAssign`, then `default(TAssign)` is yield assigned.

| Yielder       | Signature                                                                                        |
| ------------- | ------------------------------------------------------------------------------------------------ |
| `YieldAssign` | `YieldAssign<TYield> Yield<TYield>(TYield value) => new(value)` -> `Coroutine<TAssign> Assign<TAssign>()` |

## Tenekon.Coroutines.Yielders.Exchange

Use `Exchange` to instruct the coroutine to suspend and resume immediatelly. Allows the coroutine middleware to get the hands on `value` and exchange it.

| Yielder    | Signature                           |
| ---------- | ----------------------------------- |
| `Exchange` | `Coroutine<T> Exchange<T>(T value)` |
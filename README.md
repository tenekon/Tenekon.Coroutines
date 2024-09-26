# Tenekon.Coroutines [![NuGet](https://img.shields.io/nuget/v/Tenekon.Coroutines)](https://www.nuget.org/packages/Tenekon.Coroutines) [![Discord](https://img.shields.io/discord/:1288602831095468157?logo=discord&label=Tenekon&#32;Community)](https://discord.gg/VCa8ePSAqD)

_Write C# async-await coroutines in a Redux-Saga fashion and let them behave like JavaScript generators_

You found a bug, have ideas or just want to talk? [Join the Tenekon Community](https://discord.gg/VCa8ePSAqD), [start a discussion](https://github.com/tenekon/Tenekon.Coroutines/discussions/new/choose) or [open an issue](https://github.com/tenekon/Tenekon.Coroutines/discussions).

### Tenekon.Coroutines key facts on a glance

- Async iterators: make coroutines iterable
  - Each suspension point allows to clone the async iterator
- Fully AOT-compatible
- Highly extendible via new yielders and/or custom coroutine context
- Child coroutines inherit context of their parents: reduce explicit dependency injection
- Almost as fast as `ValueTask`

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

**Simple Coroutine**

```csharp
using static Tenekon.Coroutines.Yielders;

await Coroutine.Start(asnyc () => {
  await Call(Console.WriteLine, "Hello world");
});

// or

await Func<Coroutine>(asnyc () => {
  await Call(Console.WriteLine, "Hello world");
});

// Outputs:
// Hello world
```

**Simple AsyncIterator**

```csharp
using static Tenekon.Coroutines.Yielders;
using static Tenekon.Coroutines.Yielders.Arguments;

var iterator = AsyncIterator.Create(asnyc () => {
  await Call(Console.WriteLine, "Hello world");
});

while (await iterator.MoveNextAsync()) {
  if (iterator.CurrentKey == CallKey) {
    Console.WriteLine(((CallArgument<string>)iterator.Current).Closure);
  }
}

// Outputs:
// Hello world
```

**Advanced AsyncIterator**

```csharp
using static Tenekon.Coroutines.Yielders;
using static Tenekon.Coroutines.Yielders.Arguments;

var iterator = AsyncIterator.Create(asnyc () => {
  await Call(Console.WriteLine, "Hello world");
});

while (await iterator.MoveNextAsync()) {
  if (iterator.CurrentKey == CallKey) {
    Console.WriteLine(((CallArgument<string>)iterator.Current).Closure);
    iterator.Current = new CallArgument<string>(Console.WriteLine, "Hello iterator")
  }
}

// Outputs:
// Hello world
// Hello iterator
```

**Advanced AsyncIterator**

```csharp
using static Tenekon.Coroutines.Iterator.Yielders;
using static Tenekon.Coroutines.Iterator.Yielders.Arguments;

var iterator = AsyncIterator.Create(asnyc () => {
  Console.WriteLine(await Exchange("Hello world"));
});

while (await iterator.MoveNextAsync()) {
  if (iterator.CurrentKey == ExchangeKey) {
    Console.WriteLine(((CallArgument<string>)iterator.Current).Closure);
    iterator.YieldReturn("Hello iterator");
  }
}

// Outputs:
// Hello world
// Hello iterator
```

## Quick references

Yielders are the equivalent to effects in Redux-Saga.

In the following are all available `Yielders` classes and their contained yielders listed.

We recommend to make use of `using static`, e.g. `using static Tenekon.Coroutines.Yielders`.

### Tenekon.Coroutines.Yielders

#### Call

The `Call` yielder is the equivalent to the `call` effect in Redux-Saga.

Use it to instruct the coroutine to suspend and invoke `provider` with optional `closure`. The coroutine resumes as soon as `provider` completed.

| Yielder | Signature                                                                                                   |
| ------- | ----------------------------------------------------------------------------------------------------------- |
| `Call`  | `Coroutine Call(Func<Coroutine> provider)`                                                                  |
| `Call`  | `Coroutine Call<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure)`                            |
| `Call`  | `Coroutine<TResult> Call<TResult>(Func<Coroutine<TResult>> provider)`                                       |
| `Call`  | `Coroutine<TResult> Call<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure)` |

#### Launch

The `Launch` yielder is the equivalent to the `fork` effect in Redux-Saga.

Use it to instruct the coroutine to suspend and invoke `provider` with optional `closure`. The coroutine resumes immediatelly, but in case coroutine returns before `provider` finished it will suspend as long as `provider` won't have completed.

| Yielder  | Signature                                                                                                                         |
| -------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `Launch` | `Coroutine<CoroutineAwaitable> Launch(Func<Coroutine> provider)`                                                                  |
| `Launch` | `Coroutine<CoroutineAwaitable> Launch<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure)`                            |
| `Launch` | `Coroutine<CoroutineAwaitable<TResult>> Launch<TResult>(Func<Coroutine<TResult>> provider)`                                       |
| `Launch` | `Coroutine<CoroutineAwaitable<TResult>> Launch<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure)` |

#### Spawn

The `Spawn` yielder is the equivalent to the `spawn` effect in Redux-Saga.

Use it to instruct the coroutine to suspend and invoke `provider` with optional `closure`. The coroutine resumes immediatelly, but in case coroutine returns before `provider` finished it will just return and `provider` will just continue to operate independently.

| Yielder | Signature                                                                                                                        |
| ------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `Spawn` | `Coroutine<CoroutineAwaitable> Spawn(Func<Coroutine> provider)`                                                                  |
| `Spawn` | `Coroutine<CoroutineAwaitable> Spawn<TClosure>(Func<TClosure, Coroutine> provider, TClosure closure)`                            |
| `Spawn` | `Coroutine<CoroutineAwaitable<TResult>> Spawn<TResult>(Func<Coroutine<TResult>> provider)`                                       |
| `Spawn` | `Coroutine<CoroutineAwaitable<TResult>> Spawn<TClosure, TResult>(Func<TClosure, Coroutine<TResult>> provider, TClosure closure)` |

#### Spawn

Use `Throw` to instruct the coroutine to suspend and let the coroutine throw `exception`. The coroutine resumes immediatelly and throws `excpetion`.

| Yielder | Signature                              |
| ------- | -------------------------------------- |
| `Throw` | `Coroutine Throw(Exception exception)` |

#### WithContext

Use `WithContext` to instruct the coroutine to suspend and invoke `provider` with `additiveContext` and optional `closure`.

| Yielder       | Signature                                                                                                                                            |
| ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `WithContext` | `Coroutine WithContext(CoroutineContext additiveContext, Func<Coroutine> provider)`                                                                  |
| `WithContext` | `Coroutine WithContext<TClosure>(CoroutineContext additiveContext, Func<TClosure, Coroutine> provider, TClosure closure)`                            |
| `WithContext` | `Coroutine<TResult> WithContext<TResult>(CoroutineContext additiveContext, Func<Coroutine<TResult>> provider)`                                       |
| `WithContext` | `Coroutine<TResult> WithContext<TClosure, TResult>(CoroutineContext additiveContext, Func<TClosure, Coroutine<TResult>> provider, TClosure closure)` |


#### Yield

Use `YieldReturn` to instruct the coroutine to suspend and resume immediatelly. Allows the coroutine middleware to get the hands on `value`.

| Yielder       | Signature                           |
| ------------- | ----------------------------------- |
| `YieldReturn` | `Coroutine YieldReturn<T>(T value)` |

## Tenekon.Coroutines.Iterators.Yielders

Use `Exchange` to instruct the coroutine to suspend and resume immediatelly. Allows the coroutine middleware to get the hands on `value` and exchange it.

| Yielder    | Signature                           |
| ---------- | ----------------------------------- |
| `Exchange` | `Coroutine<T> Exchange<T>(T value)` |
﻿using System.Runtime.CompilerServices;

namespace Vernuntii.Coroutines.CompilerServices;

public struct CoroutineAwaiter : ICriticalNotifyCompletion, ICoroutineAwaiter, IRelativeCoroutine
{
    public readonly bool IsCompleted => _awaiter.IsCompleted;

    private readonly object? _coroutineActioner;
    internal CoroutineAction _coroutineAction;
    private readonly ValueTaskAwaiter _awaiter;

    readonly object? IRelativeCoroutine.CoroutineActioner => _coroutineActioner;
    readonly CoroutineAction IRelativeCoroutine.CoroutineAction => _coroutineAction;

    internal CoroutineAwaiter(in ValueTaskAwaiter awaiter, object? coroutineActioner, CoroutineAction coroutineAction)
    {
        _awaiter = awaiter;
        _coroutineActioner = coroutineActioner;
        _coroutineAction = coroutineAction;
    }

    void IRelativeCoroutine.MarkCoroutineAsActedOn() => _coroutineAction = CoroutineAction.Task;

    public void GetResult() => _awaiter.GetResult();

    public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation) => _awaiter.UnsafeOnCompleted(continuation);
}

using System.Diagnostics;
using Tenekon.Coroutines.Iterators;

namespace Tenekon.Coroutines.CompilerServices;

public struct ConfiguredCoroutineAwaiter : ICriticalNotifyCompletion, IRelativeCoroutineAwaiter, ICoroutineAwaiter
{
    public readonly bool IsCompleted => _awaiter.IsCompleted;

    internal readonly object? _coroutineActioner;
    internal CoroutineAction _coroutineAction;
    internal ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _awaiter;

    readonly object? IRelativeCoroutine.CoroutineActioner => _coroutineActioner;
    readonly CoroutineAction IRelativeCoroutine.CoroutineAction => _coroutineAction;

    internal ConfiguredCoroutineAwaiter(in ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter, object? coroutineActioner, CoroutineAction coroutineAction)
    {
        _awaiter = awaiter;
        _coroutineActioner = coroutineActioner;
        _coroutineAction = coroutineAction;
    }

    void IRelativeCoroutine.MarkCoroutineAsActedOn() => _coroutineAction = CoroutineAction.None;

    public readonly void GetResult() => _awaiter.GetResult();

    public void OnCompleted(Action continuation)
    {
        if (_coroutineAction != CoroutineAction.None) {
            CoroutineMethodBuilderCore.ActOnCoroutine(ref this);
        }
        _awaiter.OnCompleted(continuation);
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        if (_coroutineAction != CoroutineAction.None) {
            CoroutineMethodBuilderCore.ActOnCoroutine(ref this);
        }
        _awaiter.UnsafeOnCompleted(continuation);
    }

    readonly void IRelativeCoroutineAwaiter.RenewStateMachineCoroutineAwaiter<[DAM(StateMachineMemberTypes)] TStateMachine>(
        IAsyncIteratorStateMachineHolder theirStateMachineHolder,
        in SuspensionPoint ourSuspensionPoint,
        ref SuspensionPoint theirSuspensionPoint) =>
        CoroutineAwaiterCore.RenewStateMachineCoroutineAwaiter<TStateMachine, ConfiguredCoroutineAwaiter, ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter, ValueTaskAccessor, VoidCoroutineResult>(
            theirStateMachineHolder,
            in ourSuspensionPoint,
            ref theirSuspensionPoint,
            GetAwaiter);

    private static ref ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter GetAwaiter(ref ConfiguredCoroutineAwaiter coroutineAwaiter) => ref coroutineAwaiter._awaiter;
}

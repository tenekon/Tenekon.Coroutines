using Tenekon.Coroutines.Iterators;

namespace Tenekon.Coroutines.CompilerServices;

public struct CoroutineAwaiter : ICriticalNotifyCompletion, ICoroutineAwaiter, IRelativeCoroutineAwaiter
{
    public readonly bool IsCompleted => _awaiter.IsCompleted;

    private object? _coroutineActioner;
    internal CoroutineAction _coroutineAction;
    private ValueTaskAwaiter _awaiter;

    readonly object? IRelativeCoroutine.CoroutineActioner => _coroutineActioner;
    readonly CoroutineAction IRelativeCoroutine.CoroutineAction => _coroutineAction;

    internal CoroutineAwaiter(in ValueTaskAwaiter awaiter, object? coroutineActioner, CoroutineAction coroutineAction)
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
        CoroutineAwaiterCore.RenewStateMachineCoroutineAwaiter<TStateMachine, CoroutineAwaiter, ValueTaskAwaiter, ValueTaskAccessor, Nothing>(
            theirStateMachineHolder,
            in ourSuspensionPoint,
            ref theirSuspensionPoint,
            GetAwaiter);

    private static ref ValueTaskAwaiter GetAwaiter(ref CoroutineAwaiter coroutineAwaiter) => ref coroutineAwaiter._awaiter;
}

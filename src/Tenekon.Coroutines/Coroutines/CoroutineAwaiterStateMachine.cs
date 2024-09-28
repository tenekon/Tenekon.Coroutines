namespace Tenekon.Coroutines;

internal interface ICoroutineAwaiterMethodBuilder
{
    bool IsCompleted { get; }
    void AwaitUnsafeOnCompleted();
    void GetResult();
    void SetException(Exception e);
    void SetResult();
}

internal interface ICoroutineAwaiterMethodBuilder<TResult>
{
    bool IsCompleted { get; }
    void AwaitUnsafeOnCompleted();
    TResult GetResult();
    void SetException(Exception e);
    void SetResult(TResult result);
}

internal readonly struct CoroutineAwaiterMethodBuilder<TCoroutineAwaiter>(
    in TCoroutineAwaiter awaiter,
    CoroutineStateMachineHolder<Nothing, CoroutineAwaiterStateMachine<CoroutineAwaiterMethodBuilder<TCoroutineAwaiter>>> stateMachineHolder) : ICoroutineAwaiterMethodBuilder
    where TCoroutineAwaiter : struct, ICriticalNotifyCompletion, ICoroutineAwaiter
{
    public readonly bool IsCompleted => _awaiter.IsCompleted;

    public readonly TCoroutineAwaiter _awaiter = awaiter;
    public readonly CoroutineStateMachineHolder<Nothing,CoroutineAwaiterStateMachine<CoroutineAwaiterMethodBuilder<TCoroutineAwaiter>>> _stateMachineHolder = stateMachineHolder;

    public readonly void AwaitUnsafeOnCompleted() => _awaiter.UnsafeOnCompleted(_stateMachineHolder.MoveNextAction);

    public readonly void SetException(Exception e) => _stateMachineHolder.SetException(e);

    public readonly void GetResult() => _awaiter.GetResult();

    public readonly void SetResult() => _stateMachineHolder.SetResult(default);
}

internal readonly struct CoroutineAwaiterMethodBuilder<TCoroutineAwaiter, TResult>(
    in TCoroutineAwaiter awaiter,
    CoroutineStateMachineHolder<TResult, CoroutineAwaiterStateMachine<CoroutineAwaiterMethodBuilder<TCoroutineAwaiter, TResult>, TResult>> stateMachineHolder) : ICoroutineAwaiterMethodBuilder<TResult>
    where TCoroutineAwaiter : struct, ICriticalNotifyCompletion, ICoroutineAwaiter<TResult>
{
    public readonly bool IsCompleted => _awaiter.IsCompleted;

    private readonly TCoroutineAwaiter _awaiter = awaiter;
    private readonly CoroutineStateMachineHolder<TResult,CoroutineAwaiterStateMachine<CoroutineAwaiterMethodBuilder<TCoroutineAwaiter, TResult>, TResult>> _stateMachineHolder = stateMachineHolder;

    public readonly void AwaitUnsafeOnCompleted() => _awaiter.UnsafeOnCompleted(_stateMachineHolder.MoveNextAction);

    public readonly void SetException(Exception e) => _stateMachineHolder.SetException(e);

    public readonly TResult GetResult() => _awaiter.GetResult();

    public readonly void SetResult(TResult result) => _stateMachineHolder.SetResult(result);
}

internal struct CoroutineAwaiterStateMachine<TBuilder>(in TBuilder builder) : IAsyncStateMachine
    where TBuilder : struct, ICoroutineAwaiterMethodBuilder
{
    internal int _state; // Externally set to -1

    private readonly TBuilder _builder = builder;

    void IAsyncStateMachine.MoveNext()
    {
        var state = _state;

        try {
            if (state != 0) {
                if (!_builder.IsCompleted) {
                    _builder.AwaitUnsafeOnCompleted();
                    return;
                }
            }
            _builder.GetResult();
        } catch (Exception error) {
            _state = -2;
            _builder.SetException(error);
            return;
        }

        _state = -2;
        _builder.SetResult();
    }

    void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) => throw new NotImplementedException();
}

internal struct CoroutineAwaiterStateMachine<TBuilder, TResult>(in TBuilder builder) : IAsyncStateMachine
    where TBuilder : struct, ICoroutineAwaiterMethodBuilder<TResult>
{
    internal int _state; // Externally set to -1

    private readonly TBuilder _builder = builder;

    void IAsyncStateMachine.MoveNext()
    {
        var state = _state;
        TResult result;

        try {
            if (state != 0) {
                if (!_builder.IsCompleted) {
                    _builder.AwaitUnsafeOnCompleted();
                    return;
                }
            }
            result = _builder.GetResult();
        } catch (Exception error) {
            _state = -2;
            _builder.SetException(error);
            return;
        }

        _state = -2;
        _builder.SetResult(result);
    }

    void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) => throw new NotImplementedException();
}

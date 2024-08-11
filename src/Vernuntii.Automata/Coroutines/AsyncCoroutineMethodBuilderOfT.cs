﻿using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vernuntii.Coroutines;

public struct AsyncCoroutineMethodBuilder<T>
{
    public static AsyncCoroutineMethodBuilder<T> Create()
    {
        return new AsyncCoroutineMethodBuilder<T>();
    }

    public unsafe Coroutine<T> Task {
        get {
            fixed (AsyncCoroutineMethodBuilder<T>* builder = &this) {
                _coroutineNode.SetResultStateMachine(new CoroutineResultStateMachine(builder));
                return new Coroutine<T>(_builder.Task, builder);
            }
        }
    }

    private CoroutineStackNode _coroutineNode;
    private Action? _stateMachineInitiator;
    private PoolingAsyncValueTaskMethodBuilder<T> _builder; // Must not be readonly due to mutable struct

    internal void SetCoroutineNode(ref CoroutineStackNode parentNode)
    {
        parentNode.InitializeChildCoroutine(ref _coroutineNode);
    }

    [DebuggerStepThrough]
    public unsafe void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        _stateMachineInitiator = stateMachine.MoveNext;
    }

    internal unsafe void Start()
    {
        _coroutineNode.Start();
        _stateMachineInitiator?.Invoke();
        _stateMachineInitiator = null;
    }

    public void SetException(Exception e)
    {
        var resultStateMachine = Unsafe.As<CoroutineResultStateMachine>(_coroutineNode.ResultStateMachine);
        resultStateMachine.SetException(e);
        _coroutineNode.Stop();
    }

    public void SetResult(T result)
    {
        var resultStateMachine = Unsafe.As<CoroutineResultStateMachine>(_coroutineNode.ResultStateMachine);
        resultStateMachine.SetResult(result);
        _coroutineNode.Stop();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        AsyncCoroutineMethodBuilderCore.ProcessAwaiterBeforeAwaitingOnCompleted(ref awaiter, ref _coroutineNode);
        _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        _builder.SetStateMachine(stateMachine);
    }

    internal unsafe class CoroutineResultStateMachine : AbstractCoroutineResultStateMachine<T>
    {
        private AsyncCoroutineMethodBuilder<T>* _builder;

        public CoroutineResultStateMachine(AsyncCoroutineMethodBuilder<T>* builder) => _builder = builder;

        protected override void SetExceptionCore(Exception error)
        {
            _builder->_builder.SetException(error);
        }

        protected override void SetResultCore(T result)
        {
            _builder->_builder.SetResult(result);
        }
    }
}

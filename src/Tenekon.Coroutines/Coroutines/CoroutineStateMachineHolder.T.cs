﻿using System.Diagnostics;
using System.Threading.Tasks.Sources;
using Tenekon.Coroutines.Iterators;
using Tenekon.Coroutines.Sources;

namespace Tenekon.Coroutines;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/// <summary>Provides a strongly-typed box object based on the specific state machine type in use.</summary>
internal sealed class CoroutineStateMachineHolder<TResult, [DAM(StateMachineMemberTypes)] TStateMachine> : CoroutineStateMachineHolder<TResult>, IValueTaskSource<TResult>, IValueTaskSource,
    ICoroutineStateMachineHolder<TResult>, ICoroutineResultStateMachineHolder, IAsyncIteratorStateMachineHolder<TResult>
    where TStateMachine : IAsyncStateMachine
{
    /// <summary>Delegate used to invoke on an ExecutionContext when passed an instance of this box type.</summary>
    private static readonly ContextCallback s_callback = ExecutionContextCallback;

    /// <summary>Per-core cache of boxes, with one box per core.</summary>
    /// <remarks>Each element is padded to expected cache-line size so as to minimize false sharing.</remarks>
    private static readonly CacheLineSizePaddedReference[] s_perCoreCache = new CacheLineSizePaddedReference[Environment.ProcessorCount];

    /// <summary>Gets the slot in <see cref="s_perCoreCache"/> for the current core.</summary>
    private static ref CoroutineStateMachineHolder<TResult, TStateMachine>? PerCoreCacheSlot {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // only two callers are RentFrom/ReturnToCache
        get {
            // Get the current processor ID.  We need to ensure it fits within s_perCoreCache, so we
            // could % by its length, but we can do so instead by Environment.ProcessorCount, which will be a const
            // in tier 1, allowing better code gen, and then further use uints for even better code gen.
            Debug.Assert(s_perCoreCache.Length == Environment.ProcessorCount, $"{s_perCoreCache.Length} != {Environment.ProcessorCount}");
            var i = (int)((uint)Thread.GetCurrentProcessorId() % (uint)Environment.ProcessorCount);

            // We want an array of StateMachineBox<> objects, each consuming its own cache line so that
            // elements don't cause false sharing with each other.  But we can't use StructLayout.Explicit
            // with generics.  So we use object fields, but always reinterpret them (for all reads and writes
            // to avoid any safety issues) as StateMachineBox<> instances.
#if DEBUG
            var transientValue = s_perCoreCache[i].Object;
            Debug.Assert(transientValue is null || transientValue is CoroutineStateMachineHolder<TResult, TStateMachine>,
                $"Expected null or {nameof(CoroutineStateMachineHolder<TResult, TStateMachine>)}, got '{transientValue}'");
#endif
            return ref Unsafe.As<object?, CoroutineStateMachineHolder<TResult, TStateMachine>?>(ref s_perCoreCache[i].Object);
        }
    }

    /// <summary>Thread-local cache of boxes. This currently only ever stores one.</summary>
    [ThreadStatic]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "thread-static")]
    private static CoroutineStateMachineHolder<TResult, TStateMachine>? t_tlsCache;

    /// <summary>Gets a box object to use for an operation.  This may be a reused, pooled object, or it may be new.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // only one caller
    internal static CoroutineStateMachineHolder<TResult, TStateMachine> RentFromCache()
    {
        // First try to get a box from the per-thread cache.
        var box = t_tlsCache;

        if (box is not null) {
            t_tlsCache = null;
        } else {
            // If we can't, then try to get a box from the per-core cache.
            ref var slot = ref PerCoreCacheSlot;

            if (slot is null || (box = Interlocked.Exchange(ref slot, null)) is null) {
                // If we can't, just create a new one.
                box = new CoroutineStateMachineHolder<TResult, TStateMachine>();
            }
        }

        box.Initialize();
        return box;
    }

    /// <summary>The state machine itself.</summary>
    public TStateMachine? StateMachine;

    /// <summary>A delegate to the <see cref="MoveNext()"/> method.</summary>
    public Action MoveNextAction => _moveNextAction ??= new Action(MoveNext);

    ref CoroutineContext ICoroutineStateMachineHolder.CoroutineContext => ref _coroutineContext;

    private void Initialize()
    {
        _coroutineContext.SetResultStateMachine(this);
        _result = CoroutineStateMachineBoxResult<TResult>.Default;
    }

    /// <summary>Returns this instance to the cache.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // only two callers
    private void ReturnToCache()
    {
        // Clear out the state machine and associated context to avoid keeping arbitrary state referenced by
        // lifted locals, and reset the instance for another await.
        StateMachine = default;
        _executionContext = default;
        _coroutineContext.OnCoroutineCompleted();
        _coroutineContext = default;
        //_isCoroutineReserved = 0;
        _valueTaskSource.Reset();

        // If the per-thread cache is empty, store this into it..
        if (t_tlsCache is null) {
            t_tlsCache = this;
        } else {
            // Otherwise, store it into the per-core cache.
            ref var slot = ref PerCoreCacheSlot;

            if (slot is null) {
                // Try to avoid the write if we know the slot isn't empty (we may still have a benign race condition and
                // overwrite what's there if something arrived in the interim).
                Volatile.Write(ref slot, this);
            }
        }
    }

    void ICoroutineResultStateMachineHolder.IncrementBackgroundTasks()
    {
        CoroutineStateMachineBoxResult<TResult>? currentState;
        CoroutineStateMachineBoxResult<TResult> newState;

        do {
            currentState = _result;

            if (currentState is null || currentState.Status != CoroutineStateMachineBoxResult<TResult>.CoroutineStatus.Running) {
                throw new InvalidOperationException("Result state machine has already finished");
            }

            newState = new CoroutineStateMachineBoxResult<TResult>(currentState, currentState.ForkCount + 1);
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _result, newState, currentState), currentState));
    }

    void ICoroutineResultStateMachineHolder.DecrementBackgroundTasks()
    {
        CoroutineStateMachineBoxResult<TResult>? currentState;
        CoroutineStateMachineBoxResult<TResult>? newState;

        do {
            currentState = _result;

            if (currentState is null) {
                return;
            }

            if (currentState.ForkCount == 1 && currentState.Status != CoroutineStateMachineBoxResult<TResult>.CoroutineStatus.Running) {
                newState = null;
            } else {
                newState = new CoroutineStateMachineBoxResult<TResult>(currentState, currentState.ForkCount - 1);
            }
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _result, newState, currentState), currentState));

        if (newState is null) {
            if (currentState.IsCompletedSuccessfully) {
                SetResultDirectly(currentState.Result);
            } else {
                Debug.Assert(currentState.Exception is not null);
                SetExceptionDirectly(currentState.Exception);
            }

            if (_coroutineContext._isCoroutineAsyncIteratorSupplier) {
                Debug.Assert(currentState.CompletionPendingBackgroundTaskSource is not null);
                currentState.CompletionPendingBackgroundTaskSource.SetDefaultResult();
            }
        }
    }

    void ICoroutineResultStateMachineHolder.RegisterCriticalBackgroundTaskAndNotifyOnCompletion<TAwaiter>(ref TAwaiter forkAwaiter, Action forkCompleted)
    {
        CoroutineStateMachineBoxResult<TResult>? currentState;
        CoroutineStateMachineBoxResult<TResult> newState;

        do {
            currentState = _result;

            if (currentState is null || currentState.Status != CoroutineStateMachineBoxResult<TResult>.CoroutineStatus.Running) {
                throw new InvalidOperationException("Result state machine has already finished");
            }

            newState = new CoroutineStateMachineBoxResult<TResult>(currentState, currentState.ForkCount + 1);
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _result, newState, currentState), currentState));

        forkAwaiter.UnsafeOnCompleted(() => {
            Exception? childError = null;

            try {
                forkCompleted();
            } catch (Exception error) {
                childError = error;
            }

            CoroutineStateMachineBoxResult<TResult>? currentState;
            CoroutineStateMachineBoxResult<TResult>? newState;

            do {
                currentState = _result;

                if (currentState is null) {
                    return;
                }

                if (currentState.ForkCount == 1 && currentState.Status != CoroutineStateMachineBoxResult<TResult>.CoroutineStatus.Running) {
                    newState = null;
                } else {
                    newState = new CoroutineStateMachineBoxResult<TResult>(currentState, currentState.ForkCount - 1);
                }
            } while (!ReferenceEquals(Interlocked.CompareExchange(ref _result, newState, currentState), currentState));

            if (newState is null) {
                if (currentState.IsCompletedSuccessfully) {
                    SetResultDirectly(currentState.Result);
                } else {
                    Debug.Assert(currentState.Exception is not null);
                    SetExceptionDirectly(currentState.Exception);
                }

                if (_coroutineContext._isCoroutineAsyncIteratorSupplier) {
                    Debug.Assert(currentState.CompletionPendingBackgroundTaskSource is not null);
                    currentState.CompletionPendingBackgroundTaskSource.SetDefaultResult();
                }
            }
        });
    }

    /// <summary>
    /// Used to initialize s_callback above. We don't use a lambda for this on purpose: a lambda would
    /// introduce a new generic type behind the scenes that comes with a hefty size penalty in AOT builds.
    /// </summary>
    private static void ExecutionContextCallback(object? s)
    {
        // Only used privately to pass directly to EC.Run
        Debug.Assert(s is CoroutineStateMachineHolder<TResult, TStateMachine>, $"Expected {nameof(CoroutineStateMachineHolder<TResult, TStateMachine>)}, got '{s}'");
        Unsafe.As<CoroutineStateMachineHolder<TResult, TStateMachine>>(s).StateMachine!.MoveNext();
    }

    /// <summary>Calls MoveNext on <see cref="StateMachine"/></summary>
    public void MoveNext()
    {
        var context = _executionContext;

        if (context is null) {
            Debug.Assert(StateMachine is not null, $"Null {nameof(StateMachine)}");
            StateMachine.MoveNext();
        } else {
            ExecutionContext.Run(context, s_callback, this);
        }
    }

    /// <summary>Completes the box with a result.</summary>
    /// <param name="result">The result.</param>
    public override void SetResult(TResult result)
    {
        CoroutineStateMachineBoxResult<TResult> currentState;
        CoroutineStateMachineBoxResult<TResult>? newState;

        var isCoroutineAsyncIteratorSupplier = _coroutineContext._isCoroutineAsyncIteratorSupplier;
        ManualResetCoroutineCompletionSource<VoidCoroutineResult>? completionSource = null;

        do {
            currentState = _result!; // Cannot be null at this state

            newState = currentState.ForkCount == 0 ? null : new CoroutineStateMachineBoxResult<TResult>(
                currentState.ForkCount,
                result,
                isCoroutineAsyncIteratorSupplier
                    ? completionSource ??= ManualResetCoroutineCompletionSource<VoidCoroutineResult>.RentFromCache()
                    : null);
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _result, newState, currentState), currentState));

        if (newState is null) {
            SetResultDirectly(result);
            return;
        }

        //if (isCoroutineAsyncIteratorSupplier) {
        //    Debug.Assert(completionSource is not null);
        //    var iteratorContextService = _coroutineContext.GetAsyncIteratorContextService();
        //    iteratorContextService._currentSuspensionPoint.SupplyAwaiterCompletionNotifierInternal(completionSource);
        //}
    }

    /// <summary>Completes the box with an error.</summary>
    /// <param name="error">The exception.</param>
    public override void SetException(Exception error)
    {
        CoroutineStateMachineBoxResult<TResult> currentState;
        CoroutineStateMachineBoxResult<TResult>? newState;

        var isCoroutineAsyncIteratorSupplier = _coroutineContext._isCoroutineAsyncIteratorSupplier;
        ManualResetCoroutineCompletionSource<VoidCoroutineResult>? completionSource = null;

        do {
            currentState = _result!; // Cannot be null at this state
            newState = currentState.ForkCount == 0 ? null : new CoroutineStateMachineBoxResult<TResult>(
                currentState.ForkCount,
                error,
                isCoroutineAsyncIteratorSupplier
                    ? completionSource ??= ManualResetCoroutineCompletionSource<VoidCoroutineResult>.RentFromCache()
                    : null);
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _result, newState, currentState), currentState));

        if (newState is null) {
            SetExceptionDirectly(error);
            return;
        }

        //if (isCoroutineAsyncIteratorSupplier) {
        //    Debug.Assert(completionSource is not null);
        //    var iteratorContextService = _coroutineContext.GetAsyncIteratorContextService();
        //    iteratorContextService._currentSuspensionPoint.SupplyAwaiterCompletionNotifierInternal(completionSource);
        //}
    }

    /// <summary>Get the result of the operation.</summary>
    TResult IValueTaskSource<TResult>.GetResult(short token)
    {
        try {
            return _valueTaskSource.GetResult(token);
        } finally {
            ReturnToCache();
        }
    }

    /// <summary>Get the result of the operation.</summary>
    void IValueTaskSource.GetResult(short token)
    {
        try {
            _valueTaskSource.GetResult(token);
        } finally {
            ReturnToCache();
        }
    }

    void IAsyncIteratorStateMachineHolder<TResult>.SetAsyncIteratorCompletionSource(ICompletionSource<TResult>? completionSource) =>
        _valueTaskSource._asyncIteratorCompletionSource = completionSource;

    void IAsyncIteratorStateMachineHolder<TResult>.SetResult(TResult result) => _valueTaskSource._valueTaskSource.SetResult(result);

    void IAsyncIteratorStateMachineHolder<TResult>.SetException(Exception e) => _valueTaskSource._valueTaskSource.SetException(e);

    IAsyncIteratorStateMachineHolder<VoidCoroutineResult> IAsyncIteratorStateMachineHolder.CreateNewByCloningUnderlyingStateMachine(
        in SuspensionPoint ourSuspensionPoint,
        ref SuspensionPoint theirSuspensionPoint)
    {
        Debug.Assert(StateMachine is not null);
        var theirStateMachine = CoroutineStateMachineAccessor<TStateMachine>.CloneStateMachine(in StateMachine);
        if (GlobalRuntimeFeature.IsDynamicCodeSupported) {
            ref var theirBuilder = ref CoroutineStateMachineAccessor<TStateMachine>.CoroutineMethodBuilderAccessor.GetValueReference(ref theirStateMachine);
            var theirStateMachineHolder = theirBuilder.ReplaceCoroutineUnderlyingStateMachine(ref theirStateMachine);
            ourSuspensionPoint._coroutineAwaiter.RenewStateMachineCoroutineAwaiter<TStateMachine>(theirStateMachineHolder, in ourSuspensionPoint, ref theirSuspensionPoint);
            return theirStateMachineHolder;
        } else {
            var stateMachineRef = __makeref(theirStateMachine);
            var theirBuilderBox = CoroutineStateMachineAccessor<TStateMachine>.CoroutineMethodBuilderAccessor.GetValue(stateMachineRef);
            ref var theirBuilder = ref Unsafe.Unbox<CoroutineMethodBuilder>(theirBuilderBox);
            var theirStateMachineHolder = theirBuilder.ReplaceCoroutineUnderlyingStateMachine(ref theirStateMachine);
            ourSuspensionPoint._coroutineAwaiter.RenewStateMachineCoroutineAwaiter<TStateMachine>(theirStateMachineHolder, in ourSuspensionPoint, ref theirSuspensionPoint);
            CoroutineStateMachineAccessor<TStateMachine, TResult>.CoroutineMethodBuilderAccessor.SetValue(stateMachineRef, theirBuilderBox);
            return theirStateMachineHolder;
        }
    }

    IAsyncIteratorStateMachineHolder<TResult> IAsyncIteratorStateMachineHolder<TResult>.CreateNewByCloningUnderlyingStateMachine(
        in SuspensionPoint ourSuspensionPoint,
        ref SuspensionPoint theirSuspensionPoint)
    {
        Debug.Assert(StateMachine is not null);
        var theirStateMachine = CoroutineStateMachineAccessor<TStateMachine, TResult>.CloneStateMachine(in StateMachine);
        if (GlobalRuntimeFeature.IsDynamicCodeSupported) {
            ref var theirBuilder = ref CoroutineStateMachineAccessor<TStateMachine, TResult>.CoroutineMethodBuilderAccessor.GetValueReference(ref theirStateMachine);
            var theirStateMachineHolder = theirBuilder.ReplaceCoroutineUnderlyingStateMachine(ref theirStateMachine);
            ourSuspensionPoint._coroutineAwaiter.RenewStateMachineCoroutineAwaiter<TStateMachine>(theirStateMachineHolder, in ourSuspensionPoint, ref theirSuspensionPoint);
            return theirStateMachineHolder;
        } else {
            var stateMachineRef = __makeref(theirStateMachine);
            var theirBuilderBox = CoroutineStateMachineAccessor<TStateMachine, TResult>.CoroutineMethodBuilderAccessor.GetValue(stateMachineRef);
            ref var theirBuilder = ref Unsafe.Unbox<CoroutineMethodBuilder<TResult>>(theirBuilderBox);
            var theirStateMachineHolder = theirBuilder.ReplaceCoroutineUnderlyingStateMachine(ref theirStateMachine);
            ourSuspensionPoint._coroutineAwaiter.RenewStateMachineCoroutineAwaiter<TStateMachine>(theirStateMachineHolder, in ourSuspensionPoint, ref theirSuspensionPoint);
            CoroutineStateMachineAccessor<TStateMachine, TResult>.CoroutineMethodBuilderAccessor.SetValue(stateMachineRef, theirBuilderBox);
            return theirStateMachineHolder;
        }
    }
}

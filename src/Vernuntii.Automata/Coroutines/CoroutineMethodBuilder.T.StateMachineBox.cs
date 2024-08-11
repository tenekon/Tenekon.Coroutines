﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace Vernuntii.Coroutines;

partial struct CoroutineMethodBuilder<T>
{
    /// <summary>The base type for all value task box reusable box objects, regardless of state machine type.</summary>
    internal abstract class CoroutineStateMachineBox : IValueTaskSource<T>, IValueTaskSource, ICoroutineResultStateMachine
    {
        /// <summary>A delegate to the MoveNext method.</summary>
        protected Action? _moveNextAction;

        /// <summary>Captured ExecutionContext with which to invoke MoveNext.</summary>
        internal ExecutionContext? Context;

        /// <summary>Implementation for IValueTaskSource interfaces.</summary>
        protected ManualResetValueTaskSourceCore<T> _valueTaskSource;

        internal CoroutineStateMachineBoxState? State = CoroutineStateMachineBoxState.Default;

        void ICoroutineResultStateMachine.AwaitUnsafeOnCompletedThenContinueWith<TAwaiter>(ref TAwaiter awaiter, Action continuation)
        {
            CoroutineStateMachineBoxState? currentState;
            CoroutineStateMachineBoxState newState;

            do {
                currentState = State;

                if (currentState is null || currentState.HasResult) {
                    throw new InvalidOperationException("Result state machine has already finished");
                }

                newState = new CoroutineStateMachineBoxState(currentState.ForkCount + 1);
            } while (Interlocked.CompareExchange(ref State, newState, currentState)?.ForkCount != currentState.ForkCount);

            awaiter.UnsafeOnCompleted(() => {
                Exception? childError = null;

                try {
                    continuation();
                } catch (Exception error) {
                    childError = error;
                }

                CoroutineStateMachineBoxState? currentState;
                CoroutineStateMachineBoxState? newState;

                do {
                    currentState = State;

                    if (currentState is null) {
                        return;
                    }

                    if (currentState.HasResult) {
                        if (currentState.ForkCount == 1) {
                            newState = null;
                        } else {
                            newState = new CoroutineStateMachineBoxState(currentState.ForkCount - 1, currentState.HasResult, currentState.Result);
                        }
                    } else {
                        if (currentState.ForkCount == 1) {
                            newState = CoroutineStateMachineBoxState.Default;
                        } else {
                            newState = new CoroutineStateMachineBoxState(currentState.ForkCount - 1);
                        }
                    }
                } while (Interlocked.CompareExchange(ref State, newState, currentState)?.ForkCount != currentState.ForkCount);

                if (newState is null) {
                    if (currentState.HasError) {
                        SetExceptionCore(currentState.Error);
                    } else if (childError is not null)
                        SetExceptionCore(childError);
                    else {
                        SetResultCore(currentState.Result);
                    }
                }
            });
        }

        protected void SetExceptionCore(Exception error) =>
            _valueTaskSource.SetException(error);

        protected void SetResultCore(T result) =>
            _valueTaskSource.SetResult(result);

        /// <summary>Completes the box with a result.</summary>
        /// <param name="result">The result.</param>
        public void SetResult(T result)
        {
            CoroutineStateMachineBoxState currentState;
            CoroutineStateMachineBoxState? newState;

            do {
                currentState = State!; // Cannot be null at this state
                newState = currentState.ForkCount == 0 ? null : new CoroutineStateMachineBoxState(currentState.ForkCount, hasResult: true, result);
            } while (Interlocked.CompareExchange(ref State, newState, currentState)!.ForkCount != currentState.ForkCount);

            if (newState is null) {
                SetResultCore(result);
            }
        }

        /// <summary>Completes the box with an error.</summary>
        /// <param name="error">The exception.</param>
        public void SetException(Exception error)
        {
            CoroutineStateMachineBoxState currentState;
            CoroutineStateMachineBoxState? newState;

            do {
                currentState = State!; // Cannot be null at this state
                newState = currentState.ForkCount == 0 ? null : new CoroutineStateMachineBoxState(currentState.ForkCount, hasError: true, error);
            } while (Interlocked.CompareExchange(ref State, newState, currentState)!.ForkCount != currentState.ForkCount);

            if (newState is null) {
                SetExceptionCore(error);
            }
        }

        /// <summary>Gets the status of the box.</summary>
        public ValueTaskSourceStatus GetStatus(short token) => _valueTaskSource.GetStatus(token);

        /// <summary>Schedules the continuation action for this box.</summary>
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _valueTaskSource.OnCompleted(continuation, state, token, flags);

        /// <summary>Gets the current version number of the box.</summary>
        public short Version => _valueTaskSource.Version;

        /// <summary>Implemented by derived type.</summary>
        T IValueTaskSource<T>.GetResult(short token) => throw new NotImplementedException("");

        /// <summary>Implemented by derived type.</summary>
        void IValueTaskSource.GetResult(short token) => throw new NotImplementedException("");
    }

    internal class CoroutineStateMachineBoxState : IEquatable<CoroutineStateMachineBoxState>
    {
        internal readonly static CoroutineStateMachineBoxState Default = new(forkCount: 0, hasResult: false, result: default!);

        public CoroutineStateMachineBoxState(int forkCount)
        {
            ForkCount = forkCount;
            Result = default!;
        }

        public CoroutineStateMachineBoxState(int forkCount, bool hasResult, T result)
        {
            ForkCount = forkCount;
            HasResult = hasResult;
            Result = result;
        }

        public CoroutineStateMachineBoxState(int forkCount, bool hasError, Exception error)
        {
            ForkCount = forkCount;
            Result = default!;
            HasError = hasError;
            Error = error;
        }

        public int ForkCount { get; init; }
        [MemberNotNullWhen(true, nameof(Result))]
        public bool HasResult { get; init; }
        [MemberNotNullWhen(true, nameof(Error))]
        public bool HasError { get; init; }
        public T Result { get; init; }
        public Exception? Error { get; init; }

        public bool Equals(CoroutineStateMachineBoxState? other)
        {
            if (other is null) {
                return false;
            }

            return ForkCount == other.ForkCount &&
                HasResult == other.HasResult;
        }
    }

    /// <summary>Type used as a singleton to indicate synchronous success for an async method.</summary>
    private sealed class SyncSuccessSentinelCoroutineStateMachineBox : CoroutineStateMachineBox
    {
        public SyncSuccessSentinelCoroutineStateMachineBox() => SetResultCore(default!);
    }

    /// <summary>Provides a strongly-typed box object based on the specific state machine type in use.</summary>
    private sealed class CoroutineStateMachineBox<TStateMachine> :
        CoroutineStateMachineBox, IValueTaskSource<T>, IValueTaskSource, ICoroutineStateMachineBox, IThreadPoolWorkItem
        where TStateMachine : IAsyncStateMachine
    {
        /// <summary>Delegate used to invoke on an ExecutionContext when passed an instance of this box type.</summary>
        private static readonly ContextCallback s_callback = ExecutionContextCallback;

        /// <summary>Per-core cache of boxes, with one box per core.</summary>
        /// <remarks>Each element is padded to expected cache-line size so as to minimize false sharing.</remarks>
        private static readonly PaddedReference[] s_perCoreCache = new PaddedReference[Environment.ProcessorCount];

        /// <summary>Thread-local cache of boxes. This currently only ever stores one.</summary>
        [ThreadStatic]
        private static CoroutineStateMachineBox<TStateMachine>? t_tlsCache;

        /// <summary>The state machine itself.</summary>
        public TStateMachine? StateMachine;

        /// <summary>A delegate to the <see cref="MoveNext()"/> method.</summary>
        public Action MoveNextAction => _moveNextAction ??= new Action(MoveNext);

        /// <summary>Gets a box object to use for an operation.  This may be a reused, pooled object, or it may be new.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // only one caller
        internal static CoroutineStateMachineBox<TStateMachine> RentFromCache()
        {
            // First try to get a box from the per-thread cache.
            CoroutineStateMachineBox<TStateMachine>? box = t_tlsCache;
            if (box is not null) {
                t_tlsCache = null;
            } else {
                // If we can't, then try to get a box from the per-core cache.
                ref CoroutineStateMachineBox<TStateMachine>? slot = ref PerCoreCacheSlot;
                if (slot is null ||
                    (box = Interlocked.Exchange(ref slot, null)) is null) {
                    // If we can't, just create a new one.
                    box = new CoroutineStateMachineBox<TStateMachine>();
                }
            }

            return box;
        }

        /// <summary>Returns this instance to the cache.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // only two callers
        private void ReturnToCache()
        {
            // Clear out the state machine and associated context to avoid keeping arbitrary state referenced by
            // lifted locals, and reset the instance for another await.
            ClearStateUponCompletion();
            _valueTaskSource.Reset();

            // If the per-thread cache is empty, store this into it..
            if (t_tlsCache is null) {
                t_tlsCache = this;
            } else {
                // Otherwise, store it into the per-core cache.
                ref CoroutineStateMachineBox<TStateMachine>? slot = ref PerCoreCacheSlot;
                if (slot is null) {
                    // Try to avoid the write if we know the slot isn't empty (we may still have a benign race condition and
                    // overwrite what's there if something arrived in the interim).
                    Volatile.Write(ref slot, this);
                }
            }
        }

        /// <summary>Gets the slot in <see cref="s_perCoreCache"/> for the current core.</summary>
        private static ref CoroutineStateMachineBox<TStateMachine>? PerCoreCacheSlot {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] // only two callers are RentFrom/ReturnToCache
            get {
                // Get the current processor ID.  We need to ensure it fits within s_perCoreCache, so we
                // could % by its length, but we can do so instead by Environment.ProcessorCount, which will be a const
                // in tier 1, allowing better code gen, and then further use uints for even better code gen.
                Debug.Assert(s_perCoreCache.Length == Environment.ProcessorCount, $"{s_perCoreCache.Length} != {Environment.ProcessorCount}");
                int i = (int)((uint)Thread.GetCurrentProcessorId() % (uint)Environment.ProcessorCount);

                // We want an array of StateMachineBox<> objects, each consuming its own cache line so that
                // elements don't cause false sharing with each other.  But we can't use StructLayout.Explicit
                // with generics.  So we use object fields, but always reinterpret them (for all reads and writes
                // to avoid any safety issues) as StateMachineBox<> instances.
#if DEBUG
                object? transientValue = s_perCoreCache[i].Object;
                Debug.Assert(transientValue is null || transientValue is CoroutineStateMachineBox<TStateMachine>,
                    $"Expected null or {nameof(CoroutineStateMachineBox<TStateMachine>)}, got '{transientValue}'");
#endif
                return ref Unsafe.As<object?, CoroutineStateMachineBox<TStateMachine>?>(ref s_perCoreCache[i].Object);
            }
        }

        /// <summary>
        /// Clear out the state machine and associated context to avoid keeping arbitrary state referenced by lifted locals.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearStateUponCompletion()
        {
            StateMachine = default;
            Context = default;
            State = CoroutineStateMachineBoxState.Default;
        }

        /// <summary>
        /// Used to initialize s_callback above. We don't use a lambda for this on purpose: a lambda would
        /// introduce a new generic type behind the scenes that comes with a hefty size penalty in AOT builds.
        /// </summary>
        private static void ExecutionContextCallback(object? s)
        {
            // Only used privately to pass directly to EC.Run
            Debug.Assert(s is CoroutineStateMachineBox<TStateMachine>, $"Expected {nameof(CoroutineStateMachineBox<TStateMachine>)}, got '{s}'");
            Unsafe.As<CoroutineStateMachineBox<TStateMachine>>(s).StateMachine!.MoveNext();
        }

        /// <summary>Invoked to run MoveNext when this instance is executed from the thread pool.</summary>
        void IThreadPoolWorkItem.Execute() => MoveNext();

        /// <summary>Calls MoveNext on <see cref="StateMachine"/></summary>
        public void MoveNext()
        {
            ExecutionContext? context = Context;

            if (context is null) {
                Debug.Assert(StateMachine is not null, $"Null {nameof(StateMachine)}");
                StateMachine.MoveNext();
            } else {
                ExecutionContext.Run(context, s_callback, this);
            }
        }

        /// <summary>Get the result of the operation.</summary>
        T IValueTaskSource<T>.GetResult(short token)
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
    }
}

/// <summary>
/// An interface implemented by all <see cref="AsyncTaskMethodBuilder{TResult}.AsyncStateMachineBox{TStateMachine}"/> instances, regardless of generics.
/// </summary>
internal interface ICoroutineStateMachineBox
{
    /// <summary>Move the state machine forward.</summary>
    void MoveNext();

    /// <summary>
    /// Gets an action for moving forward the contained state machine.
    /// This will lazily-allocate the delegate as needed.
    /// </summary>
    Action MoveNextAction { get; }

    /// <summary>Clears the state of the box.</summary>
    void ClearStateUponCompletion();
}


/// <summary>Internal interface used to enable optimizations from <see cref="AsyncTaskMethodBuilder"/>.</summary>>
internal interface ICoroutineStateMachineBoxAwareAwaiter
{
    /// <summary>Invoked to set <see cref="ITaskCompletionAction.Invoke"/> of the <paramref name="box"/> as the awaiter's continuation.</summary>
    /// <param name="box">The box object.</param>
    void AwaitUnsafeOnCompleted(ICoroutineStateMachineBox box);
}

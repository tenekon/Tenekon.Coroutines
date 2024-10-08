﻿using System.Diagnostics;
using System.Threading.Tasks.Sources;

namespace Tenekon.Coroutines.Sources;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
internal class ManualResetCoroutineCompletionSource<TResult> : IValueTaskSource<TResult>, IValueTaskSource, ICompletionSource<TResult>, ICoroutineCompletionSource
{
    /// <summary>Per-core cache of boxes, with one box per core.</summary>
    /// <remarks>Each element is padded to expected cache-line size so as to minimize false sharing.</remarks>
    private static readonly CacheLineSizePaddedReference[] s_perCoreCache = new CacheLineSizePaddedReference[Environment.ProcessorCount];

    /// <summary>Thread-local cache of boxes. This currently only ever stores one.</summary>
    [ThreadStatic]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "thread-static")]
    private static ManualResetCoroutineCompletionSource<TResult>? t_tlsCache;

    /// <summary>Gets the slot in <see cref="s_perCoreCache"/> for the current core.</summary>
    private static ref ManualResetCoroutineCompletionSource<TResult>? PerCoreCacheSlot {
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
            Debug.Assert(transientValue is null || transientValue is ManualResetCoroutineCompletionSource<TResult>,
                $"Expected null or {nameof(ManualResetCoroutineCompletionSource<TResult>)}, got '{transientValue}'");
#endif
            return ref Unsafe.As<object?, ManualResetCoroutineCompletionSource<TResult>?>(ref s_perCoreCache[i].Object);
        }
    }

    /// <summary>Gets a box object to use for an operation.  This may be a reused, pooled object, or it may be new.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // only one caller
    internal static ManualResetCoroutineCompletionSource<TResult> RentFromCache()
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
                box = new ManualResetCoroutineCompletionSource<TResult>();
            }
        }

        return box;
    }

    ICoroutineCompletionSource ICoroutineCompletionSource.CreateNew(out short token)
    {
        var completionSource = RentFromCache();
        token = completionSource.Version;
        return completionSource;
    }

    /// <summary>Returns this instance to the cache.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // only two callers
    private void ReturnToCache()
    {
        // Clear out the state machine and associated context to avoid keeping arbitrary state referenced by
        // lifted locals, and reset the instance for another await.
        _executionContext = default;
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

    /// <summary>Captured ExecutionContext with which to invoke MoveNext.</summary>
    internal ExecutionContext? _executionContext;

    /// <summary>Implementation for IValueTaskSource interfaces.</summary>
    protected ManualResetValueTaskSourceCore<TResult> _valueTaskSource;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueTask<TResult> CreateGenericValueTask() => new(this, Version);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueTask CreateValueTask() => new(this, Version);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ValueTask<TResult>(ManualResetCoroutineCompletionSource<TResult> completionSource) => new(completionSource, completionSource.Version);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ValueTask(ManualResetCoroutineCompletionSource<TResult> completionSource) => new(completionSource, completionSource.Version);


    /// <summary>Completes the box with a result.</summary>
    /// <param name="result">The result.</param>
    public void SetResult(TResult result) => _valueTaskSource.SetResult(result);

    void ICoroutineCompletionSource.SetResult<TCoroutineResult>(TCoroutineResult result)
    {
        if (result is null) {
            SetResult(default!);
        }

        if (result is not TResult typedResult) {
            throw new ArgumentException($"The result type {typeof(TCoroutineResult)} mismatches the expceted result type {typeof(TResult)}");
        }

        SetResult(typedResult);
    }

    /// <summary>Completes the box with an error.</summary>
    /// <param name="error">The exception.</param>
    public void SetException(Exception error) =>
        _valueTaskSource.SetException(error);

    /// <summary>Gets the status of the box.</summary>
    public ValueTaskSourceStatus GetStatus(short token) => _valueTaskSource.GetStatus(token);

    /// <summary>Schedules the continuation action for this box.</summary>
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
        _valueTaskSource.OnCompleted(continuation, state, token, flags);

    /// <summary>Gets the current version number of the box.</summary>
    public short Version => _valueTaskSource.Version;

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
}

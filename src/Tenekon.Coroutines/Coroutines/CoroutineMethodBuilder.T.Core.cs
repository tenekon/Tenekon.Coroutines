﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using Tenekon.Coroutines.Iterators;

namespace Tenekon.Coroutines;

partial struct CoroutineMethodBuilder<TResult>
{
    internal static CoroutineStateMachineHolder<TResult> CreateWeaklyTyedStateMachineBox() => new CoroutineStateMachineHolder<TResult, IAsyncStateMachine>();

    /// <summary>Gets the "boxed" state machine object.</summary>
    /// <typeparam name="TStateMachine">Specifies the type of the async state machine.</typeparam>
    /// <param name="stateMachine">The state machine.</param>
    /// <param name="stateMachineHolder">A reference to the field containing the initialized state machine box.</param>
    /// <returns>The "boxed" state machine.</returns>
    internal static ICoroutineStateMachineHolder<TResult> GetStateMachineHolder<[DAM(StateMachineMemberTypes)] TStateMachine>(
        ref TStateMachine stateMachine,
        [NotNull] ref CoroutineStateMachineHolder<TResult>? stateMachineHolder)
        where TStateMachine : IAsyncStateMachine
    {
        var currentContext = ExecutionContext.Capture();

        // Check first for the most common case: not the first yield in an async method.
        // In this case, the first yield will have already "boxed" the state machine in
        // a strongly-typed manner into an AsyncStateMachineBox.  It will already contain
        // the state machine as well as a MoveNextDelegate and a context.  The only thing
        // we might need to do is update the context if that's changed since it was stored.
        if (stateMachineHolder is CoroutineStateMachineHolder<TResult, TStateMachine> stronglyTypedHolder) {
            if (stronglyTypedHolder._executionContext != currentContext) {
                stronglyTypedHolder._executionContext = currentContext;
            }

            return stronglyTypedHolder;
        }

        // The least common case: we have a weakly-typed boxed.  This results if the debugger
        // or some other use of reflection accesses a property like ObjectIdForDebugger.  In
        // such situations, we need to get an object to represent the builder, but we don't yet
        // know the type of the state machine, and thus can't use TStateMachine.  Instead, we
        // use the IAsyncStateMachine interface, which all TStateMachines implement.  This will
        // result in a boxing allocation when storing the TStateMachine if it's a struct, but
        // this only happens in active debugging scenarios where such performance impact doesn't
        // matter.
        if (stateMachineHolder is CoroutineStateMachineHolder<TResult, IAsyncStateMachine> weaklyTypedHolder) {
            // If this is the first await, we won't yet have a state machine, so store it.
            if (weaklyTypedHolder.StateMachine is null) {
                Debugger.NotifyOfCrossThreadDependency(); // same explanation as with usage below
                weaklyTypedHolder.StateMachine = stateMachine;
            }

            // Update the context.  This only happens with a debugger, so no need to spend
            // extra IL checking for equality before doing the assignment.
            weaklyTypedHolder._executionContext = currentContext;
            return weaklyTypedHolder;
        }

        // Alert a listening debugger that we can't make forward progress unless it slips threads.
        // If we don't do this, and a method that uses "await foo;" is invoked through funceval,
        // we could end up hooking up a callback to push forward the async method's state machine,
        // the debugger would then abort the funceval after it takes too long, and then continuing
        // execution could result in another callback being hooked up.  At that point we have
        // multiple callbacks registered to push the state machine, which could result in bad behavior.
        Debugger.NotifyOfCrossThreadDependency();

        // At this point, m_task should really be null, in which case we want to create the box.
        // However, in a variety of debugger-related (erroneous) situations, it might be non-null,
        // e.g. if the Task property is examined in a Watch window, forcing it to be lazily-initialized
        // as a Task<TResult> rather than as an ValueTaskStateMachineBox.  The worst that happens in such
        // cases is we lose the ability to properly step in the debugger, as the debugger uses that
        // object's identity to track this specific builder/state machine.  As such, we proceed to
        // overwrite whatever's there anyway, even if it's non-null.
        var typedStateMachineBox = CoroutineStateMachineHolder<TResult, TStateMachine>.RentFromCache();
        stateMachineHolder = typedStateMachineBox; // important: this must be done before storing stateMachine into box.StateMachine!
        typedStateMachineBox.StateMachine = stateMachine;
        typedStateMachineBox._executionContext = currentContext;
        return typedStateMachineBox;
    }

    /// <summary>Gets the "boxed" state machine object.</summary>
    /// <typeparam name="TStateMachine">Specifies the type of the async state machine.</typeparam>
    /// <param name="stateMachine">The state machine.</param>
    /// <param name="stateMachineHolderToRenew">A reference to the field containing the initialized state machine box.</param>
    /// <returns>The "boxed" state machine.</returns>
    internal static IAsyncIteratorStateMachineHolder<TResult> RenewCoroutineStateMachineHolder<[DAM(StateMachineMemberTypes)] TStateMachine>(
        ref TStateMachine stateMachine,
        [NotNull] ref CoroutineStateMachineHolder<TResult>? stateMachineHolderToRenew)
        where TStateMachine : IAsyncStateMachine
    {
        var currentContext = ExecutionContext.Capture();
        var weaklyTypedMachineBox = CoroutineStateMachineHolder<TResult, TStateMachine>.RentFromCache();
        stateMachineHolderToRenew = weaklyTypedMachineBox;
        weaklyTypedMachineBox.StateMachine = stateMachine;
        weaklyTypedMachineBox._executionContext = currentContext;
        return weaklyTypedMachineBox;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref CoroutineStateMachineHolder<TResult> stateMachineHolder)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        // We should never expect an awaiter being a coroutine in this path
        Debug.Assert(awaiter is not IRelativeCoroutine);
        ref var context = ref stateMachineHolder._coroutineContext;

        if (context._isCoroutineAsyncIteratorSupplier) {
            var asyncIteratorContextService = context.GetAsyncIteratorContextService();
            asyncIteratorContextService._currentSuspensionPoint.SupplyAwaiterCompletionNotifier(ref awaiter);
        } else {
            var typedStateMachineBox = GetStateMachineHolder(ref stateMachine, ref stateMachineHolder);
            try {
                awaiter.OnCompleted(typedStateMachineBox.MoveNextAction);
            } catch (Exception e) {
                GlobalScope.ThrowAsync(e, targetContext: null);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref CoroutineStateMachineHolder<TResult> stateMachineHolder)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        ref var context = ref stateMachineHolder._coroutineContext;
        var asyncIteratorContextService = context._isCoroutineAsyncIteratorSupplier ? context.GetAsyncIteratorContextService() : null;
        var isCoroutineAwaiter = CoroutineMethodBuilderCore.ActOnAwaiterIfCoroutineAwaiter(ref awaiter, ref context, asyncIteratorContextService);

        if (context._isCoroutineAsyncIteratorSupplier) {
            Debug.Assert(asyncIteratorContextService is not null);
            asyncIteratorContextService._currentSuspensionPoint.SupplyAwaiterCriticalCompletionNotifier(ref awaiter);
            if (isCoroutineAwaiter && asyncIteratorContextService.IsAsyncIteratorCloneable) {
                // We must box to assist in the complex process of cloning the async iterator at current suspension point on demand
                asyncIteratorContextService._currentSuspensionPoint.SupplyCoroutineAwaiter((IRelativeCoroutineAwaiter)awaiter);
            }
        } else {
            var typedStateMachineBox = GetStateMachineHolder(ref stateMachine, ref stateMachineHolder);
            try {
                awaiter.UnsafeOnCompleted(typedStateMachineBox.MoveNextAction);
            } catch (Exception e) {
                GlobalScope.ThrowAsync(e, targetContext: null);
            }
        }
    }
}

﻿using Tenekon.Coroutines.Iterators;
using System.Runtime.InteropServices;

namespace Tenekon.Coroutines;

public delegate void CoroutineArgumentReceiverDelegate(ref CoroutineArgumentReceiver argumentReceiver);

public readonly ref struct CoroutineArgumentReceiver
{
    internal readonly Span<CoroutineContext> _context;
    private readonly AsyncIteratorContextService? _preKnownAsyncIteratorContextService;

    internal readonly ref CoroutineContext Context {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref MemoryMarshal.GetReference(_context);
    }

    internal CoroutineArgumentReceiver(in CoroutineContext coroutineContext, AsyncIteratorContextService? preKnownAsyncIteratorContextService = null)
    {
        _context = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in coroutineContext), 1);
        _preKnownAsyncIteratorContextService = preKnownAsyncIteratorContextService;
    }

    public void ReceiveCallableArgument<TArgument, TCompletionSource>(in Key argumentKey, in TArgument argument, TCompletionSource completionSource)
        where TArgument : ICallableArgument<TCompletionSource>
        where TCompletionSource : class, ICoroutineCompletionSource
    {
        ref var context = ref Context;

        if (context._isCoroutineAsyncIteratorSupplier) {
            var iteratorContextService = _preKnownAsyncIteratorContextService ?? context.GetAsyncIteratorContextService();
            iteratorContextService._currentSuspensionPoint.SupplyArgument(argumentKey, argument, completionSource);
        } else {
            argument.Callback(in context, completionSource);
        }
    }
}

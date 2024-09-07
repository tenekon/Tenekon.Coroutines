﻿using Vernuntii.Coroutines.Iterators;

namespace Vernuntii.Coroutines;

public delegate void CoroutineArgumentReceiverDelegate(ref CoroutineArgumentReceiver argumentReceiver);

public ref struct CoroutineArgumentReceiver
{
    private ref CoroutineContext _context;

    internal CoroutineArgumentReceiver(ref CoroutineContext coroutineContext)
    {
        _context = ref coroutineContext;
    }

    internal void ReceiveCallableArgument<TArgument>(in Key argumentKey, in TArgument argument, IYieldReturnCompletionSource completionSource)
        where TArgument : ICallableArgument
    {
        if (_context.IsCoroutineAsyncIteratorSupplier) {
            var iteratorContextService = _context.GetAsyncIteratorContextService();
            iteratorContextService.CurrentOperation.SupplyArgument(argumentKey, argument, completionSource);
        } else {
            argument.Callback(in _context);
        }
    }
}

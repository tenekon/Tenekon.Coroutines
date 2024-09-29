using System.Threading.Tasks.Sources;

namespace Tenekon.Coroutines.Iterators;

internal interface IAsyncIteratorStateMachineHolder : ICoroutineStateMachineHolder, IValueTaskSource
{
    short Version { get; }

    IAsyncIteratorStateMachineHolder<VoidCoroutineResult> CreateNewByCloningUnderlyingStateMachine(in SuspensionPoint ourSuspensionPoint, ref SuspensionPoint theirSuspensionPoint);
}

internal interface IAsyncIteratorStateMachineHolder<TResult> : IAsyncIteratorStateMachineHolder, ICoroutineStateMachineHolder<TResult>, IValueTaskSource<TResult>
{
    void SetAsyncIteratorCompletionSource(IValueTaskCompletionSource<TResult>? completionSource);
    void SetResult(TResult result);
    void SetException(Exception e);

    new IAsyncIteratorStateMachineHolder<TResult> CreateNewByCloningUnderlyingStateMachine(in SuspensionPoint ourSuspensionPoint, ref SuspensionPoint theirSuspensionPoint);
}

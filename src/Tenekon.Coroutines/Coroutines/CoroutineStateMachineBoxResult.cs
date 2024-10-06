using Tenekon.Coroutines.Sources;

namespace Tenekon.Coroutines;

internal class CoroutineStateMachineBoxResult<TResult> : IEquatable<CoroutineStateMachineBoxResult<TResult>>
{
    public static readonly CoroutineStateMachineBoxResult<TResult> Default = new();

    public int ForkCount { get; init; }

    [MemberNotNullWhen(true, nameof(Result))]
    public bool IsCompletedSuccessfully {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Status & CoroutineStatus.CompletedSuccessfully) != 0;
    }

    [MemberNotNullWhen(true, nameof(Exception))]
    public bool IsFaulted {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Status & CoroutineStatus.Faulted) != 0;
    }

    public CoroutineStatus Status { get; init; }
    public TResult Result { get; init; }
    public ManualResetCoroutineCompletionSource<VoidCoroutineResult>? CompletionSource { get; }
    public Exception? Exception { get; init; }

    private CoroutineStateMachineBoxResult()
    {
        Result = default!;
    }

    public CoroutineStateMachineBoxResult(int forkCount)
    {
        Result = default!;
        ForkCount = forkCount;
    }

    public CoroutineStateMachineBoxResult(int forkCount, TResult result, ManualResetCoroutineCompletionSource<VoidCoroutineResult>? completionSource)
    {
        ForkCount = forkCount;
        Status = CoroutineStatus.CompletedSuccessfully;
        Result = result;
        CompletionSource = completionSource;
    }

    public CoroutineStateMachineBoxResult(int forkCount, Exception exception, ManualResetCoroutineCompletionSource<VoidCoroutineResult>? completionSource)
    {
        ForkCount = forkCount;
        Status = CoroutineStatus.Faulted;
        Result = default!;
        Exception = exception;
        CompletionSource = completionSource;
    }

    public CoroutineStateMachineBoxResult(CoroutineStateMachineBoxResult<TResult> original, int forkCount)
    {
        ForkCount = forkCount;
        Status = original.Status;
        Result = original.Result;
        Exception = original.Exception;
        CompletionSource = original.CompletionSource;
    }

    public bool Equals(CoroutineStateMachineBoxResult<TResult>? other)
    {
        if (other is null) {
            return false;
        }

        return ForkCount == other.ForkCount && Status == other.Status && !(CompletionSource is null ^ other.CompletionSource is null);
    }

    [Flags]
    internal enum CoroutineStatus : byte
    {
        Running = 0,
        CompletedSuccessfully = 1,
        Faulted = 2,
        Completed = CompletedSuccessfully | Faulted
    }
}

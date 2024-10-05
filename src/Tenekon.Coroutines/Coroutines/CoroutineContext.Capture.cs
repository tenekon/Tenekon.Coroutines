using Tenekon.Coroutines.Sources;

namespace Tenekon.Coroutines;

partial struct CoroutineContext
{
    public static Coroutine<CoroutineContext> Capture()
    {
        var completionSource = ManualResetCoroutineCompletionSource<CoroutineContext>.RentFromCache();
        return new(completionSource, new CaptureArgument(completionSource));
    }

    private class CaptureArgument(ManualResetCoroutineCompletionSource<CoroutineContext> completionSource) : AbstractCoroutineArgumentReceiverAcceptor
    {
        protected override void AcceptCoroutineArgumentReceiver(ref CoroutineArgumentReceiver argumentReceiver) => completionSource.SetResult(argumentReceiver.Context);
    }
}

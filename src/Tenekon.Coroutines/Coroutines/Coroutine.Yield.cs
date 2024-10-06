using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial struct Coroutine
{
    public static Coroutine Yield()
    {
        var completionSource = ManualResetCoroutineCompletionSource<VoidCoroutineResult>.RentFromCache();
        var argument = new YieldArgument(completionSource);
        return new(completionSource, argument);
    }
}

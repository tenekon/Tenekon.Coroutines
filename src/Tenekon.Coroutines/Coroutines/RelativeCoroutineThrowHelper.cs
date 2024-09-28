namespace Tenekon.Coroutines;

internal static class RelativeCoroutineThrowHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InvalidOperationException CannotBePreprocessedTwice() => new("Coroutine can only be preprocessed once");
}

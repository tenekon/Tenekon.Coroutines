using Tenekon.Coroutines.Sources;

namespace Tenekon.Coroutines;

partial class Yielders
{
    partial class Arguments
    {
        public class YieldArgument : ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>, ISiblingCoroutine
        {
            private readonly ManualResetCoroutineCompletionSource<VoidCoroutineResult>? _completionSource;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal YieldArgument(ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource) => _completionSource = completionSource;

            public YieldArgument() { }

            void ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource) =>
                new YieldAwaitable.YieldAwaiter().UnsafeOnCompleted(completionSource.SetDefaultResult);

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in YieldKey, this, _completionSource);

            public override bool Equals([AllowNull] object obj) => obj is YieldArgument;

            public override int GetHashCode() => 0;
        }
    }
}

using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    public static Coroutine Throw(Exception exception)
    {
        var completionSource = ManualResetCoroutineCompletionSource<VoidCoroutineResult>.RentFromCache();
        var argument = new ThrowArgument(exception, completionSource);
        return new(completionSource, argument);
    }

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct ThrowArgumentCore(Exception exception, ManualResetCoroutineCompletionSource<VoidCoroutineResult>? completionSource)
        {
            internal readonly ManualResetCoroutineCompletionSource<VoidCoroutineResult>? _completionSource = completionSource;

            public readonly Exception Exception = exception;

            public bool Equals(in ThrowArgumentCore other) => Equals(Exception, other.Exception);

            public override bool Equals([AllowNull] object obj) => throw new NotImplementedException();

            public readonly override int GetHashCode() => Exception.GetHashCode();
        }

        public class ThrowArgument : ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>, ISiblingCoroutine
        {
            private readonly ThrowArgumentCore _core;

            public Exception Exception {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Exception;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ThrowArgument(Exception exception, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource) => _core = new(exception, completionSource);

            public ThrowArgument(Exception exception) => _core = new(exception, completionSource: null);

            public ThrowArgument() { }

            void ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource) =>
                completionSource.SetException(_core.Exception);

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, in ThrowKey, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is ThrowArgument other && _core.Equals(in other._core);

            public override int GetHashCode() => 0;
        }
    }
}

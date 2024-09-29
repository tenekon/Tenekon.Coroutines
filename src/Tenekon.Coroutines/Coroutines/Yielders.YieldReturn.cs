using System.Diagnostics;
using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    public static Coroutine YieldReturn<T>(T value)
    {
        var completionSource = ManualResetCoroutineCompletionSource<VoidCoroutineResult>.RentFromCache();
        var argument = new YieldReturnArgument<T>(value, completionSource);
        return new Coroutine(completionSource, argument);
    }

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct YieldReturnArgumentCore<T>(T value, ManualResetCoroutineCompletionSource<VoidCoroutineResult>? completionSource)
        {
            internal readonly ManualResetCoroutineCompletionSource<VoidCoroutineResult>? _completionSource = completionSource;

            public readonly T Value = value;

            public bool Equals(YieldReturnArgumentCore<T> other) => Equals(Value, other.Value);

            public readonly override int GetHashCode() => HashCode.Combine(Value);
        }

        public class YieldReturnArgument<T> : ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>, ISiblingCoroutine
        {
            private readonly YieldReturnArgumentCore<T> _core;

            public T Value {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal YieldReturnArgument(T value, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource) => _core = new(value, completionSource);

            public YieldReturnArgument(T value) => _core = new(value, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<VoidCoroutineResult>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<VoidCoroutineResult> completionSource) =>
                completionSource.SetDefaultResult();

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver)
            {
                Debug.Assert(_core._completionSource is not null);
                argumentReceiver.ReceiveCallableArgument(in YieldReturnVariantKey, this, _core._completionSource);
            }

            public override bool Equals([AllowNull] object obj) => obj is YieldReturnArgument<T> other && _core.Equals(other._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

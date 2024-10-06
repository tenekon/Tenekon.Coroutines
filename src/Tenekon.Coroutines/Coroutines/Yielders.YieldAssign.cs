using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    public readonly struct YieldAssign<TYield>(TYield value)
    {
        public readonly Coroutine<TAssign> Assign<TAssign>()
        {
            var completionSource = ManualResetCoroutineCompletionSource<TAssign>.RentFromCache();
            var argument = new YieldReturnArgument<TYield, TAssign>(value, completionSource);
            return new(completionSource, argument);
        }
    }

    public static YieldAssign<TYield> Yield<TYield>(TYield value) => new(value);

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct YieldReturnArgumentCore<TYield, TReturn>(TYield value, ManualResetCoroutineCompletionSource<TReturn>? completionSource) : IEquatable<YieldReturnArgumentCore<TYield, TReturn>>
        {
            internal readonly ManualResetCoroutineCompletionSource<TReturn>? _completionSource = completionSource;

            public readonly TYield Value = value;

            public bool Equals(YieldReturnArgumentCore<TYield, TReturn> other) => Equals(Value, other.Value);

            public override bool Equals([AllowNull] object obj) => throw new NotImplementedException();

            public readonly override int GetHashCode() => HashCode.Combine(Value);
        }

        public class YieldReturnArgument<TYield, TAssign> : ICallableArgument<ManualResetCoroutineCompletionSource<TAssign>>, ISiblingCoroutine
        {
            private readonly YieldReturnArgumentCore<TYield, TAssign> _core;

            public TYield Value {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal YieldReturnArgument(TYield value, ManualResetCoroutineCompletionSource<TAssign> completionSource) => _core = new(value, completionSource);

            public YieldReturnArgument(TYield value) => _core = new(value, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<TAssign>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<TAssign> completionSource) =>
                completionSource.SetDefaultResult();

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver) => ActOnCoroutine(ref argumentReceiver, YieldAssign, this, _core._completionSource);

            public override bool Equals([AllowNull] object obj) => obj is YieldReturnArgument<TYield, TAssign> other && _core.Equals(other._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

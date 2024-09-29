using System.Diagnostics;
using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    public readonly struct YieldReturnQuery<TYield>(TYield value)
    {
        public readonly Coroutine<TReturn> Return<TReturn>()
        {
            var completionSource = ManualResetCoroutineCompletionSource<TReturn>.RentFromCache();
            var argument = new YieldReturnArgument<TYield, TReturn>(value, completionSource);
            return new Coroutine<TReturn>(completionSource, argument);
        }
    }

    public static YieldReturnQuery<TValue> Yield<TValue>(TValue value) => new(value);

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct YieldReturnArgumentCore<TYield, TReturn>(TYield value, ManualResetCoroutineCompletionSource<TReturn>? completionSource) : IEquatable<YieldReturnArgumentCore<TYield, TReturn>>
        {
            internal readonly ManualResetCoroutineCompletionSource<TReturn>? _completionSource = completionSource;

            public readonly TYield Value = value;

            public bool Equals(YieldReturnArgumentCore<TYield, TReturn> other) => Equals(Value, other.Value);

            public readonly override int GetHashCode() => HashCode.Combine(Value);
        }

        public class YieldReturnArgument<TYield, TReturn> : ICallableArgument<ManualResetCoroutineCompletionSource<TReturn>>, ISiblingCoroutine
        {
            private readonly YieldReturnArgumentCore<TYield, TReturn> _core;

            public TYield Value {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal YieldReturnArgument(TYield value, ManualResetCoroutineCompletionSource<TReturn> completionSource) => _core = new(value, completionSource);

            public YieldReturnArgument(TYield value) => _core = new(value, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<TReturn>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<TReturn> completionSource) =>
                completionSource.SetDefaultResult();

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver)
            {
                Debug.Assert(_core._completionSource is not null);
                argumentReceiver.ReceiveCallableArgument(in YieldReturnVariantKey, this, _core._completionSource);
            }

            public override bool Equals([AllowNull] object obj) => obj is YieldReturnArgument<TYield, TReturn> other && _core.Equals(other._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

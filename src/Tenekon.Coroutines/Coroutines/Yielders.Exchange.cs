using System.Diagnostics;
using Tenekon.Coroutines.Sources;
using static Tenekon.Coroutines.Yielders.Arguments;

namespace Tenekon.Coroutines;

partial class Yielders
{
    public static Coroutine<T> Exchange<T>(T value)
    {
        var completionSource = ManualResetCoroutineCompletionSource<T>.RentFromCache();
        var argument = new ExchangeArgument<T>(value, completionSource);
        return new Coroutine<T>(completionSource, argument);
    }

    partial class Arguments
    {
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly struct ExchangeArgumentCore<T>(T value, ManualResetCoroutineCompletionSource<T>? completionSource) : IEquatable<ExchangeArgumentCore<T>>
        {
            internal readonly ManualResetCoroutineCompletionSource<T>? _completionSource = completionSource;

            public readonly T Value = value;

            public bool Equals(ExchangeArgumentCore<T> other) => Equals(Value, other.Value);

            public readonly override int GetHashCode() => HashCode.Combine(Value);
        }

        public class ExchangeArgument<T> : ICallableArgument<ManualResetCoroutineCompletionSource<T>>, ISiblingCoroutine
        {
            private readonly ExchangeArgumentCore<T> _core;

            public T Value {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _core.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ExchangeArgument(T value, ManualResetCoroutineCompletionSource<T> completionSource) => _core = new(value, completionSource);

            public ExchangeArgument(T value) => _core = new(value, completionSource: null);

            void ICallableArgument<ManualResetCoroutineCompletionSource<T>>.Callback(in CoroutineContext context, ManualResetCoroutineCompletionSource<T> completionSource) =>
                completionSource.SetResult(Value);

            void ISiblingCoroutine.ActOnCoroutine(ref CoroutineArgumentReceiver argumentReceiver)
            {
                Debug.Assert(_core._completionSource is not null);
                argumentReceiver.ReceiveCallableArgument(in ExchangeKey, this, _core._completionSource);
            }

            public override bool Equals([AllowNull] object obj) => obj is ExchangeArgument<T> other && _core.Equals(other._core);

            public override int GetHashCode() => _core.GetHashCode();
        }
    }
}

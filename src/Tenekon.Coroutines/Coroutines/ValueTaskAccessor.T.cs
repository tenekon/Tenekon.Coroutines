using System.Runtime.InteropServices;

namespace Tenekon.Coroutines;

[StructLayout(LayoutKind.Auto)]
internal struct ValueTaskAccessor<TResult> : IValueTaskAccessor
{
    internal object? _obj;
    [AllowNull] internal readonly TResult _result;
    internal short _token;
    internal readonly bool _continueOnCapturedContext;

    object? IValueTaskAccessor._obj {
        readonly get => _obj;
        set => _obj = value;
    }

    short IValueTaskAccessor._token {
        readonly get => _token;
        set => _token = value;
    }
}

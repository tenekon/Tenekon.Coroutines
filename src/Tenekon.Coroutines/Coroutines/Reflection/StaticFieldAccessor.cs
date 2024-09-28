using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Tenekon.Coroutines.Reflection;

delegate ref TField GetStaticValueDelegate<T, TField>();

internal class StaticFieldAccessor<T, TField>
{
    private readonly Type _fieldOwnerType;
    private readonly Type _fieldType;
    private readonly FieldInfo _fieldInfo;
    private readonly GetStaticValueDelegate<T, TField>? _getValueReferenceDelegate;

    [UnconditionalSuppressMessage(IL3050RequiresDynamicCodeCategory, IL3050RequiresDynamicCode, Justification = IL3050RequiresDynamicCodeJustfication)]
    internal StaticFieldAccessor(Type fieldOwnerType, Type fieldType, FieldInfo fieldInfo)
    {
        _fieldOwnerType = fieldOwnerType;
        _fieldType = fieldType;
        _fieldInfo = fieldInfo;
        if (GlobalRuntimeFeature.IsDynamicCodeSupported) {
            _getValueReferenceDelegate = CompileGetValueDelegate(_fieldInfo);
        }
    }

    internal GetStaticValueDelegate<T, TField> GetValueReference {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _getValueReferenceDelegate ?? throw new InvalidOperationException("Runtime does not support emitting dynamic code");
    }

    [RequiresDynamicCode($"Use {nameof(GetValue)}/{nameof(SetValue)} instead")]
    private GetStaticValueDelegate<T, TField> CompileGetValueDelegate(FieldInfo coroutineAwaiterFieldInfo)
    {
        var method = new DynamicMethod(
            nameof(GetValueReference),
            returnType: _fieldType.MakeByRefType(),
            parameterTypes: [_fieldOwnerType.MakeByRefType()],
            restrictedSkipVisibility: true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldflda, coroutineAwaiterFieldInfo); // Load address of the field into the stack
        il.Emit(OpCodes.Ret); // Return the address of the field (by reference)
        return Unsafe.As<GetStaticValueDelegate<T, TField>>(method.CreateDelegate(typeof(GetStaticValueDelegate<T, TField>)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object GetValue()
    {
        Debug.Assert(_fieldInfo is not null);
        return _fieldInfo.GetValue(null)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetValue(object value)
    {
        Debug.Assert(_fieldInfo is not null);
        _fieldInfo.SetValue(null, value);
    }
}

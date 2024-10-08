﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using K4os.Hash.xxHash;

namespace Tenekon.Coroutines;

internal enum KeyFlags : byte
{
    None = 0,
    ContextService = 1,
    Service = 2,
    Inheritable = 4,
}

// 0.-3. (4 bytes) -> hash
// 4. (1 bytes) -> schema version
// 5. (1 bytes) -> flags
// 6.-19. (12 bytes) -> scope
// 18.-19. (2 bytes) -> argument
// 6.-19. (14 bytes) -> service
[StructLayout(LayoutKind.Explicit, Pack = 1)]
[DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
public struct Key : IEquatable<Key>
{
    private static readonly XXH32 s_hash = new();

    internal const byte CurrentSchemaVersion = 1;
    internal const int KeyLength = 20;
    internal const int ScopeLength = 12;
    internal const int ArgumentLength = 2;
    internal const int ServiceLength = 14;

    [FieldOffset(0)]
    internal readonly uint _hash;

    [field: FieldOffset(4)]
    public readonly byte SchemaVersion { get; } = CurrentSchemaVersion;

    [FieldOffset(5)]
    internal readonly byte _flags;

    [FieldOffset(6)]
    private unsafe fixed byte _scope[ScopeLength];

    [FieldOffset(18)]
    private ushort _argument;

    [FieldOffset(6)]
    private unsafe fixed byte _service[ServiceLength];

    public unsafe Key(Span<byte> scope, ushort argument)
    {
        if (scope.Length > ScopeLength) {
            throw new ArgumentOutOfRangeException(nameof(scope), $"Scope must be {ScopeLength} bytes or lesser");
        }

        fixed (byte* scopePointer = _scope) {
            var scopeSpan = new Span<byte>(scopePointer, ScopeLength);
            scope.CopyTo(scopeSpan);
        }

        _argument = argument;
        ComputeHash(out _hash);
    }

    public unsafe Key(byte[] scope, ushort argument) : this(scope.AsSpan(), argument)
    {
    }

    internal unsafe Key(Span<byte> service, bool isContextService = false, bool inheritable = false)
    {
        if (service.Length > ServiceLength) {
            throw new ArgumentOutOfRangeException(nameof(service), $"Scope must be {ServiceLength} bytes or lesser");
        }


        _flags = (byte)((isContextService ? KeyFlags.ContextService : KeyFlags.Service) | (inheritable ? KeyFlags.Inheritable : KeyFlags.None));

        fixed (byte* servicePointer = _service) {
            var serviceSpan = new Span<byte>(servicePointer, ServiceLength);
            service.CopyTo(serviceSpan);
        }

        ComputeHash(out _hash);
    }

    public unsafe Key(string service, bool inheritable = false) : this(Encoding.ASCII.GetBytes(service), isContextService: false, inheritable: false)
    {
    }

    public unsafe Key(Span<byte> service, bool inheritable) : this(service, isContextService: false, inheritable: inheritable)
    {
    }

    private unsafe void ComputeHash(out uint hash)
    {
        fixed (byte* thisPointer = &Unsafe.As<Key, byte>(ref this)) {
#pragma warning disable CS0618 // Type or member is obsolete
            s_hash.Update(thisPointer, 2);
#pragma warning restore CS0618 // Type or member is obsolete
        }
        hash = s_hash.Digest();
        s_hash.Reset();
    }

    internal unsafe bool SequenceEqual(in Key other)
    {
        fixed (byte* thisPointer = &Unsafe.As<Key, byte>(ref this)) {
            fixed (byte* otherPointer = &Unsafe.As<Key, byte>(ref Unsafe.AsRef(in other))) {
                for (var i = 0; i < KeyLength; i++) {
                    if (thisPointer[i] != otherPointer[i]) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public bool Equals(Key other) => SequenceEqual(other);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Key key && SequenceEqual(key);

    public static bool operator ==(Key left, Key right) => left.Equals(right);

    public static bool operator !=(Key left, Key right) => !(left == right);

    public override readonly int GetHashCode() => (int)_hash;

    public override unsafe string ToString()
    {
        var isService = (_flags & (byte)(KeyFlags.Service | KeyFlags.ContextService)) != 0;

        var sb = new StringBuilder();
        sb.Append("Key{v");
        sb.Append(SchemaVersion);
        sb.Append('/');

        if (isService) {
            fixed (byte* servicePointer = _service) {
                var serviceSpan = new Span<byte>(servicePointer, ServiceLength);
                sb.Append(Encoding.ASCII.GetString(serviceSpan).Replace("\0", "\\0"));
            }
        } else {
            fixed (byte* scopePointer = _scope) {
                var scopeSpan = new Span<byte>(scopePointer, ServiceLength);
                sb.Append(Encoding.ASCII.GetString(scopeSpan).Replace("\0", "\\0"));
                sb.Append('#');
                sb.Append(_argument);
            }
        }

        sb.Append('}');
        return sb.ToString();
    }
}

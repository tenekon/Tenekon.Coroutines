﻿/*
MIT License

Copyright (c) 2019
Copyright (c) 2024 Tenekon authors and contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Tenekon.Collections;

internal delegate bool ReadonlyReferenceEqualsDelegate<T>(in T? x, in T? y);
internal delegate uint ReadonlyReferenceGetHashCodeDelegate<T>([DisallowNull] in T obj);

/// <summary>
/// This hashmap uses the following
/// - Open addressing
/// - Uses linear probing
/// - Robinghood hashing
/// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
/// - Keeps track of the currentProbeCount which makes sure we can back out early eventhough the maxprobcount exceeds the cpc
/// - fibonacci hashing
/// </summary>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class AbstractRobinHoodHashMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    public const double DefaultLoadFactor = 0.5;

    private const uint GoldenRatio = 0x9E3779B9; // 2654435769;
    private const int DefaultShiftToSubtractFrom = 32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref Entry Find(Entry[] array, uint index)
    {
#if NETSTANDARD2_1
        return ref array[index];
#else
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref byte Find(byte[] array, uint index)
    {

#if NETSTANDARD2_1
        return ref array[index];
#else
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#endif
    }

    /// <summary>
    /// Gets or sets how many elements are stored in the map
    /// </summary>
    /// <value>
    /// 
    /// The EntryTwo count.
    /// </value>
    public int Count { get => _count; private set => _count = value; }

    /// <summary>
    /// Gets the size of the map
    /// </summary>
    /// <value>
    /// The size.
    /// </value>
    public int Size => _entries.Length;

    /// <summary>
    /// Returns all the entries as KeyValuePair objects
    /// </summary>
    /// <value>
    /// The entries.
    /// </value>
    public IEnumerable<KeyValuePair<TKey, TValue>> Entries {
        get {
            // Iterate backwards so we can remove the current item
            for (var i = _meta.Length - 1; i >= 0; --i) {
                var meta = _meta[i];
                if (meta is not 0) {
                    yield return new KeyValuePair<TKey, TValue>(_entries[i].Key, _entries[i].Value);
                }
            }
        }
    }

    /// <summary>
    /// Returns all keys
    /// </summary>
    /// <value>
    /// The keys.
    /// </value>
    public IEnumerable<TKey> Keys {
        get {
            // Iterate backwards so we can remove the current item
            for (var i = _meta.Length - 1; i >= 0; --i) {
                var meta = _meta[i];
                if (meta > 0) {
                    yield return _entries[i].Key;
                }
            }
        }
    }

    /// <summary>
    /// Returns all Values
    /// </summary>
    /// <value>
    /// The keys.
    /// </value>
    public IEnumerable<TValue> Values {
        get {
            for (var i = _meta.Length - 1; i >= 0; --i) {
                var meta = _meta[i];
                if (meta is not 0) {
                    yield return _entries[i].Value;
                }
            }
        }
    }

    internal byte[] _meta;
    internal Entry[] _entries;
    private uint _length;
    private readonly double _loadFactor;
    private byte _shift;
    private byte _maxProbeSequenceLength;
    private readonly ReadonlyReferenceEqualsDelegate<TKey> _keyEqualityComparer;
    private readonly ReadonlyReferenceGetHashCodeDelegate<TKey> _keyHashCodeProvider;
    private int _maxLookupsBeforeResize;
    private int _count;

    /// <summary>
    /// Initializes a new instance of class.
    /// </summary>
    /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
    /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
    /// <param name="keyEqualityComparer">Used to compare keys to resolve hashcollisions</param>
    internal AbstractRobinHoodHashMap(
        uint length,
        double loadFactor,
        ReadonlyReferenceEqualsDelegate<TKey> keyEqualityComparer,
        ReadonlyReferenceGetHashCodeDelegate<TKey> keyHashCodeProvider)
    {
        _length = BitOperations.RoundUpToPowerOf2(length);
        _loadFactor = loadFactor;

        if (length < 4) {
            _length = 4;
        }

        _maxProbeSequenceLength = (byte)BitOperations.Log2(_length);
        _maxLookupsBeforeResize = (int)((_length + _maxProbeSequenceLength) * loadFactor);
        _keyEqualityComparer = keyEqualityComparer;
        _keyHashCodeProvider = keyHashCodeProvider;
        _shift = (byte)(DefaultShiftToSubtractFrom - BitOperations.Log2(_length));

        var size = (int)_length + _maxProbeSequenceLength;

        /* Pooling these arrays via FixedSizedPooling is only beneficial when exceeding following conditions
         * Entry (e.g. Entry<Key,object>) byte size >= 24 & array size >= 64 & runs >= 100_000
         * (TBD)
         */

        // We cannot use AllocateUninitializedArray because the performance benefical path is only taken,
        // if TKey or TValue are not or does not contain a reference type and the total size in bytes of that array is greater than 2048.
        _entries = new Entry[size];
        // Since we must not create an pinned array but also require the array to be zero-initialized, we cannot use AllocateArray/AllocateUninitializedArray either.
        _meta = new byte[size];
    }

    /// <summary>
    /// Gets or sets the value by using a Tkey
    /// </summary>
    /// <value>
    /// The 
    /// </value>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException">
    /// Unable to find Entry - {key.GetType().FullName} key - {key.GetHashCode()}
    /// or
    /// Unable to find EntryTwo - {key.GetType().FullName} key - {key.GetHashCode()}
    /// </exception>
    public TValue this[in TKey key] {
        get {
            if (TryGetValue(in key, out var result)) {
                return result;
            }

            throw Exceptions.KeyNotFound(key);
        }

        set => SetOrReplace(in key, value);
    }

    /// <summary>
    /// Inserts the specified value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool SetOrReplace(in TKey key, TValue value)
    {
        //Resize if loadfactor is reached
        if (_count >= _maxLookupsBeforeResize) {
            Resize();
        }

        var index = Hash(in key);

        byte distance = 1;
        var entry = new Entry(key, value);

        do {
            ref var meta = ref Find(_meta, index);

            //Empty spot, add Entry
            if (meta == 0) {
                meta = distance;
                Find(_entries, index) = entry;
                ++_count;
                return true;
            }

            //Steal from the rich, give to the poor
            if (distance > meta) {
                Swap(ref distance, ref meta);
                Swap(ref entry, ref Find(_entries, index));

                ++distance;
                ++index;
                continue;
            }

            //equals check
            if (_keyEqualityComparer(in key, Find(_entries, index).Key)) {
                return false;
            }

            //increase probe sequence length
            ++distance;
            ++index;
        } while (true);
    }

    /// <summary>
    /// Gets the value with the corresponding key
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(in TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var index = Hash(in key);
        var maxDistance = index + _maxProbeSequenceLength;

        do {
            ref var entry = ref Find(_entries, index);

            if (_keyEqualityComparer(in entry.Key, key)) {
                value = entry.Value;
                return true;
            }

        } while (++index < maxDistance);


        value = default;
        //Entry not found
        return false;
    }

    /// <summary>
    /// Gets the value for the specified key, or, if the key is not present,
    /// adds an entry and returns the value by ref. This makes it possible to
    /// add or update a value in a single look up operation.
    ///
    /// Will only use one lookup instead of two
    ///
    /// * Example *
    ///
    /// var counterMap = new RobinhoodMap<uint, uint>(16, 0.5);
    /// ref var counter = ref counterMap.GetOrUpdate(1);
    ///
    /// ++counter;
    /// 
    /// </summary>
    /// <param name="key">Key to look for</param>
    /// <returns>Reference to the existing value</returns>    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue GetOrUpdate(in TKey key)
    {
        //Resize if loadfactor is reached
        if (_count >= _maxLookupsBeforeResize) {
            Resize();
        }

        var index = Hash(in key);
        var entry = new Entry(key, default!);
        byte distance = 1;

        do {
            ref var meta = ref Find(_meta, index);

            //Empty spot, add Entry
            if (meta == 0) {
                meta = distance;
                ref var x = ref Find(_entries, index);
                x = entry;

                ++_count;
                return ref x.Value;
            }

            //Steal from the rich, give to the poor
            if (distance > meta) {
                Swap(ref distance, ref meta);
                Overwrite(in entry, ref Find(_entries, index));
                goto next;
            }

            //equals check
            if (_keyEqualityComparer(key, Find(_entries, index).Key)) {
                return ref Find(_entries, index).Value;
            }

            next:
            //increase probe sequence length
            ++distance;
            ++index;
        } while (true);
    }

    public void Add(in TKey key, TValue value) => SetOrReplace(in key, value);

    /// <summary>
    /// Updates the value of a specific key
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Update(in TKey key, TValue value)
    {
        var index = Hash(in key);
        var maxDistance = index + _maxProbeSequenceLength;

        do {
            ref var entry = ref Find(_entries, index);

            if (_keyEqualityComparer(in entry.Key, key)) {
                entry.Value = value;
                return true;
            }

        } while (++index < maxDistance);

        //Entry not found
        return false;
    }

    /// <summary>
    ///  Remove EntryTwo with a backshift removal
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        var index = Hash(key);
        var maxDistance = index + _maxProbeSequenceLength;

        do {
            //validate hash en compare keys
            if (_keyEqualityComparer(key, Find(_entries, index).Key)) {
                var nextIndex = index + 1;
                var nextMeta = Find(_meta, nextIndex);

                while (nextMeta > 1) {
                    //decrease next psl by 1
                    nextMeta--;

                    Find(_meta, index) = nextMeta;
                    Find(_entries, index) = Find(_entries, nextIndex);

                    index++;
                    nextIndex++;

                    //increase index by one
                    nextMeta = Find(_meta, nextIndex);
                }

                Find(_meta, index) = default;
                Find(_entries, index) = default;

                --_count;
                return true;
            }

            ++index;
            //increase index by one and validate if within bounds
        } while (index < maxDistance);

        // No entries removed
        return false;
    }

    /// <summary>
    /// Determines whether the specified key contains key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>
    ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(in TKey key)
    {
        var index = Hash(in key);
        var maxDistance = index + _maxProbeSequenceLength;

        do {
            var entry = Find(_entries, index);
            if (_keyEqualityComparer(in entry.Key, key)) {
                return true;
            }

        } while (++index < maxDistance);

        //not found
        return false;
    }

    /// <summary>
    /// Copies entries from one map to another
    /// </summary>
    /// <param name="denseMap">The map.</param>
    public void CopyFrom(AbstractRobinHoodHashMap<TKey, TValue> denseMap)
    {
        for (var i = 0; i < denseMap._entries.Length; ++i) {
            var meta = denseMap._meta[i];
            if (meta is 0) {
                continue;
            }

            SetOrReplace(denseMap._entries[i].Key, denseMap._entries[i].Value);
        }
    }

    /// <summary>
    /// Clears this instance.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_entries, 0, _entries.Length);
        Array.Clear(_meta, 0, _entries.Length);
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal uint Hash(in TKey key)
    {
        var hashcode = _keyHashCodeProvider(key);
        return (GoldenRatio * hashcode) >> _shift;
    }

    /// <summary>
    /// Overwrites the entry without checking upper bound and without increasing <see cref="Count"/>.
    /// </summary>
    /// <param name="entry"></param>
    private void CopyFrom(ref Entry entry)
    {
        var index = Hash(entry.Key);
        byte distance = 1;

        do {
            ref var meta = ref Find(_meta, index);

            //Empty spot, add Entry
            if (meta == 0) {
                meta = distance;
                Find(_entries, index) = entry;
                return;
            }

            //Steal from the rich, give to the poor
            if (distance > meta) {
                Swap(ref distance, ref meta);
                Overwrite(in entry, ref Find(_entries, index));
            }

            //increase probe sequence length
            ++distance;
            ++index;
        } while (true);
    }

    /// <summary>
    /// Swaps the specified x.
    /// </summary>
    /// <param name="from">The x.</param>
    /// <param name="to">The y.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap<T>(ref T x, ref T y) => (x, y) = (y, x);

    /// <summary>
    /// Swaps the specified x.
    /// </summary>
    /// <param name="from">The x.</param>
    /// <param name="to">The y.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Overwrite<T>(in T from, ref T to) => to = from;

    /// <summary>
    /// Resizes this instance.
    /// </summary>
    private void Resize()
    {
        _length <<= 1;
        _shift--;

        _maxProbeSequenceLength = (byte)BitOperations.Log2(_length);
        _maxLookupsBeforeResize = (int)((_length + _maxProbeSequenceLength) * _loadFactor);

        var size = Unsafe.As<uint, int>(ref _length) + _maxProbeSequenceLength;

        var oldEntries = _entries;
        var oldMeta = _meta;

        _entries = new Entry[size];
        _meta = new byte[size];

        for (var i = 0u; i < oldMeta.Length; ++i) {
            if (oldMeta[i] == 0) {
                continue;
            }

            CopyFrom(ref Find(oldEntries, i));
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();

    [DebuggerDisplay("{Key} {Value}")]
    [StructLayout(LayoutKind.Sequential)]
    public struct Entry(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;

        public override readonly string ToString() => $$"""Entry{Key={{Key}}, Value={{Value}}}""";
    }

    private static class Exceptions
    {
        internal static KeyNotFoundException KeyNotFound(TKey key) => new($"Unable to find the entry for key {key} (Hash Code = {key?.GetHashCode()})");
    }
}

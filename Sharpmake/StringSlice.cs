// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sharpmake;

[DebuggerDisplay("{Value}")]
public readonly struct StringSlice : IEquatable<StringSlice>
{
    private readonly string _value;

    public static readonly StringSlice Empty = new(string.Empty);

    public int Start { get; }

    public int Length { get; }

    public StringSlice(string value)
    {
        _value = value;
        Start = 0;
        Length = value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringSlice(string value, int start, int length)
    {
        _value = value ?? string.Empty;
        Start = start;
        Length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsSpan() => _value.AsSpan(Start, Length);

    public ReadOnlySpan<char> AsSpan(int start, int length)
    {
        if (_value.Length == 0 || start < 0 || length < 0 || (uint)(start + length) > (uint)Length)
        {
            if (_value.Length == 0 || start < 0) throw new ArgumentOutOfRangeException(nameof(start));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            throw new ArgumentException();
        }

        return _value.AsSpan(Start + start, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => string.GetHashCode(AsSpan());

    public bool Equals(StringSlice other) => Equals(other, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(StringSlice other, StringComparison comparisonType)
    {
        if (_value.Length > 0 && other._value.Length > 0)
        {
            return AsSpan().Equals(other.AsSpan(), comparisonType);
        }

        return _value.Length == 0 && other._value.Length == 0;
    }

    public override bool Equals(object obj)
    {
        return obj is StringSlice other && Equals(other);
    }

    public int IndexOf(char c) => IndexOf(c, 0, Length);
    public int IndexOf(char c, int start) => IndexOf(c, start, Length - start);
    public int IndexOf(char c, int start, int count)
    {
        int index = -1;

        if (_value.Length > 0)
        {
            if ((uint)start > (uint)Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if ((uint)count > (uint)(Length - start))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            index = AsSpan(start, count).IndexOf(c);
            if (index >= 0)
            {
                index += start;
            }
        }

        return index;
    }

    public StringSlice Slice(int start)
    {
        // TODO: [hpintoribeiro] validate

        return new(_value, Start + start, Length - start);
    }

    public StringSlice Slice(int start, int length)
    {
        // TODO: [hpintoribeiro] validate

        return new(_value, Start + start, length);
    }

    public override string ToString() => _value.Length == 0 ? string.Empty : _value.Substring(Start, Length);

    public bool IsEmpty => Length == 0;
    public bool IsWhitespace => AsSpan().IsWhiteSpace();
}

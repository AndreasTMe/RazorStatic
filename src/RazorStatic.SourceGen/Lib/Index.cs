﻿// ReSharper disable once CheckNamespace

namespace System;

public readonly struct Index : IEquatable<Index>
{
    private readonly int _value;

    public Index(int value, bool fromEnd = false)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");
        }

        if (fromEnd)
            _value = ~value;
        else
            _value = value;
    }

    private Index(int value) => _value = value;

    public static Index Start => new Index(0);

    public static Index End => new Index(~0);

    public static Index FromStart(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");
        }

        return new Index(value);
    }

    public static Index FromEnd(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");
        }

        return new Index(~value);
    }

    public int Value
    {
        get
        {
            if (_value < 0)
                return ~_value;
            else
                return _value;
        }
    }

    public bool IsFromEnd => _value < 0;

    public int GetOffset(int length)
    {
        int offset = _value;
        if (IsFromEnd)
        {
            // offset = length - (~value)
            // offset = length + (~(~value) + 1)
            // offset = length + value + 1

            offset += length + 1;
        }
        return offset;
    }

    public override bool Equals(object? value) => value is Index && _value == ((Index)value)._value;

    public bool Equals(Index other) => _value == other._value;

    public override int GetHashCode() => _value;

    public static implicit operator Index(int value) => FromStart(value);

    public override string ToString()
    {
        if (IsFromEnd)
            return "^" + ((uint)Value).ToString();

        return ((uint)Value).ToString();
    }
}
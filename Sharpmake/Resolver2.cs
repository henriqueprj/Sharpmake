// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Sharpmake;

public class IgnoreCaseCharComparer : IEqualityComparer<char>
{
    public static readonly IgnoreCaseCharComparer Default = new();
    
    public bool Equals(char x, char y)
    {
        return char.ToLowerInvariant(x) == char.ToLowerInvariant(y);
    }

    public int GetHashCode(char obj)
    {
        return char.ToLowerInvariant(obj).GetHashCode();
    }
}

public class ReadOnlyMemoryCharComparer : IEqualityComparer<ReadOnlyMemory<char>>
{
    public static readonly ReadOnlyMemoryCharComparer Default = new();

    public ReadOnlyMemoryCharComparer()
    {
        
    }
    
    public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => x.Span.SequenceEqual(y.Span);
    public int GetHashCode(ReadOnlyMemory<char> obj) => string.GetHashCode(obj.Span);
}

public class IgnoreCaseReadOnlyMemoryCharComparer : IEqualityComparer<ReadOnlyMemory<char>>
{
    public static readonly IgnoreCaseReadOnlyMemoryCharComparer Default = new();
    
    public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
    {
        return x.Span.SequenceEqual(y.Span, IgnoreCaseCharComparer.Default);
    }

    public int GetHashCode(ReadOnlyMemory<char> obj)
    {
        Span<char> lowerSpan = stackalloc char[obj.Length];
        _ = obj.Span.ToLowerInvariant(lowerSpan);
        return string.GetHashCode(lowerSpan);
    }
}


public class TypeAndReadOnlyMemoryCharComparer : IEqualityComparer<(Type, ReadOnlyMemory<char>)>
{
    public static readonly TypeAndReadOnlyMemoryCharComparer Default = new();
    
    public bool Equals((Type, ReadOnlyMemory<char>) x, (Type, ReadOnlyMemory<char>) y)
    {
        return x.Item1 == y.Item1 && x.Item2.Span.SequenceEqual(y.Item2.Span);
    }

    public int GetHashCode((Type, ReadOnlyMemory<char>) obj)
    {
        return HashCode.Combine(obj.Item1, string.GetHashCode(obj.Item2.Span));
    }
}

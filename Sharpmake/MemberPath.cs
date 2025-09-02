// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sharpmake;

// [DebuggerDisplay("{Value}")]
// internal readonly struct MemberPath : IEquatable<MemberPath>
// {
//     public const char Separator = '.';
//     // public static readonly MemberPath Empty = new(StringSlice.Empty);
//     public static readonly MemberPath Empty = new(StringSegment.Empty);
//
//     // private readonly StringSlice _path;
//     private readonly StringSegment _path;
//
//     //private MemberPath(in StringSlice path)
//     private MemberPath(in StringSegment path)
//     {
//         _path = path;
//     }
//
//     // public static MemberPath Parse(in StringSlice s)
//     public static MemberPath Parse(in StringSegment s)
//     {
//         // TODO: [hpintoribeiro] Validate MemberPath
//
//         return new MemberPath(s);
//     }
//
//     public MemberPathSegment FirstSegment => new MemberPathSegment(this);
//
//     // public StringSlice Value => _path;
//     public StringSegment Value => _path;
//
//     public override int GetHashCode() => _path.GetHashCode();
//
//     public override string ToString() => _path.ToString();
//
//     public bool Equals(MemberPath other) => _path.Equals(other._path);
//
//     public override bool Equals(object obj) => obj is MemberPath other && Equals(other);
//
//     public static bool operator ==(MemberPath left, MemberPath right) => left.Equals(right);
//
//     public static bool operator !=(MemberPath left, MemberPath right) => !left.Equals(right);
// }
//
// [DebuggerDisplay("Value: {Value}")]
// internal readonly struct MemberPathSegment : IEquatable<MemberPathSegment>
// {
//     public static readonly MemberPathSegment Empty = new(MemberPath.Empty);
//
//     private readonly MemberPath _memberPath;
//     private readonly int _segmentStartPosition;
//
//     public MemberPathSegment(in MemberPath memberPath, int segmentStartPosition = 0)
//     {
//         _memberPath = memberPath;
//         _segmentStartPosition = segmentStartPosition;
//     }
//
//     public bool TryGetNextSegment(out MemberPathSegment nextSegment)
//     {
//         var nextSegmentIndex = _memberPath.Value.IndexOf(MemberPath.Separator, _segmentStartPosition + 1);
//         if (nextSegmentIndex == -1)
//         {
//             nextSegment = Empty;
//             return false;
//         }
//
//         nextSegment = new MemberPathSegment(_memberPath, nextSegmentIndex + 1);
//         return true;
//     }
//
//     public bool TryGetPreviousSegment(out MemberPathSegment previousSegment)
//     {
//         if (_segmentStartPosition == 0)
//         {
//             previousSegment = Empty;
//             return false;
//         }
//
//         var previousSegmentIndex = _memberPath.Value
//             .AsSpan(0, _segmentStartPosition - 1)
//             .LastIndexOf(MemberPath.Separator);
//
//         if (previousSegmentIndex == -1)
//         {
//             previousSegment = new MemberPathSegment(_memberPath);
//             return true;
//         }
//
//         previousSegment = new MemberPathSegment(_memberPath, previousSegmentIndex + 1);
//         return true;
//     }
//
//     public bool HasNextSegment => _memberPath.Value.IndexOf(MemberPath.Separator, _segmentStartPosition) >= 0;
//
//     public MemberPath Path => _memberPath;
//
//     // public StringSlice Value
//     public StringSegment Value
//     {
//         get
//         {
//             var memberPathSegment = _memberPath.Value;
//             var nextSegmentIndex = memberPathSegment.IndexOf(MemberPath.Separator, _segmentStartPosition);
//             // return nextSegmentIndex == -1
//             //     ? memberPathSegment[_segmentStartPosition..]
//             //     : memberPathSegment[_segmentStartPosition..nextSegmentIndex];
//             return nextSegmentIndex == -1
//                 ? memberPathSegment.Subsegment(_segmentStartPosition)
//                 : memberPathSegment.Subsegment(_segmentStartPosition, nextSegmentIndex);
//         }
//     }
//
//     public bool Equals(MemberPathSegment other)
//     {
//         return _memberPath.Value.Equals(other.Value);
//     }
//
//     public override bool Equals(object obj)
//     {
//         return obj is MemberPathSegment other && Equals(other);
//     }
//
//     public override int GetHashCode()
//     {
//         return _memberPath.Value.GetHashCode();
//     }
//
//     public static bool operator ==(MemberPathSegment left, MemberPathSegment right)
//     {
//         return left.Equals(right);
//     }
//
//     public static bool operator !=(MemberPathSegment left, MemberPathSegment right)
//     {
//         return !left.Equals(right);
//     }
// }

[DebuggerDisplay("{Value}")]
internal readonly struct MemberPath : IEquatable<MemberPath>
{
    public const char Separator = '.';
    // public static readonly MemberPath Empty = new(StringSlice.Empty);
    public static readonly MemberPath Empty = new(ReadOnlyMemory<char>.Empty);

    // private readonly StringSlice _path;
    private readonly ReadOnlyMemory<char> _path;

    //private MemberPath(in StringSlice path)
    private MemberPath(in ReadOnlyMemory<char> path)
    {
        _path = path;
    }

    // public static MemberPath Parse(in StringSlice s)
    public static MemberPath Parse(in ReadOnlyMemory<char> s)
    {
        // TODO: [hpintoribeiro] Validate MemberPath

        return new MemberPath(s);
    }

    public MemberPathSegment FirstSegment => new MemberPathSegment(_path);

    // public StringSlice Value => _path;
    public ReadOnlyMemory<char> Value => _path;

    public override int GetHashCode() => _path.GetHashCode();

    public override string ToString() => _path.ToString();

    public bool Equals(MemberPath other) => _path.Equals(other._path);

    public override bool Equals(object obj) => obj is MemberPath other && Equals(other);

    public static bool operator ==(MemberPath left, MemberPath right) => left.Equals(right);

    public static bool operator !=(MemberPath left, MemberPath right) => !left.Equals(right);
}
//
// [DebuggerDisplay("Value: {Value}")]
// internal readonly struct MemberPathSegment : IEquatable<MemberPathSegment>
// {
//     public static readonly MemberPathSegment Empty = new(MemberPath.Empty);
//
//     private readonly MemberPath _memberPath;
//     private readonly int _segmentStartPosition;
//
//     public MemberPathSegment(in MemberPath memberPath, int segmentStartPosition = 0)
//     {
//         _memberPath = memberPath;
//         _segmentStartPosition = segmentStartPosition;
//     }
//
//     public bool TryGetNextSegment(out MemberPathSegment nextSegment)
//     {
//         var nextSegmentIndex = _memberPath.Value.Span[(_segmentStartPosition + 1)..].IndexOf(MemberPath.Separator);
//         if (nextSegmentIndex == -1)
//         {
//             nextSegment = Empty;
//             return false;
//         }
//
//         nextSegment = new MemberPathSegment(_memberPath, nextSegmentIndex + 1);
//         return true;
//     }
//
//     public bool HasNextSegment => _memberPath.Value.Span[_segmentStartPosition..].IndexOf(MemberPath.Separator) >= 0;
//
//     public MemberPath Path => _memberPath;
//
//     // public StringSlice Value
//     public ReadOnlyMemory<char> Value
//     {
//         get
//         {
//             var memberPathMem = _memberPath.Value;
//             var nextSegmentIndex = memberPathMem.Span[_segmentStartPosition..].IndexOf(MemberPath.Separator);
//             return nextSegmentIndex == -1
//                 ? memberPathMem[_segmentStartPosition..]
//                 : memberPathMem.Slice(_segmentStartPosition, nextSegmentIndex);
//         }
//     }
//
//     public bool Equals(MemberPathSegment other)
//     {
//         return _memberPath.Value.Equals(other.Value);
//     }
//
//     public override bool Equals(object obj)
//     {
//         return obj is MemberPathSegment other && Equals(other);
//     }
//
//     public override int GetHashCode()
//     {
//         return _memberPath.Value.GetHashCode();
//     }
//
//     public static bool operator ==(MemberPathSegment left, MemberPathSegment right)
//     {
//         return left.Equals(right);
//     }
//
//     public static bool operator !=(MemberPathSegment left, MemberPathSegment right)
//     {
//         return !left.Equals(right);
//     }
// }

[DebuggerDisplay("Value: {SegmentValue}")]
internal readonly struct MemberPathSegment : IEquatable<MemberPathSegment>
{
    private const char Separator = '.';

    public static readonly MemberPathSegment Empty = new(ReadOnlyMemory<char>.Empty);

    private readonly ReadOnlyMemory<char> _fullPath;
    private readonly int _segmentStartPosition;

    public MemberPathSegment(in ReadOnlyMemory<char> fullPath, int segmentStartPosition = 0)
    {
        _fullPath = fullPath;
        _segmentStartPosition = segmentStartPosition;
    }
    
    public MemberPathSegment(string fullPath, int segmentStartPosition = 0) : this(fullPath.AsMemory(), segmentStartPosition)
    {
    }

    public bool TryGetNextSegment(out MemberPathSegment nextSegment)
    {
        var currentOffsetSpan = _fullPath.Span[_segmentStartPosition..];
        var nextSegmentIndex = currentOffsetSpan.IndexOf(Separator);
        if (nextSegmentIndex == -1)
        {
            nextSegment = Empty;
            return false;
        }

        int startIndexNextSegment = _segmentStartPosition + nextSegmentIndex + 1;
        nextSegment = new MemberPathSegment(_fullPath, startIndexNextSegment);
        return true;
    }

    public bool HasNextSegment => _fullPath.Span[_segmentStartPosition..].IndexOf(Separator) >= 0;

    public ReadOnlyMemory<char> FullPath => _fullPath;

    // public StringSlice Value
    public ReadOnlyMemory<char> SegmentValue
    {
        get
        {
            var memberPathMem = _fullPath;
            var memberPathOffsetSpan = memberPathMem.Span[_segmentStartPosition..]; 
            var nextSegmentIndex = memberPathOffsetSpan.IndexOf(Separator);
            return nextSegmentIndex == -1
                ? memberPathMem[_segmentStartPosition..]
                : memberPathMem.Slice(_segmentStartPosition, nextSegmentIndex);
        }
    }

    public bool Equals(MemberPathSegment other) => SegmentValue.Span.SequenceEqual(other.SegmentValue.Span);
    public override bool Equals(object obj) => obj is MemberPathSegment other && Equals(other);
    public override int GetHashCode() => SegmentValue.GetHashCode();
    public static bool operator ==(MemberPathSegment left, MemberPathSegment right) => left.Equals(right);
    public static bool operator !=(MemberPathSegment left, MemberPathSegment right) => !left.Equals(right);
}

[DebuggerDisplay("Value: {SegmentValue}")]
internal ref struct MemberPathSegmentIterator : IEquatable<MemberPathSegment>
{
    private const char Separator = '.';

    public static readonly MemberPathSegment Empty = new(ReadOnlyMemory<char>.Empty);

    private readonly ReadOnlyMemory<char> _fullPath;
    private int _segmentStartPosition;

    public MemberPathSegmentIterator(in ReadOnlyMemory<char> fullPath, int segmentStartPosition = 0)
    {
        _fullPath = fullPath;
        _segmentStartPosition = segmentStartPosition;
    }
    
    public MemberPathSegmentIterator(string fullPath, int segmentStartPosition = 0) : this(fullPath.AsMemory(), segmentStartPosition)
    {
    }

    public bool MoveNext()
    {
        var currentOffsetSpan = _fullPath.Span[_segmentStartPosition..];
        var nextSegmentIndex = currentOffsetSpan.IndexOf(Separator);
        if (nextSegmentIndex == -1)
        {
            return false;
        }

        _segmentStartPosition = _segmentStartPosition + nextSegmentIndex + 1;
        return true;
    }

    public bool HasNextSegment => _fullPath.Span[_segmentStartPosition..].IndexOf(Separator) >= 0;

    public ReadOnlyMemory<char> FullPath => _fullPath;

    // public StringSlice Value
    public ReadOnlyMemory<char> Current
    {
        get
        {
            var memberPathMem = _fullPath;
            var memberPathOffsetSpan = memberPathMem.Span[_segmentStartPosition..]; 
            var nextSegmentIndex = memberPathOffsetSpan.IndexOf(Separator);
            return nextSegmentIndex == -1
                ? memberPathMem[_segmentStartPosition..]
                : memberPathMem.Slice(_segmentStartPosition, nextSegmentIndex);
        }
    }

    public bool Equals(MemberPathSegmentIterator other) => Current.Span.SequenceEqual(other.Current.Span);
    public override bool Equals(object obj) => obj is MemberPathSegment other && Equals(other);
    public override int GetHashCode() => Current.GetHashCode();
    public static bool operator ==(MemberPathSegmentIterator left, MemberPathSegmentIterator right) => left.Equals(right);
    public static bool operator !=(MemberPathSegmentIterator left, MemberPathSegmentIterator right) => !left.Equals(right);
}

[DebuggerDisplay("Current: {Current}")]
internal ref struct MemberPathSegmentEnumerator
{
    private const char Separator = '.';

    private readonly ReadOnlyMemory<char> _fullPath;
    private ReadOnlySpan<char> _path;

    public MemberPathSegmentEnumerator(in ReadOnlyMemory<char> fullPath)
    {
        _fullPath = fullPath;
        _path = fullPath.Span;
    }

    public bool MoveNext()
    {
        var nextSegmentIndex = _path.IndexOf(Separator);
        if (nextSegmentIndex == -1)
        {
            return false;
        }

        _path = _path[(nextSegmentIndex + 1)..];
        return true;
    }
    
    public bool IsEmpty => _path.IsEmpty;
    
    public ReadOnlySpan<char> Current
    {
        get
        {
            var nextSegmentIndex = _path.IndexOf(Separator);
            return nextSegmentIndex == -1 ? _path : _path[..nextSegmentIndex];
        }
    }
}

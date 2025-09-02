// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Sharpmake;

public enum TokenType
{
    Literal,
    Expression,
    EscapedExpression
}

public readonly struct Delimiter
{
    public string Open { get; }
    public char Close { get; }

    public Delimiter(string open, char close)
    {
        if (string.IsNullOrEmpty(open))
            throw new ArgumentException("Open token cannot be null or empty.", nameof(open));
        Open = open;
        Close = close;
    }

    public ReadOnlySpan<char> OpenSpan => Open.AsSpan();
}

public readonly ref struct Token
{
    public ReadOnlySpan<char> Value { get; }
    public TokenType Type { get; }

    public Token(ReadOnlySpan<char> value, TokenType type)
    {
        Value = value;
        Type = type;
    }
}

public ref struct TokenEnumerator
{
    private readonly ReadOnlySpan<char> _input;
    private readonly Delimiter[] _delimiters;
    private int _pos;

    public TokenEnumerator(ReadOnlySpan<char> input, Delimiter[] delimiters)
    {
        _input = input;
        _delimiters = delimiters;
        _pos = 0;
        Current = default;
    }

    public Token Current { get; private set; }

    public bool MoveNext()
    {
        if (_pos >= _input.Length)
            return false;

        int literalStart = _pos;

        while (_pos < _input.Length)
        {
            int matchedIdx = MatchAnyOpen(_input[_pos..], out Delimiter delimiter);
            if (matchedIdx == -1)
            {
                _pos++;
                continue;
            }

            // Literal before match
            if (_pos > literalStart)
            {
                Current = new Token(_input[literalStart.._pos], TokenType.Literal);
                return true;
            }

            // Escaped expression (only for single-char open)
            if (delimiter.Open.Length == 1 &&
                _pos + 1 < _input.Length &&
                _input[_pos + 1] == delimiter.Open[0])
            {
                var escapeClose = new string(delimiter.Open[0], 1) + delimiter.Close;
                int closeIdx = _input[(_pos + 2)..].IndexOf(escapeClose.AsSpan());
                if (closeIdx == -1)
                {
                    Current = new Token(_input[_pos..], TokenType.Literal);
                    _pos = _input.Length;
                    return true;
                }
                closeIdx += _pos + 2;
                Current = new Token(_input.Slice(_pos + 1, closeIdx - _pos - 1), TokenType.EscapedExpression);
                _pos = closeIdx + 2;
                return true;
            }

            // Normal expression
            int searchStart = _pos + delimiter.Open.Length;
            int closePos = _input[searchStart..].IndexOf(delimiter.Close);
            if (closePos == -1)
            {
                Current = new Token(_input[_pos..], TokenType.Literal);
                _pos = _input.Length;
                return true;
            }
            closePos += searchStart;

            Current = new Token(_input[searchStart..closePos], TokenType.Expression);
            _pos = closePos + 1;
            return true;
        }

        // Emit final literal if any
        if (literalStart < _input.Length)
        {
            Current = new Token(_input[literalStart..], TokenType.Literal);
            _pos = _input.Length;
            return true;
        }

        return false;
    }

    private int MatchAnyOpen(ReadOnlySpan<char> span, out Delimiter matched)
    {
        for (int i = 0; i < _delimiters.Length; i++)
        {
            var open = _delimiters[i].OpenSpan;
            if (span.StartsWith(open))
            {
                matched = _delimiters[i];
                return i;
            }
        }
        matched = default;
        return -1;
    }
}

public static class TokenParser
{
    public static TokenEnumerator ParseTokens(ReadOnlySpan<char> input, Delimiter[] delimiters)
        => new TokenEnumerator(input, delimiters);
}

#nullable enable

public static class ExpressionEvaluator
{
    // Cache accessor: (rootType, "Name.Age" etc) -> compiled accessor delegate
    // object (root) => object? (final value)
    private static readonly ConcurrentDictionary<(Type, ReadOnlyMemory<char>), Func<object, object?>> AccessorCache = new(TypeAndReadOnlyMemoryCharComparer.Default);

    public static void Add(Type type, ReadOnlyMemory<char> parameter, Func<object, object?> accessor)
    {
        AccessorCache.TryAdd((type, parameter), accessor);
    }
    
    /// <summary>
    /// Evaluate an expression token (e.g., "person.Name") against a variables dictionary.
    /// Returns null if anything is missing (variable not found, null intermediate, or member not found).
    /// </summary>
    public static object? Evaluate(in ReadOnlyMemory<char> expression /*ReadOnlySpan<char> expression*/, IReadOnlyDictionary<ReadOnlyMemory<char>, object> variables)
    {
        if (expression.IsEmpty) return null;

        // Find root variable name: up to first '.'
        int dot = expression.Span.IndexOf('.');
        ReadOnlyMemory<char> rootMem = dot >= 0 ? expression[..dot] : expression;
        ReadOnlyMemory<char> pathMem = dot >= 0 ? expression[(dot + 1)..] : ReadOnlyMemory<char>.Empty;

        // Dictionary<string, object> requires string key.
        // This is the only unavoidable allocation to lookup the root variable.
        //string rootName = new string(rootMem);
        if (!variables.TryGetValue(rootMem, out object? root) /*|| root is null*/)
            return null;

        if (pathMem.IsEmpty)
            return root;

        // Build or get compiled accessor for this (Type, path)
        //string pathKey = new string(pathMem); // allocation once per distinct path
        var key = (root.GetType(), pathMem);

        var accessor = AccessorCache.GetOrAdd(key, k => BuildAccessor(k.Item1, k.Item2));
        return accessor(root);
    }

    // Build a compiled accessor for a given Type and dotted path ("A.B.C")
    // private static Func<object, object?> BuildAccessor(Type rootType, ReadOnlyMemory<char> dottedPath)
    // {
    //     // Parse segments without creating an array of substrings more than necessary.
    //     // We still need strings for reflection lookups, so we create one per segment.
    //     //var memberPathSegment = new MemberPathSegment(dottedPath);
    //     var memberPathSegment = new MemberPathSegmentEnumerator(dottedPath.Span);
    //
    //     // Build: (object obj) => (object?) { var r=(RootType)obj; if (r==null) return null; var tmp = r.A; if(tmp==null) return null; tmp = tmp.B; ... ; return (object?)tmp; }
    //     var objParam = Expression.Parameter(typeof(object), "obj");
    //     var typedRoot = Expression.Variable(rootType, "root");
    //     var assignRoot = Expression.Assign(typedRoot, Expression.Convert(objParam, rootType));
    //
    //     var blockVars = new List<ParameterExpression> { typedRoot };
    //     Expression body = typedRoot;
    //
    //     // If root is null, return null
    //     Expression resultExpr;
    //
    //     if (memberPathSegment.IsEmpty)
    //     {
    //         resultExpr = Expression.Convert(typedRoot, typeof(object));
    //     }
    //     else
    //     {
    //         // Walk each segment, adding null-propagation
    //         Expression current = typedRoot;
    //
    //         do
    //         {
    //             // current = current?.Member
    //             // First, if current is a value type, no null check needed (unless nullable)
    //             var currentType = current.Type;
    //
    //             // Resolve member: property or field (public instance)
    //             MemberInfo? member = ResolveMember(currentType, memberPathSegment.Current.ToString());
    //             if (member == null)
    //             {
    //                 // Member not found: compile accessor that always returns null
    //                 return static _ => null;
    //             }
    //
    //             Expression access = member is PropertyInfo pi
    //                 ? Expression.Property(current, pi)
    //                 : Expression.Field(current, (FieldInfo)member);
    //
    //             // Add null check if current is reference type or Nullable<T>
    //             if (!currentType.IsValueType || Nullable.GetUnderlyingType(currentType) != null)
    //             {
    //                 var tmp = Expression.Variable(access.Type, "t");
    //                 blockVars.Add(tmp);
    //
    //                 // tmp = current == null ? default : current.Member
    //                 var assignTmp = Expression.Assign(
    //                     tmp,
    //                     Expression.Condition(
    //                         Expression.Equal(current, Expression.Constant(null, current.Type)),
    //                         Expression.Default(access.Type),
    //                         access));
    //
    //                 // Next iteration reads from tmp (may be null/default)
    //                 current = tmp;
    //                 body = Expression.Block(new[] { tmp }, assignTmp, tmp);
    //             }
    //             else
    //             {
    //                 // Value type: just access
    //                 current = access;
    //             }
    //         } while (memberPathSegment.MoveNext());
    //
    //         // Box final
    //         resultExpr = Expression.Convert(current, typeof(object));
    //         // Prepend typedRoot assignment
    //         body = Expression.Block(blockVars, assignRoot, body, resultExpr);
    //     }
    //
    //     var lambda = Expression.Lambda<Func<object, object?>>(body, objParam);
    //     return lambda.Compile(); // JIT-compiled delegate; cached for reuse
    // }
    
    // Build a compiled accessor for a given Type and dotted path ("A.B.C")
    private static Func<object, object?> BuildAccessor(Type rootType, ReadOnlyMemory<char> dottedPath)
    {
        var segments = new MemberPathSegmentEnumerator(dottedPath.Span);

        var objParam = Expression.Parameter(typeof(object), "obj");
        var typedRoot = Expression.Variable(rootType, "root");

        var assignRoot = Expression.Assign(typedRoot, Expression.Convert(objParam, rootType));

        Expression current = typedRoot;

        //foreach (string seg in segments)
        do
        {
            MemberInfo? member = ResolveMember(current.Type, segments.Current.ToString());
            if (member == null)
                return static _ => null;

            Expression access = member is PropertyInfo pi
                ? Expression.Property(current, pi)
                : Expression.Field(current, (FieldInfo)member);

            // Add null-check if reference type
            if (!current.Type.IsValueType || Nullable.GetUnderlyingType(current.Type) != null)
            {
                var tmp = Expression.Variable(access.Type, "t");
                var assignTmp = Expression.Assign(
                    tmp,
                    Expression.Condition(
                        Expression.Equal(current, Expression.Constant(null, current.Type)),
                        Expression.Default(access.Type),
                        access));
                //current = tmp;
                current = Expression.Block(new[] { tmp }, assignTmp, tmp);
            }
            else
            {
                current = access;
            }
        } while (segments.MoveNext());

        var body = Expression.Block(new[] { typedRoot }, assignRoot, Expression.Convert(current, typeof(object)));
        return Expression.Lambda<Func<object, object?>>(body, objParam).Compile();
    }

    private static MemberInfo? ResolveMember(Type type, string name)
    {
        // Exact, public instance only. Prefer Property, then Field.
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

        // Fields first
        var field = type.GetField(name, Flags);
        if (field != null) return field;

        // Properties next
        var prop = type.GetProperty(name, Flags);
        if (prop != null) return prop;

        // Optional: support case-insensitive fallback (commented out for perf)
        // var propIgnore = type.GetProperty(name, Flags | BindingFlags.IgnoreCase);
        // if (propIgnore != null) return propIgnore;
        // var fieldIgnore = type.GetField(name, Flags | BindingFlags.IgnoreCase);
        // if (fieldIgnore != null) return fieldIgnore;

        return null;
    }
}

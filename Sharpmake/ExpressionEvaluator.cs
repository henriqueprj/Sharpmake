// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Sharpmake;

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
    public static object? Evaluate(in ReadOnlyMemory<char> expression, IReadOnlyDictionary<ReadOnlyMemory<char>, Resolver3.RefCountedSymbol> variables)
    {
        if (expression.IsEmpty) return null;

        // Find root variable name: up to first '.'
        int dot = expression.Span.IndexOf('.');
        ReadOnlyMemory<char> rootMem = dot >= 0 ? expression[..dot] : expression;
        ReadOnlyMemory<char> pathMem = dot >= 0 ? expression[(dot + 1)..] : ReadOnlyMemory<char>.Empty;

        // Dictionary<string, object> requires string key.
        // This is the only unavoidable allocation to lookup the root variable.
        //string rootName = new string(rootMem);
        if (!variables.TryGetValue(rootMem, out Resolver3.RefCountedSymbol? root) /*|| root is null*/)
            return null;

        if (pathMem.IsEmpty)
            return root.Value;

        // Build or get compiled accessor for this (Type, path)
        //string pathKey = new string(pathMem); // allocation once per distinct path
        var key = (root.GetType(), pathMem);

        var accessor = AccessorCache.GetOrAdd(key, k => BuildAccessor(k.Item1, k.Item2));
        return accessor(root.Value);
    }

    // TODO: [hpintoribeiro] Add support for IDictionary.
    // Build a compiled accessor for a given Type and dotted path ("A.B.C")
    private static Func<object, object?> BuildAccessor(Type rootType, ReadOnlyMemory<char> dottedPath)
    {
        var segments = new MemberPathSegmentIterator(dottedPath);

        var objParam = Expression.Parameter(typeof(object), "obj");
        var typedRoot = Expression.Variable(rootType, "root");

        var assignRoot = Expression.Assign(typedRoot, Expression.Convert(objParam, rootType));

        Expression current = typedRoot;

        while (segments.MoveNext())
        {
            MemberInfo? member = ResolveMember(current.Type, segments.Current.Span.ToString());

            // TODO: [hpintoribeiro] Add support for IDictionary. If is last segment and rootType is IDictionary, add code to retrieve the value from it.
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
        }

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

// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    public static object? Evaluate(in ReadOnlyMemory<char> expression, IReadOnlyDictionary<ReadOnlyMemory<char>, Resolver3.RefCountedSymbol> variables, bool throwIfNotFound)
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

        var accessor = AccessorCache.GetOrAdd(key, static (k, shouldThrowIfNotFound) => BuildAccessor(k.Item1, k.Item2, shouldThrowIfNotFound), throwIfNotFound);
        return accessor(root.Value);
    }

    // TODO: [hpintoribeiro] Test support for IDictionary.
    // Build a compiled accessor for a given Type and dotted path ("A.B.C")
    private static Func<object, object?> BuildAccessor(Type rootType, ReadOnlyMemory<char> memberPath, bool throwIfNotFound)
    {
        var segments = new MemberPathSegmentIterator(memberPath);

        var objParam = Expression.Parameter(typeof(object), "obj");
        var typedRoot = Expression.Variable(rootType, "root");

        var assignRoot = Expression.Assign(typedRoot, Expression.Convert(objParam, rootType));

        Expression current = typedRoot;

        while (segments.MoveNext())
        {
            // Reflection api only accept string...
            var currentSegmentString = segments.Current.Span.ToString();
            MemberInfo? member = ResolveMember(current.Type, currentSegmentString);

            if (member is null)
            {
                if (!segments.HasNext && typeof(IDictionary).IsAssignableFrom(current.Type))
                {
                    // Generate code to lookup value from IDictionary
                    var dictVar = Expression.Variable(typeof(IDictionary), "dict");
                    var keyConst = Expression.Constant(currentSegmentString, typeof(object));
                    var assignDict = Expression.Assign(dictVar, Expression.Convert(current, typeof(IDictionary)));
                    var indexerProp = typeof(IDictionary).GetProperty("Item")!;
                    var indexerAccess = Expression.Property(dictVar, indexerProp, keyConst);

                    current = Expression.Block(new[] { dictVar }, assignDict, indexerAccess);
                    continue;
                }

                if (throwIfNotFound)
                    throw GetNotFoundException(current.Type, segments); // TODO: [hpintoribeiro] Continue here...

                return static _ => null;
            }

            Expression access = member switch
            {
                PropertyInfo pi => Expression.Property(current, pi),
                FieldInfo fi => Expression.Field(current, fi),
                _ => throw new NotSupportedException() // TODO: [hpintoribeiro] Improve error message
            };

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

    [DoesNotReturn]
    private static Resolver3.NotFoundException GetNotFoundException(Type currentType, in MemberPathSegmentIterator memberPathIterator)
    {
        string currentPath = memberPathIterator.AbsolutePreviousPath.Span.ToString() + MemberPathSegmentIterator.Separator;

        // get all public fields
        var possibleArguments = currentType.GetFields().Select(f => currentPath + f.Name);

        // all public properties
        possibleArguments = possibleArguments.Concat(currentType.GetProperties().Select(p => currentPath + p.Name));

        // and dictionary keys, if they are strings
        var dictionary = parameter as IDictionary; // TODO: [hpintoribeiro] Continue here...
        if (dictionary != null)
        {
            var keysAsStrings = ((IDictionary)parameter).Keys as IEnumerable<string>;
            if (keysAsStrings != null)
                possibleArguments = possibleArguments.Concat(keysAsStrings.Select(k => currentPath + k));
        }

        return new Resolver3.NotFoundException(
            $"Cannot find path '{nameChunk}' in parameter path '{memberPath}'",
            possibleArguments
        );
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

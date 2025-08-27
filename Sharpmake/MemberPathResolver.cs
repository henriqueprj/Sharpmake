// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

//
// using System.Collections.Generic;
//
// namespace Sharpmake;
//
// internal class MemberPathResolver
// {
//     private readonly Dictionary<StringSlice, RefCountedSymbol> _parameters;
//
//     public MemberPathResolver(Dictionary<StringSlice, RefCountedSymbol> parameters)
//     {
//         _parameters = parameters;
//     }
//
//     public object Resolve(MemberPath memberPath, object fallbackValue)
//     {
//
//     }
//
//
// }
//
// internal class RefCountedSymbol
// {
//     private readonly Stack<object> _scopedReferences = new Stack<object>();
//
//     public object Value
//     {
//         get
//         {
//             return _scopedReferences.Peek();
//         }
//         set
//         {
//             _scopedReferences.Pop();
//             _scopedReferences.Push(value);
//         }
//     }
//     public bool HasValue => _scopedReferences.Count > 0;
//
//     public RefCountedSymbol(object symbolValue)
//     {
//         _scopedReferences.Push(symbolValue);
//     }
//
//     public void PushValue(object value)
//     {
//         _scopedReferences.Push(value);
//     }
//
//     public void PopValue()
//     {
//         _scopedReferences.Pop();
//     }
// }

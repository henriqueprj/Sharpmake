// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sharpmake.UnitTests;

public class ExpressionEvaluatorTest
{
    [Test]
    public void Evaluate()
    {
        var foo = new Foo { BarProp = new Bar { Value = "baz" } };
        
        var parameters = new Dictionary<ReadOnlyMemory<char>, Resolver3.RefCountedSymbol>(ReadOnlyMemoryCharComparer.Default)
        {
            { "foo".AsMemory(), new Resolver3.RefCountedSymbol(foo) }
        };

        var expr = "foo.BarProp.Value".AsMemory();
        
        ExpressionEvaluator.Add(typeof(Foo), "BarProp.Value".AsMemory(), static obj => ((Foo)obj).BarProp.Value);
        object result = ExpressionEvaluator.Evaluate(expr, parameters, false);

        Assert.AreEqual(foo.BarProp.Value, result);
    }


    class Foo
    {
        public Bar BarProp { get; set; }
    }

    class Bar
    {
        public string Value { get; set; }
    }
}

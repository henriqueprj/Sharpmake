// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using NUnit.Framework;

namespace Sharpmake.UnitTests;

public class MemberPathSegmentTest
{
    [Test]
    public void ShouldReturnFirstSegment()
    {
        var expr = "a.b.c";

        var memberPath = new MemberPathSegment(expr);

        var firstSegment = memberPath.SegmentValue.Span.ToString();
        
        Assert.AreEqual("a", firstSegment);
    }
    
    [Test]
    public void ShouldReturnSecondSegment()
    {
        var expr = "foo.bar.baz";

        var memberPath = new MemberPathSegment(expr);

        memberPath.TryGetNextSegment(out var secondSegment);
        
        Assert.AreEqual("bar", secondSegment.SegmentValue.Span.ToString());
    }
    
    [Test]
    public void ShouldReturnThirdSegment()
    {
        var expr = "foo.bar.baz";

        var memberPath = new MemberPathSegment(expr);

        memberPath.TryGetNextSegment(out var secondSegment);
        secondSegment.TryGetNextSegment(out var thirdSegment);
        
        Assert.AreEqual("baz", thirdSegment.SegmentValue.Span.ToString());
    }
}

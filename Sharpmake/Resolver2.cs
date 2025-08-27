// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Sharpmake;

internal sealed class StringSliceComparer : IEqualityComparer<StringSlice>
{
    private readonly StringComparison _comparison;

    public static readonly StringSliceComparer Ordinal = new(StringComparison.Ordinal);
    public static readonly StringSliceComparer OrdinalIgnoreCase = new(StringComparison.OrdinalIgnoreCase);

    public StringSliceComparer(StringComparison comparison)
    {
        _comparison = comparison;
    }

    public bool Equals(StringSlice x, StringSlice y)
    {
        return x.AsSpan().Equals(y.AsSpan(), _comparison);
    }

    public int GetHashCode(StringSlice obj) => string.GetHashCode(obj.AsSpan());
}

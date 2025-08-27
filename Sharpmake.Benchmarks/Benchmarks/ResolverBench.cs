// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using BenchmarkDotNet.Attributes;

namespace Sharpmake.Benchmarks.Benchmarks;


[SimpleJob, MemoryDiagnoser]
public class ResolverBench
{
    private static readonly Person _person = new("Henrique");

    [Benchmark(Baseline = true)]
    public string Resolve()
    {
        Resolver resolver = new();
        resolver.SetParameter("person", _person);

        return resolver.Resolve("Hello [person.Name]. How are you doing?", null, out _);
    }

    [Benchmark]
    public string Resolve_New()
    {
        Resolver resolver = new();
        resolver.SetParameter("person", _person);

        return resolver.Resolve2("Hello [person.Name]. How are you doing?", null, out _);
    }


}

public record Person(string Name);

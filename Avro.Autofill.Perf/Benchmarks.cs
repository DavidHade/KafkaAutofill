using BenchmarkDotNet.Attributes;
using Avro.Autofill.Tests;
using Avro.Autofill.Tests.Person2;
using Avro.Autofill.Tests.Person3;

namespace Avro.Autofill.Perf;

// -------------------------- Benchmark results -----------------------------------//
//
// | Method                  | Mean          | Error       | StdDev      | Allocated   |
// |------------------------ |--------------:|------------:|------------:|------------:|
// | Person_SchemaGen_True   |     10.240 us |   0.0992 us |   0.0880 us |    80.47 KB |
// | Person_SchemaGen_False  | 17,114.492 us | 258.0807 us | 241.4089 us | 20210.13 KB |
// | Person_SchemaGen_Cached |     10.092 us |   0.1814 us |   0.1608 us |    80.47 KB |
// | Person_AvroGen          |      1.691 us |   0.0125 us |   0.0111 us |    32.81 KB |
//
// Caching schema in a static field is essential for performance & memory usage
// Goal: get close to AvroGen performance, although it has little meaningful difference in real world


[MemoryDiagnoser(false)]
public class Benchmarks
{
    [Benchmark]
    public void Person_SchemaGen_True()
    {
        for (int i = 0; i < 100; i++)
        {
            var p = new Person();
        }
    }

    [Benchmark]
    public void Person_SchemaGen_False()
    {
        for (int i = 0; i < 100; i++)
        {
            var p = new Person2();
        }
    }
    
    [Benchmark]
    public void Person_SchemaGen_Cached()
    {
        for (int i = 0; i < 100; i++)
        {
            var p = new Person3();
        }
    }
    
    [Benchmark]
    public void Person_AvroGen()
    {
        for (int i = 0; i < 100; i++)
        {
            var p = new Avro.Autofill.Tests.AvroGen.AvroGenPerson.Person();
        }
    }
}
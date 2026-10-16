using BenchmarkDotNet.Attributes;

namespace BrutalUi.Benchmarks;

/// <summary>
/// Holds one empty benchmark. It keeps the benchmark project buildable until real benchmarks exist.
/// </summary>
[MemoryDiagnoser]
public class PlaceholderBenchmark
{
    /// <summary>Does nothing. It proves that the benchmark harness runs.</summary>
    [Benchmark]
    public void Empty()
    {
    }
}

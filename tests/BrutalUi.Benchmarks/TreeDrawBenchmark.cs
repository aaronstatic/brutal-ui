using BenchmarkDotNet.Attributes;
using BrutalUi.Core;
using BrutalUi.Core.Tests;

namespace BrutalUi.Benchmarks;

/// <summary>
/// Measures the draw walk on the shared tree of 1000 elements (<see cref="ThousandElementTree"/>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Draw"/> calls <c>Draw</c> of the context with one <see cref="RecordingRenderer"/>. The time holds the
/// walk of the tree and the cost of the recording. The renderer appends one entry for each call and clears its list
/// at <c>Begin</c>, so the cost of the recording is small and stable.
/// </para>
/// <para>
/// The body must allocate nothing after warm-up, so the <c>Allocated</c> column shows <c>-</c> or <c>0 B</c>.
/// The setup builds the tree and can allocate.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class TreeDrawBenchmark
{
    private const float FrameTime = 0.016f;

    private ThousandElementTree _tree = null!;
    private RecordingRenderer _renderer = null!;

    /// <summary>Builds the tree, runs the first <c>Update</c>, creates the renderer, and runs <c>Draw</c> two times as warm-up.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _tree = ThousandElementTree.Build();
        _tree.Context.Update(FrameTime);
        _renderer = new RecordingRenderer();

        for (int i = 0; i < 2; i++)
        {
            Draw();
        }
    }

    /// <summary>Draws the tree into the recording renderer.</summary>
    [Benchmark]
    public void Draw()
    {
        _tree.Context.Draw(_renderer);
    }
}

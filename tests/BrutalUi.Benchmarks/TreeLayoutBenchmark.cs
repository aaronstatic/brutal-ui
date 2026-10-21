using BenchmarkDotNet.Attributes;
using BrutalUi.Core;
using BrutalUi.Core.Tests;

namespace BrutalUi.Benchmarks;

/// <summary>
/// Measures the layout pass on the shared tree of 1000 elements (<see cref="ThousandElementTree"/>).
/// </summary>
/// <remarks>
/// <para>
/// The benchmark has three paths. <see cref="FullLayout"/> marks each element of the tree for layout, then runs one
/// <c>Update</c>. The public API has no call that lays out all elements, so the number holds the 1000 marks.
/// The settings panel is hidden, so the pass lays out 780 elements and sets the 220 elements of the panel to zero.
/// <see cref="DirtySubtree"/> changes the width of one stat value leaf, marks it, and runs one <c>Update</c>.
/// <see cref="CleanUpdate"/> runs one <c>Update</c> on a clean tree.
/// </para>
/// <para>
/// Each body leaves the tree clean. Each body must allocate nothing, so the <c>Allocated</c> column shows <c>-</c>
/// or <c>0 B</c>. The setup builds the tree and can allocate.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class TreeLayoutBenchmark
{
    private const float FrameTime = 0.016f;

    private ThousandElementTree _tree = null!;
    private bool _wide;

    /// <summary>Builds the tree, runs the first <c>Update</c>, and runs each body two times as warm-up.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _tree = ThousandElementTree.Build();
        _tree.Context.Update(FrameTime);

        for (int i = 0; i < 2; i++)
        {
            FullLayout();
            DirtySubtree();
            CleanUpdate();
        }
    }

    /// <summary>Marks each element for layout, then runs one <c>Update</c>.</summary>
    [Benchmark]
    public void FullLayout()
    {
        Element[] all = _tree.All;

        for (int i = 0; i < all.Length; i++)
        {
            all[i].MarkDirty(DirtyFlags.Layout);
        }

        _tree.Context.Update(FrameTime);
    }

    /// <summary>Changes the width of one stat value leaf, marks it, then runs one <c>Update</c>.</summary>
    [Benchmark]
    public void DirtySubtree()
    {
        _wide = !_wide;
        _tree.StatValueLeaf.Inline.Width = Length.Percent(
            _wide ? ThousandElementTree.ValueWidePercent : ThousandElementTree.ValueNarrowPercent);
        _tree.StatValueLeaf.MarkDirty(DirtyFlags.Layout);
        _tree.Context.Update(FrameTime);
    }

    /// <summary>Runs one <c>Update</c> on a clean tree.</summary>
    [Benchmark]
    public void CleanUpdate()
    {
        _tree.Context.Update(FrameTime);
    }
}

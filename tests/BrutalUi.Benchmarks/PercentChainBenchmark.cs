using BenchmarkDotNet.Attributes;
using BrutalUi.Core;
using BrutalUi.Core.Tests;

namespace BrutalUi.Benchmarks;

/// <summary>
/// Measures one leaf mark in a deep percent chain (<see cref="ChainTree"/>).
/// </summary>
/// <remarks>
/// <para>
/// This benchmark closes the hand-off of CR-1 of the flexbox-layout code review. Each chain has 20 levels below the
/// root and a text leaf at the end. <see cref="PaddingChainLeafMark"/> uses a percent padding on each level.
/// <see cref="MaxWidthChainLeafMark"/> uses a percent maximum width. Each body marks the leaf for layout, then runs
/// one <c>Update</c>. The callback count of the same chains has a budget in <c>PercentChainBudgetTests</c>.
/// </para>
/// <para>
/// Each body leaves the chain clean and allocates nothing, so the <c>Allocated</c> column shows <c>-</c> or
/// <c>0 B</c>. The setup builds the chains and can allocate.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class PercentChainBenchmark
{
    private const float FrameTime = 0.016f;

    private ChainTree _padding = null!;
    private ChainTree _maxWidth = null!;

    /// <summary>Builds both chains, runs the first <c>Update</c> on each, and runs each body two times as warm-up.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _padding = ChainTree.Build(ChainShape.PercentPadding, ChainTree.Depth);
        _maxWidth = ChainTree.Build(ChainShape.PercentMaxWidth, ChainTree.Depth);
        _padding.Context.Update(FrameTime);
        _maxWidth.Context.Update(FrameTime);

        for (int i = 0; i < 2; i++)
        {
            PaddingChainLeafMark();
            MaxWidthChainLeafMark();
        }
    }

    /// <summary>Marks the leaf of the percent padding chain, then runs one <c>Update</c>.</summary>
    [Benchmark]
    public void PaddingChainLeafMark()
    {
        _padding.Leaf.MarkDirty(DirtyFlags.Layout);
        _padding.Context.Update(FrameTime);
    }

    /// <summary>Marks the leaf of the percent maximum width chain, then runs one <c>Update</c>.</summary>
    [Benchmark]
    public void MaxWidthChainLeafMark()
    {
        _maxWidth.Leaf.MarkDirty(DirtyFlags.Layout);
        _maxWidth.Context.Update(FrameTime);
    }
}

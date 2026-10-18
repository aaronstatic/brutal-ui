using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Holds a budget of measure callback calls for one leaf mark in a deep percent chain.
/// </summary>
/// <remarks>
/// <para>
/// This class closes the hand-off of CR-1 of the flexbox-layout code review. A chain of 20 levels below the root
/// alternates <c>Row</c> and <c>Column</c>. The last level is a text leaf. A mark on the leaf dirties each level.
/// With a percent padding on each level, the review counted 3031 callback calls for one <c>Update</c>. With a percent
/// maximum width, the leaf gets 529 different constraints, and the review counted 5674 calls. The count grows about
/// 1.7 times for each level.
/// </para>
/// <para>
/// A call count is deterministic, so the budget lives in a test. The budgets have a margin of about 30 percent
/// (Assumption A8). The <c>PercentChainBenchmark</c> measures the time of the same chains.
/// <see cref="LayoutCacheTests"/> holds the first test of the padding chain.
/// </para>
/// </remarks>
public sealed class PercentChainBudgetTests
{
    private const float FrameTime = 0f;

    [Fact]
    public void LeafMarkInPercentPaddingChainStaysInBudget()
    {
        AssertLeafMarkStaysInBudget(ChainShape.PercentPadding, ChainTree.PaddingCallBudget);
    }

    [Fact]
    public void LeafMarkInPercentMaxWidthChainStaysInBudget()
    {
        AssertLeafMarkStaysInBudget(ChainShape.PercentMaxWidth, ChainTree.MaxWidthCallBudget);
    }

    private static void AssertLeafMarkStaysInBudget(ChainShape shape, int budget)
    {
        ChainTree chain = ChainTree.Build(shape, ChainTree.Depth);

        chain.Context.Update(FrameTime);
        LayoutFixture.AssertNoLayoutBits(chain.Root);
        Assert.True(chain.Text.Count > 0, "The first pass measures the leaf.");
        chain.Text.Reset();

        chain.Leaf.MarkDirty(DirtyFlags.Layout);
        chain.Context.Update(FrameTime);

        int calls = chain.Text.Count;
        Assert.True(calls > 0, "The mark measures the leaf again.");
        Assert.True(calls <= budget, $"A leaf mark in the {shape} chain gave {calls} callback calls. The budget is {budget}.");

        LayoutFixture.AssertNoLayoutBits(chain.Root);
        LayoutDifferentialTests.AssertMatchesFreshLayout(chain.Root, chain.Context.ViewportSize, $"after the leaf mark in the {shape} chain");
    }
}

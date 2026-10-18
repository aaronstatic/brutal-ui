using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using BrutalUi.Core;
using BrutalUi.Core.Tests;

namespace BrutalUi.Benchmarks;

/// <summary>
/// Prints four facts of the core tree that BenchmarkDotNet cannot show.
/// </summary>
/// <remarks>
/// <para>
/// BenchmarkDotNet has no column for a memory total, a call count, or a stack limit. This report prints one line
/// for each. The first line gives the bytes that <see cref="ThousandElementTree.Build"/> allocates. The bytes hold
/// the 1000 <c>Element</c> objects with their inline style and layout state, the child lists and the pool arrays,
/// the <c>All</c> array, the <c>UiContext</c>, and the two <c>TextMeasure</c> objects with their delegates, so the
/// number is not the size of the elements alone. The report builds and drops one tree first, then measures a second
/// build, so the number holds no one-time type set-up (CR-7). The second and the third line give the callback calls for
/// one leaf mark in a percent chain (CR-1). The last line gives the deepest plain column chain that passes on a 1 MB
/// stack (CR-10).
/// </para>
/// <para>
/// The report runs outside the frame loop, so it can allocate (Assumption A7). It returns 1 when a call count is
/// above its budget, else 0.
/// </para>
/// </remarks>
internal static class FactsReport
{
    private const int StackBytes = 1024 * 1024;
    private const float FrameTime = 0f;

    /// <summary>
    /// Prints the four lines that start with <c>facts:</c>.
    /// </summary>
    /// <returns>0 when each call count is in its budget, else 1.</returns>
    public static int Run()
    {
        // The first build runs the one-time type set-up. The report drops it, then measures a second build.
        WarmUpBuild();

        long before = GC.GetAllocatedBytesForCurrentThread();
        ThousandElementTree tree = ThousandElementTree.Build();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        int elements = tree.All.Length;
        GC.KeepAlive(tree);

        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"facts: tree elements {elements}, bytes {bytes}, bytes per element {bytes / (double)elements:F1}"));

        int paddingCalls = CountLeafMarkCalls(ChainShape.PercentPadding);
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"facts: percent padding chain depth {ChainTree.Depth}: {paddingCalls} callback calls for one leaf mark (budget {ChainTree.PaddingCallBudget})"));

        int maxWidthCalls = CountLeafMarkCalls(ChainShape.PercentMaxWidth);
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"facts: percent max width chain depth {ChainTree.Depth}: {maxWidthCalls} callback calls for one leaf mark (budget {ChainTree.MaxWidthCallBudget})"));

        int levels = StackProbe.FindLimit(StackBytes);
        string levelText = levels >= StackProbe.MaxDepth
            ? string.Create(CultureInfo.InvariantCulture, $"{StackProbe.MaxDepth} or more")
            : levels.ToString(CultureInfo.InvariantCulture);
#if DEBUG
        const string Build = "Debug";
#else
        const string Build = "Release";
#endif
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"facts: plain column chain on a 1 MB stack: {levelText} levels pass ({Build} build)"));

        bool inBudget = paddingCalls <= ChainTree.PaddingCallBudget && maxWidthCalls <= ChainTree.MaxWidthCallBudget;
        return inBudget ? 0 : 1;
    }

    // A separate method keeps the first tree out of the scope of the measure. No reference to it stays alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WarmUpBuild()
    {
        ThousandElementTree.Build();
    }

    private static int CountLeafMarkCalls(ChainShape shape)
    {
        ChainTree chain = ChainTree.Build(shape, ChainTree.Depth);

        chain.Context.Update(FrameTime);
        chain.Text.Reset();

        chain.Leaf.MarkDirty(DirtyFlags.Layout);
        chain.Context.Update(FrameTime);

        return chain.Text.Count;
    }
}

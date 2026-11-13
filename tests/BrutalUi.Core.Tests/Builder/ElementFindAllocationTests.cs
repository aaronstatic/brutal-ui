using System;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation gate of the query by name. After warm-up, <c>Find</c> and <c>Find&lt;T&gt;</c> must allocate
/// 0 bytes, so a game can call them in a frame.
/// </summary>
/// <remarks>
/// <para>
/// The scene is a tree of 218 named elements, three levels deep. The target is the <see cref="Label"/> with the
/// name <c>target</c>, and it is the last element in tree order, so a hit walks the whole tree. The miss walks
/// the whole tree too: each name check either rejects on the length or runs the byte compare.
/// </para>
/// <para>
/// The pattern is the one of <see cref="ElementAllocationTests"/>: the test warms up, calls
/// <see cref="AllocationGate.ReleaseAllocationContext"/>, reads <see cref="GC.GetAllocatedBytesForCurrentThread"/>
/// before and after the measured rounds, and asserts a difference of 0.
/// </para>
/// </remarks>
public sealed class ElementFindAllocationTests
{
    private const int BranchCount = 6;
    private const int ChildCount = 5;
    private const int LeafCount = 6;
    private const int Rounds = 1000;

    /// <summary>The name of the target label: 6 bytes, the last element in tree order.</summary>
    private static ReadOnlySpan<byte> TargetName => "target"u8;

    /// <summary>A miss name: 12 bytes, inside the fast reject of each element name, so it reaches the compare.</summary>
    private static ReadOnlySpan<byte> MissName => "e-1234567890"u8;

    /// <summary>The tree and the target. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required Element Root { get; init; }

        public required Label Target { get; init; }
    }

    /// <summary>
    /// A hit, a miss, and a typed hit allocate nothing after warm-up. One step runs all three calls, so
    /// <paramref name="Rounds"/> steps run 3000 walks of the tree.
    /// </summary>
    [Fact]
    public void FindAndFindTAllocateNothingAfterWarmUp()
    {
        Scene scene = BuildScene();

        // Warm up with one call of each. The step counts 3, so the warm-up ran the three paths.
        Assert.Equal(3, Step(scene));

        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int hits = 0;
        for (int i = 0; i < Rounds; i++)
        {
            hits += Step(scene);
        }

        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(3 * Rounds, hits);
        Assert.Equal(0L, bytes);
    }

    /// <summary>
    /// The measured calls of <see cref="FindAndFindTAllocateNothingAfterWarmUp"/> do real work. The three calls
    /// give the target, <see langword="null"/>, and the typed target, and the round counter reads
    /// <see cref="Rounds"/>, so the measured body cannot be empty.
    /// </summary>
    [Fact]
    public void TheMeasuredFindDoesRealWork()
    {
        Scene scene = BuildScene();

        Assert.Same(scene.Target, scene.Root.Find(TargetName));
        Assert.Null(scene.Root.Find(MissName));
        Assert.Same(scene.Target, scene.Root.Find<Label>(TargetName));

        int rounds = 0;
        int hits = 0;
        for (int i = 0; i < Rounds; i++)
        {
            hits += Step(scene);
            rounds++;
        }

        Assert.Equal(Rounds, rounds);
        Assert.Equal(3 * Rounds, hits);
    }

    /// <summary>Runs the three measured calls and counts the results that match the target.</summary>
    /// <param name="scene">The built tree.</param>
    /// <returns>3 when the hit finds the target, the miss finds nothing, and the typed hit finds the target.</returns>
    private static int Step(Scene scene)
    {
        int count = 0;
        if (ReferenceEquals(scene.Root.Find(TargetName), scene.Target))
        {
            count++;
        }

        if (scene.Root.Find(MissName) is null)
        {
            count++;
        }

        if (ReferenceEquals(scene.Root.Find<Label>(TargetName), scene.Target))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Builds the tree: a root, <see cref="BranchCount"/> branches, <see cref="ChildCount"/> children for each
    /// branch, and <see cref="LeafCount"/> leaves for each child. The target label is the last child of the root,
    /// so it is the last element in tree order.
    /// </summary>
    /// <returns>The scene with the root and the target.</returns>
    private static Scene BuildScene()
    {
        Element root = new Element { Name = "root" };
        int index = 0;

        for (int branch = 0; branch < BranchCount; branch++)
        {
            Element branchElement = new Element { Name = NextName(ref index) };
            root.Children.Add(branchElement);

            for (int child = 0; child < ChildCount; child++)
            {
                Element childElement = new Element { Name = NextName(ref index) };
                branchElement.Children.Add(childElement);

                for (int leaf = 0; leaf < LeafCount; leaf++)
                {
                    Element leafElement = new Element { Name = NextName(ref index) };
                    childElement.Children.Add(leafElement);
                }
            }
        }

        Label target = new Label(default(Label.CreateInfo)) { Name = "target" };
        root.Children.Add(target);

        return new Scene { Root = root, Target = target };
    }

    /// <summary>Gives the next element name and moves the counter.</summary>
    /// <param name="index">The counter of the built elements.</param>
    /// <returns>A name like <c>e42</c>.</returns>
    private static string NextName(ref int index)
    {
        index++;
        return "e" + index.ToString();
    }
}

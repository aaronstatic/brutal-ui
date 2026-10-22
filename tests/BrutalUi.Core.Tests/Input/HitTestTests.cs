using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the hit test of <see cref="UiContext"/>: the order of the walk, the gap of a parent, a point outside each
/// root, two overlapped children, two overlapped roots, and a tree deeper than the start stack.
/// </summary>
public sealed class HitTestTests
{
    /// <summary>
    /// A point in a child hits the child, and a point in a grandchild hits the grandchild. A descendant comes before
    /// its ancestor.
    /// </summary>
    [Fact]
    public void APointInAChildHitsTheChild()
    {
        PointerTree tree = new(nameof(HitTestTests) + "." + nameof(APointInAChildHitsTheChild));
        tree.Settle();

        Assert.Equal(new float4(0f, 0f, 100f, 100f), PointerTree.ViewportRect(tree.Left));
        Assert.Equal(new float4(100f, 0f, 100f, 100f), PointerTree.ViewportRect(tree.Right));
        Assert.Equal(new float4(100f, 0f, 50f, 50f), PointerTree.ViewportRect(tree.Inner));

        Assert.Same(tree.Left, tree.Context.HitTest(new float2(50f, 50f)));
        Assert.Same(tree.Right, tree.Context.HitTest(new float2(175f, 75f)));
        Assert.Same(tree.Inner, tree.Context.HitTest(new float2(125f, 25f)));

        // The test is half-open (Decision 6): the left and the top edge are inside, the right and the bottom edge
        // are outside.
        Assert.Same(tree.Left, tree.Context.HitTest(new float2(0f, 0f)));
        Assert.Same(tree.Inner, tree.Context.HitTest(new float2(100f, 0f)));
        Assert.Same(tree.Right, tree.Context.HitTest(new float2(100f, 75f)));
        Assert.Same(tree.Right, tree.Context.HitTest(new float2(150f, 25f)));
        Assert.Same(tree.Right, tree.Context.HitTest(new float2(125f, 50f)));
    }

    /// <summary>A point in the parent, but in no child, hits the parent.</summary>
    [Fact]
    public void APointInTheGapHitsTheParent()
    {
        PointerTree tree = new(nameof(HitTestTests) + "." + nameof(APointInTheGapHitsTheParent));
        tree.Settle();

        Assert.Equal(new float4(0f, 0f, 400f, 300f), PointerTree.ViewportRect(tree.Root));
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(300f, 200f)));
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(50f, 100f)));
        Assert.Same(tree.Root, tree.Context.HitTest(new float2(200f, 50f)));
    }

    /// <summary>A point outside each root hits nothing. A NaN point hits nothing.</summary>
    [Fact]
    public void APointOutsideEachRootHitsNothing()
    {
        PointerTree tree = new(nameof(HitTestTests) + "." + nameof(APointOutsideEachRootHitsNothing));
        tree.Settle();

        Assert.Null(tree.Context.HitTest(new float2(400f, 100f)));
        Assert.Null(tree.Context.HitTest(new float2(100f, 300f)));
        Assert.Null(tree.Context.HitTest(new float2(-1f, 10f)));
        Assert.Null(tree.Context.HitTest(new float2(700f, 500f)));
        Assert.Null(tree.Context.HitTest(new float2(float.NaN, float.NaN)));
        Assert.Null(tree.Context.HitTest(new float2(float.NaN, 10f)));
    }

    /// <summary>
    /// Two absolute children overlap. The later sibling paints on top, so it wins in the overlap. The earlier sibling
    /// wins outside the overlap.
    /// </summary>
    [Fact]
    public void TheLaterSiblingWinsWhereTwoChildrenOverlap()
    {
        Element root = new() { Name = "root" };
        root.Inline.Width = 300;
        root.Inline.Height = 300;

        Element first = new() { Name = "first" };
        first.Inline.Position = PositionType.Absolute;
        first.Inline.Left = 10;
        first.Inline.Top = 10;
        first.Inline.Width = 100;
        first.Inline.Height = 100;

        Element second = new() { Name = "second" };
        second.Inline.Position = PositionType.Absolute;
        second.Inline.Left = 50;
        second.Inline.Top = 50;
        second.Inline.Width = 100;
        second.Inline.Height = 100;

        root.Children.Add(first);
        root.Children.Add(second);
        UiContext context = NewContext();
        context.AddRoot(root);
        context.Update(0f);

        Assert.Equal(new float4(10f, 10f, 100f, 100f), PointerTree.ViewportRect(first));
        Assert.Equal(new float4(50f, 50f, 100f, 100f), PointerTree.ViewportRect(second));

        Assert.Same(second, context.HitTest(new float2(60f, 60f)));
        Assert.Same(second, context.HitTest(new float2(109f, 109f)));
        Assert.Same(first, context.HitTest(new float2(20f, 20f)));
        Assert.Same(second, context.HitTest(new float2(140f, 140f)));
        Assert.Same(root, context.HitTest(new float2(5f, 5f)));
    }

    /// <summary>Two roots overlap. The last root paints on top, so it wins in the overlap.</summary>
    [Fact]
    public void TheLastRootWinsWhereTwoRootsOverlap()
    {
        Element back = new() { Name = "back" };
        back.Inline.Width = 400;
        back.Inline.Height = 300;

        Element front = new() { Name = "front" };
        front.Inline.Width = 100;
        front.Inline.Height = 100;

        UiContext context = NewContext();
        context.AddRoot(back);
        context.AddRoot(front);
        context.Update(0f);

        Assert.Equal(new float4(0f, 0f, 400f, 300f), PointerTree.ViewportRect(back));
        Assert.Equal(new float4(0f, 0f, 100f, 100f), PointerTree.ViewportRect(front));

        Assert.Same(front, context.HitTest(new float2(50f, 50f)));
        Assert.Same(back, context.HitTest(new float2(200f, 200f)));
        Assert.Null(context.HitTest(new float2(500f, 50f)));
    }

    /// <summary>
    /// A chain of 41 elements is deeper than the start stack of 32. The walk grows the stack, hits the leaf, and leaves
    /// no element reference in a slot of the stack. A miss also leaves none.
    /// </summary>
    [Fact]
    public void ATreeDeeperThanTheStackHitsTheLeaf()
    {
        const int ancestors = 40;
        Element[] chain = new Element[ancestors + 1];
        for (int i = 0; i <= ancestors; i++)
        {
            chain[i] = new Element { Name = "level" + i };
            chain[i].Inline.Width = 10;
            chain[i].Inline.Height = 10;
            if (i > 0)
            {
                chain[i - 1].Children.Add(chain[i]);
            }
        }

        UiContext context = NewContext();
        context.AddRoot(chain[0]);
        context.Update(0f);

        Assert.True(ancestors + 1 > HitWalk.InitialStackSize);
        Assert.Equal(new float4(0f, 0f, 10f, 10f), PointerTree.ViewportRect(chain[ancestors]));

        Assert.Same(chain[ancestors], context.HitTest(new float2(5f, 5f)));
        Assert.True(context.HitStackLength >= ancestors + 1);
        AssertStackClean(context);

        Assert.Null(context.HitTest(new float2(50f, 50f)));
        AssertStackClean(context);
    }

    private static void AssertStackClean(UiContext context)
    {
        System.ReadOnlySpan<HitFrame> slots = context.HitStackSlots;
        for (int i = 0; i < slots.Length; i++)
        {
            Assert.Null(slots[i].Element);
        }
    }

    private static UiContext NewContext()
    {
        return new UiContext(new UiContext.CreateInfo { Measurer = new RecordingRenderer() })
        {
            ViewportSize = new float2(800f, 600f),
        };
    }
}

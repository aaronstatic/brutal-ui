using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The allocation check of the layout pass. After warm-up, a change, a mark, and one <c>Update</c> must
/// allocate 0 bytes.
/// </summary>
/// <remarks>
/// <para>
/// The tree has about 60 elements. It holds a row, a column, a wrapped row, a grow child, an absolute child, percent
/// sizes, a hidden element, and two leaves with a measure callback. The pattern is the one of
/// <see cref="TreeAllocationTests"/>.
/// </para>
/// <para>
/// The measured body rotates through six kinds of change. Each kind dirties another part of the scene, so the layout
/// pass runs that part and does not hit the cache: the width of the leaf, the width of one wrap item, the
/// <see cref="Display"/> of the hidden element, the size of the absolute element, a mark on each stub leaf, and the
/// viewport size. The kinds run in a fixed order, and the second round of a kind reverses the first.
/// </para>
/// </remarks>
public sealed class LayoutAllocationTests
{
    private const int Iterations = 100;
    private const int KindCount = 6;
    private const float NarrowWidth = 60f;
    private const float WideWidth = 80f;
    private const float WrapNarrow = 50f;
    private const float WrapWide = 60f;
    private const float AbsoluteSmall = 30f;
    private const float AbsoluteLarge = 40f;
    private const float ViewportSmallWidth = 700f;
    private const float ViewportLargeWidth = 800f;

    // The kinds of change that a step makes.
    private const int LeafKind = 0;
    private const int WrapKind = 1;
    private const int HiddenKind = 2;
    private const int AbsoluteKind = 3;
    private const int StubKind = 4;
    private const int ViewportKind = 5;

    /// <summary>The tree, the stubs, and the values that the measured body uses. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required UiContext Context { get; init; }

        public required Element Root { get; init; }

        /// <summary>The leaf of the first row. Its width changes in each step.</summary>
        public required Element Leaf { get; init; }

        /// <summary>The grow child that follows the leaf. It shows the effect of the width change.</summary>
        public required Element GrowChild { get; init; }

        /// <summary>The first item of the wrapped row. Its width changes in the wrap steps.</summary>
        public required Element WrapItem { get; init; }

        /// <summary>The hidden element. Its <see cref="Display"/> toggles in the hidden steps.</summary>
        public required Element Hidden { get; init; }

        /// <summary>The absolute element. Its size changes in the absolute steps.</summary>
        public required Element Absolute { get; init; }

        /// <summary>The leaf of the first row that holds the first stub.</summary>
        public required Element FirstStubLeaf { get; init; }

        /// <summary>The leaf of the section that holds the second stub.</summary>
        public required Element SecondStubLeaf { get; init; }

        public required MeasureStub FirstStub { get; init; }

        public required MeasureStub SecondStub { get; init; }

        public required int ElementCount { get; init; }

        /// <summary>The number of steps that ran. The step number gives the kind and the direction of the change.</summary>
        public int StepCount { get; set; }
    }

    private static Element Make(ref int count, string name)
    {
        count++;
        return new Element { Name = name };
    }

    private static Scene BuildScene()
    {
        int count = 0;
        MeasureStub firstStub = new(MeasureStub.ClampedText);
        MeasureStub secondStub = new(MeasureStub.WrappingText);

        Element root = Make(ref count, "root");
        root.Style.FlexDirection = FlexDirection.Column;
        root.Style.Padding = LengthEdges.All(4f);

        // A row with a grow child, a leaf that changes its width, a percent child, and a leaf with a callback.
        Element row = Make(ref count, "row");
        row.Style.FlexDirection = FlexDirection.Row;
        row.Style.Height = 40f;
        row.Style.JustifyContent = Justify.SpaceBetween;
        Element growChild = Make(ref count, "grow");
        growChild.Style.FlexGrow = 1f;
        Element leaf = Make(ref count, "leaf");
        leaf.Style.Width = NarrowWidth;
        Element percentChild = Make(ref count, "percent");
        percentChild.Style.Width = Length.Percent(25f);
        percentChild.Style.Height = Length.Percent(50f);
        Element stubLeaf = Make(ref count, "stub-leaf");
        stubLeaf.Layout.Measure = firstStub.Callback;
        row.Children.Add(growChild);
        row.Children.Add(leaf);
        row.Children.Add(percentChild);
        row.Children.Add(stubLeaf);
        root.Children.Add(row);

        // A column that grows. Each child has a height and a margin.
        Element column = Make(ref count, "column");
        column.Style.FlexGrow = 1f;
        column.Style.AlignItems = Align.Center;
        for (int i = 0; i < 8; i++)
        {
            Element item = Make(ref count, "column-item");
            item.Style.Width = 30f + i;
            item.Style.Height = 10f;
            item.Style.Margin = LengthEdges.All(1f);
            column.Children.Add(item);
        }

        root.Children.Add(column);

        // A wrapped row. The twelve items need three lines.
        Element wrap = Make(ref count, "wrap");
        wrap.Style.FlexDirection = FlexDirection.Row;
        wrap.Style.FlexWrap = FlexWrap.Wrap;
        wrap.Style.Width = 200f;
        wrap.Style.AlignContent = Align.SpaceBetween;
        Element wrapItem = null!;
        for (int i = 0; i < 12; i++)
        {
            Element item = Make(ref count, "wrap-item");
            item.Style.Width = WrapNarrow;
            item.Style.Height = 20f;
            wrap.Children.Add(item);
            if (i == 0)
            {
                wrapItem = item;
            }
        }

        root.Children.Add(wrap);

        // An absolute child with two children.
        Element absolute = Make(ref count, "absolute");
        absolute.Style.Position = PositionType.Absolute;
        absolute.Style.Right = 5f;
        absolute.Style.Top = 5f;
        absolute.Style.Width = AbsoluteSmall;
        absolute.Style.Height = AbsoluteSmall;
        for (int i = 0; i < 2; i++)
        {
            Element inner = Make(ref count, "absolute-item");
            inner.Style.FlexGrow = 1f;
            absolute.Children.Add(inner);
        }

        root.Children.Add(absolute);

        // A hidden element with three children.
        Element hidden = Make(ref count, "hidden");
        hidden.Style.Display = Display.None;
        for (int i = 0; i < 3; i++)
        {
            hidden.Children.Add(Make(ref count, "hidden-item"));
        }

        root.Children.Add(hidden);

        // A section of twelve cells. Each cell holds one child. One child is a leaf with a callback.
        Element secondStubLeaf = null!;
        Element section = Make(ref count, "section");
        section.Style.FlexDirection = FlexDirection.RowReverse;
        section.Style.Height = Length.Percent(10f);
        for (int i = 0; i < 12; i++)
        {
            Element cell = Make(ref count, "cell");
            cell.Style.FlexGrow = 1f;
            cell.Style.FlexBasis = Length.Percent(5f);
            cell.Style.Padding = LengthEdges.All(Length.Percent(1f));
            Element content = Make(ref count, "cell-content");
            content.Style.MinWidth = 2f;
            if (i == 5)
            {
                content.Layout.Measure = secondStub.Callback;
                secondStubLeaf = content;
            }

            cell.Children.Add(content);
            section.Children.Add(cell);
        }

        root.Children.Add(section);

        UiContext context = new();
        context.AddRoot(root);
        context.ViewportSize = new float2(ViewportLargeWidth, 600f);

        // The first Update lays out the tree. The second Update shows the clean path.
        context.Update(0.016f);
        context.Update(0.016f);

        return new Scene
        {
            Context = context,
            Root = root,
            Leaf = leaf,
            GrowChild = growChild,
            WrapItem = wrapItem,
            Hidden = hidden,
            Absolute = absolute,
            FirstStubLeaf = stubLeaf,
            SecondStubLeaf = secondStubLeaf,
            FirstStub = firstStub,
            SecondStub = secondStub,
            ElementCount = count,
        };
    }

    /// <summary>The measured body. It uses no lambda, no boxing, no string formatting, and no Assert.</summary>
    private static void Step(Scene scene)
    {
        int number = scene.StepCount++;
        int kind = number % KindCount;

        // The first round of a kind sets the second value, and the next round sets the first value again.
        bool second = ((number / KindCount) & 1) == 0;

        switch (kind)
        {
            case LeafKind:
                scene.Leaf.Style.Width = second ? WideWidth : NarrowWidth;
                scene.Leaf.MarkDirty(DirtyFlags.Layout);
                break;
            case WrapKind:
                scene.WrapItem.Style.Width = second ? WrapWide : WrapNarrow;
                scene.WrapItem.MarkDirty(DirtyFlags.Layout);
                break;
            case HiddenKind:
                scene.Hidden.Style.Display = second ? Display.Flex : Display.None;
                scene.Hidden.MarkDirty(DirtyFlags.Layout);
                break;
            case AbsoluteKind:
                scene.Absolute.Style.Width = second ? AbsoluteLarge : AbsoluteSmall;
                scene.Absolute.Style.Height = second ? AbsoluteLarge : AbsoluteSmall;
                scene.Absolute.MarkDirty(DirtyFlags.Layout);
                break;
            case StubKind:
                scene.FirstStubLeaf.MarkDirty(DirtyFlags.Layout);
                scene.SecondStubLeaf.MarkDirty(DirtyFlags.Layout);
                break;
            default:
                scene.Context.ViewportSize = new float2(second ? ViewportSmallWidth : ViewportLargeWidth, 600f);
                break;
        }

        scene.Context.Update(0.016f);
    }

    private static void Run(Scene scene, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            Step(scene);
        }
    }

    [Fact]
    public void LayoutStepAllocatesNothingAfterWarmUp()
    {
        Scene scene = BuildScene();

        // Warm-up: two rounds run each kind of change in both directions. This lets the JIT compile the code and
        // lets the lists reach their capacity.
        Run(scene, KindCount * 2);
        Run(scene, KindCount * 2);

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        Run(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void TheMeasuredBodyChangesARect()
    {
        Scene scene = BuildScene();
        Assert.InRange(scene.ElementCount, 55, 65);

        // The first round of the rotation sets the second value of each kind.
        float4 leafBefore = scene.Leaf.Layout.Rect;
        float growBefore = scene.GrowChild.Layout.Rect.Z;
        Run(scene, KindCount);
        Assert.Equal(WideWidth, scene.Leaf.Layout.Rect.Z, 0.001f);
        Assert.Equal(WrapWide, scene.WrapItem.Layout.Rect.Z, 0.001f);
        Assert.Equal(Display.Flex, scene.Hidden.Style.Display);
        Assert.Equal(AbsoluteLarge, scene.Absolute.Layout.Rect.Z, 0.001f);
        Assert.Equal(ViewportSmallWidth, scene.Root.Layout.Rect.Z, 0.001f);
        float growWide = scene.GrowChild.Layout.Rect.Z;

        // The second round reverses each change.
        Run(scene, KindCount);
        Assert.Equal(NarrowWidth, scene.Leaf.Layout.Rect.Z, 0.001f);
        Assert.Equal(WrapNarrow, scene.WrapItem.Layout.Rect.Z, 0.001f);
        Assert.Equal(Display.None, scene.Hidden.Style.Display);
        Assert.Equal(AbsoluteSmall, scene.Absolute.Layout.Rect.Z, 0.001f);
        Assert.Equal(ViewportLargeWidth, scene.Root.Layout.Rect.Z, 0.001f);

        // The grow child takes the free space. The wide leaf and the small viewport leave less space for it.
        Assert.Equal(growBefore, scene.GrowChild.Layout.Rect.Z, 0.001f);
        Assert.Equal(leafBefore.Z, scene.Leaf.Layout.Rect.Z, 0.001f);
        Assert.True(growWide < growBefore);
        Assert.Equal(DirtyFlags.None, scene.Root.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));
    }

    [Fact]
    public void TheHiddenElementShowsAndHidesItsChildren()
    {
        Scene scene = BuildScene();
        Assert.Equal(0f, scene.Hidden.Children[0].Layout.Rect.Z);

        // The first round shows the element. The second round hides it again.
        Run(scene, KindCount);
        Assert.Equal(Display.Flex, scene.Hidden.Style.Display);
        Assert.True(scene.Hidden.Layout.Rect.Z > 0f);
        Run(scene, KindCount);
        Assert.Equal(Display.None, scene.Hidden.Style.Display);
        Assert.Equal(0f, scene.Hidden.Layout.Rect.Z);
        Assert.Equal(0f, scene.Hidden.Layout.Rect.W);
    }

    [Fact]
    public void TheMeasuredBodyCallsEachStub()
    {
        Scene scene = BuildScene();

        // Each stub ran in the build.
        Assert.True(scene.FirstStub.Count > 0);
        Assert.True(scene.SecondStub.Count > 0);

        Run(scene, KindCount * 2);
        int firstBefore = scene.FirstStub.Count;
        int secondBefore = scene.SecondStub.Count;

        // The stub steps are about one in six, so 100 steps give 16 or 17 of them.
        Run(scene, Iterations);
        Assert.True(scene.FirstStub.Count > firstBefore);
        Assert.True(scene.SecondStub.Count > secondBefore);

        // One round with only the stub step calls each stub at least one more time.
        int firstRound = scene.FirstStub.Count;
        int secondRound = scene.SecondStub.Count;
        Run(scene, KindCount);
        Assert.True(scene.FirstStub.Count > firstRound);
        Assert.True(scene.SecondStub.Count > secondRound);
    }
}

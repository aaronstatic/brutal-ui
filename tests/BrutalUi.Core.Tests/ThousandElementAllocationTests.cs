using System;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The release gate of the first design pillar. After warm-up, a change, a mark, and one <c>Update</c> on the
/// 1000-element tree must allocate 0 bytes. The same holds for the frame: a change, a mark, one <c>Update</c>, and one
/// <c>Draw</c> into the <see cref="RecordingRenderer"/>.
/// </summary>
/// <remarks>
/// <para>
/// The pattern is the one of <see cref="TreeAllocationTests"/> and <see cref="LayoutAllocationTests"/>: the test builds
/// the scene, runs one <c>Update</c>, runs warm-up rounds of each kind, and then measures 100 rotating steps with
/// <see cref="GC.GetAllocatedBytesForCurrentThread"/>. The update gate measures the step alone. The draw gate measures
/// the step and the <c>Draw</c> call after it. Right before the measure, each gate calls
/// <see cref="AllocationGate.ReleaseAllocationContext"/>. The rotation shows and hides the settings panel, so the draw
/// walks two tree sizes, and the recording renderer reaches its peak size during the warm-up.
/// </para>
/// <para>
/// The tree is <see cref="ThousandElementTree"/>. The measured body rotates through eight kinds of change. Each kind
/// dirties another part of the tree, so the layout pass runs that part and does not hit the cache: the width of a
/// percent leaf, the width of a wrap item, the width of an absolute element, the <see cref="Display"/> of the hidden
/// settings panel, a mark on three text leaves, a reorder of the sidebar, the viewport size, and a mark on each
/// element. The kinds run in a fixed order, and the second round of a kind reverses the first.
/// </para>
/// </remarks>
public sealed class ThousandElementAllocationTests
{
    private const int Iterations = 100;
    private const int KindCount = 8;
    private const float Tolerance = 0.001f;

    /// <summary>The height of the body: 720 minus the padding of 16, the header of 48, and the footer of 64.</summary>
    private const float BodyHeight = 592f;

    /// <summary>The index of the grid slot that moves to another line when slot 0 gets wider.</summary>
    private const int WrapSlotIndex = 12;

    /// <summary>The width of the sidebar, in percent of the body width.</summary>
    private const float SidebarPercent = 20f;

    /// <summary>The width of a settings toggle. The last element of the tree is one of them.</summary>
    private const float SettingsToggleWidth = 24f;

    // The kinds of change that a step makes. A new kind gets a new constant after FullKind, and KindCount grows.
    // Each test finds its kind by the constant and does not depend on the place of the kind in the order.
    private const int LeafKind = 0;
    private const int WrapKind = 1;
    private const int AbsoluteKind = 2;
    private const int HiddenKind = 3;
    private const int TextKind = 4;
    private const int ReorderKind = 5;
    private const int ViewportKind = 6;
    private const int FullKind = 7;

    /// <summary>The tree and the step counter. The test builds all of it before the measure.</summary>
    private sealed class Scene
    {
        public required ThousandElementTree Tree { get; init; }

        /// <summary>The renderer that records the calls of <c>Draw</c>.</summary>
        public required RecordingRenderer Renderer { get; init; }

        /// <summary>The number of steps that ran. The step number gives the kind and the direction of the change.</summary>
        public int StepCount { get; set; }
    }

    private static Scene BuildScene()
    {
        return new Scene { Tree = ThousandElementTree.Build(), Renderer = new RecordingRenderer() };
    }

    /// <summary>
    /// The measured body. It uses no lambda, no boxing, no string, no <c>foreach</c> over an interface, and no Assert.
    /// </summary>
    private static void Step(Scene scene)
    {
        ThousandElementTree tree = scene.Tree;
        int number = scene.StepCount++;
        int kind = number % KindCount;

        // The first round of a kind sets the second value, and the next round sets the first value again.
        bool second = ((number / KindCount) & 1) == 0;

        switch (kind)
        {
            case LeafKind:
                tree.StatValueLeaf.Style.Width = Length.Percent(
                    second ? ThousandElementTree.ValueWidePercent : ThousandElementTree.ValueNarrowPercent);
                tree.StatValueLeaf.MarkDirty(DirtyFlags.Layout);
                break;
            case WrapKind:
                tree.GridSlot.Style.Width = second ? ThousandElementTree.SlotWideWidth : ThousandElementTree.SlotNarrowWidth;
                tree.GridSlot.MarkDirty(DirtyFlags.Layout);
                break;
            case AbsoluteKind:
                tree.Tooltip.Style.Width = second
                    ? ThousandElementTree.TooltipWideWidth
                    : ThousandElementTree.TooltipNarrowWidth;
                tree.Tooltip.MarkDirty(DirtyFlags.Layout);
                break;
            case HiddenKind:
                tree.Settings.Style.Display = second ? Display.Flex : Display.None;
                tree.Settings.MarkDirty(DirtyFlags.Layout);
                break;
            case TextKind:
                tree.SidebarLabel.MarkDirty(DirtyFlags.Layout);
                tree.StatName.MarkDirty(DirtyFlags.Layout);
                tree.Paragraph.MarkDirty(DirtyFlags.Layout);
                break;
            case ReorderKind:
                // The move marks the sidebar.
                int last = tree.Sidebar.Children.Count - 1;
                if (second)
                {
                    tree.Sidebar.Children.Move(0, last);
                }
                else
                {
                    tree.Sidebar.Children.Move(last, 0);
                }

                break;
            case ViewportKind:
                tree.Context.ViewportSize = new float2(
                    second ? ThousandElementTree.ViewportNarrowWidth : ThousandElementTree.ViewportWideWidth,
                    ThousandElementTree.ViewportHeight);
                break;
            case FullKind:
                Element[] all = tree.All;
                for (int i = 0; i < all.Length; i++)
                {
                    all[i].MarkDirty(DirtyFlags.Layout);
                }

                break;
            default:
                // No kind reaches this branch, because the number is the remainder of KindCount. A new kind needs
                // a constant, a case, and a higher KindCount.
                break;
        }

        tree.Context.Update(0.016f);
    }

    private static void Run(Scene scene, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            Step(scene);
        }
    }

    /// <summary>The measured frame: one step, then one <c>Draw</c> into the recording renderer.</summary>
    private static void Frame(Scene scene)
    {
        Step(scene);
        scene.Tree.Context.Draw(scene.Renderer);
    }

    private static void RunFrames(Scene scene, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            Frame(scene);
        }
    }

    /// <summary>Builds the scene and runs the first <c>Update</c>, which lays out the full tree.</summary>
    private static Scene BuildWarmScene()
    {
        Scene scene = BuildScene();
        scene.Tree.Context.Update(0.016f);
        return scene;
    }

    [Fact]
    public void UpdateAllocatesNothingAfterWarmUp()
    {
        Scene scene = BuildWarmScene();

        // Warm-up: two rounds run each kind of change in both directions, two times. This lets the JIT compile the
        // code and lets the lists reach their capacity.
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
    public void DrawAllocatesNothingAfterWarmUp()
    {
        Scene scene = BuildWarmScene();

        // The first Draw sizes the paint stack and the arrays of the recording renderer for the hidden panel.
        scene.Tree.Context.Draw(scene.Renderer);

        // Warm-up: the rounds show and hide the settings panel, so the renderer reaches its peak size.
        RunFrames(scene, KindCount * 2);
        RunFrames(scene, KindCount * 2);

        // The release empties the allocation context, so a background GC cannot add bytes. See AllocationGate.
        AllocationGate.ReleaseAllocationContext();
        long before = GC.GetAllocatedBytesForCurrentThread();
        RunFrames(scene, Iterations);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0L, after - before);
    }

    [Fact]
    public void TheProbeSeesAnAllocation()
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        byte[] buffer = new byte[64];
        GC.KeepAlive(buffer);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.True(after - before >= 64, "The allocation counter does not see a 64-byte array.");
    }

    [Fact]
    public void TheTreeHoldsTheRequiredMix()
    {
        ThousandElementTree tree = ThousandElementTree.Build();
        Element[] all = tree.All;
        Assert.Equal(ThousandElementTree.ElementCount, all.Length);

        int rows = 0;
        int columns = 0;
        int wraps = 0;
        int grows = 0;
        int absolutes = 0;
        int percents = 0;
        int hidden = 0;
        int callbacks = 0;
        int backgrounds = 0;
        int borders = 0;
        int clips = 0;
        int fades = 0;
        int radii = 0;
        for (int i = 0; i < all.Length; i++)
        {
            Element element = all[i];
            Assert.NotNull(element);
            ref ComputedStyle style = ref element.Style;
            rows += style.FlexDirection == FlexDirection.Row ? 1 : 0;
            columns += style.FlexDirection == FlexDirection.Column ? 1 : 0;
            wraps += style.FlexWrap == FlexWrap.Wrap ? 1 : 0;
            grows += style.FlexGrow > 0f ? 1 : 0;
            absolutes += style.Position == PositionType.Absolute ? 1 : 0;
            percents += HasPercentSize(ref style) ? 1 : 0;
            hidden += style.Display == Display.None ? 1 : 0;
            callbacks += element.Layout.Measure is null ? 0 : 1;
            backgrounds += style.BackgroundColor.A > 0 ? 1 : 0;
            borders += HasVisibleBorder(ref style) ? 1 : 0;
            clips += style.Overflow != Overflow.Visible ? 1 : 0;
            fades += style.Opacity < 1f ? 1 : 0;
            radii += HasCornerRadius(ref style) ? 1 : 0;
        }

        Assert.True(rows >= 1);
        Assert.True(columns >= 1);
        Assert.True(wraps >= 1);
        Assert.True(grows >= 1);
        Assert.True(absolutes >= 1);
        Assert.True(percents >= 1);
        Assert.True(hidden >= 1);
        Assert.Equal(ThousandElementTree.TextLeafCount, callbacks);
        Assert.True(backgrounds >= 1);
        Assert.True(borders >= 1);
        Assert.True(clips >= 1);
        Assert.True(fades >= 1);
        Assert.True(radii >= 1);

        // Each element is distinct, and each one except the root has a parent.
        Assert.Same(tree.Root, all[0]);
        Assert.Null(all[0].Parent);
        for (int i = 1; i < all.Length; i++)
        {
            Assert.NotNull(all[i].Parent);
        }

        Assert.Equal(ThousandElementTree.ElementCount, CountSubtree(tree.Root));
    }

    [Fact]
    public void TheMeasuredBodyChangesEachPart()
    {
        Scene scene = BuildWarmScene();
        ThousandElementTree tree = scene.Tree;
        Element sidebarItem = tree.SidebarLabel.Parent!;
        Element statRow = tree.StatValueLeaf.Parent!;
        Element body = tree.Sidebar.Parent!;
        Element wrapSlot = tree.GridSlot.Parent!.Children[WrapSlotIndex];
        int last = tree.Sidebar.Children.Count - 1;

        // The state after the first Update. The body holds the height between the header and the footer.
        Assert.Equal(BodyHeight, body.Layout.Rect.W, Tolerance);
        float wrapBefore = wrapSlot.Layout.Rect.Y;

        // The wrap kind alone moves slot 12 to another line and back. The viewport stays at 1280 pixels.
        scene.StepCount = WrapKind;
        Run(scene, 1);
        Assert.NotEqual(wrapBefore, wrapSlot.Layout.Rect.Y, Tolerance);
        scene.StepCount = KindCount + WrapKind;
        Run(scene, 1);
        Assert.Equal(wrapBefore, wrapSlot.Layout.Rect.Y, Tolerance);
        scene.StepCount = 0;
        Assert.Equal(ThousandElementTree.SlotNarrowWidth, tree.GridSlot.Layout.Rect.Z, Tolerance);
        float valueBefore = tree.StatValueLeaf.Layout.Rect.Z;
        float sidebarBefore = tree.Sidebar.Layout.Rect.Z;
        Assert.Equal(body.Layout.Rect.Z * SidebarPercent / 100f, sidebarBefore, Tolerance);
        Assert.Equal(statRow.Layout.Rect.Z * ThousandElementTree.ValueNarrowPercent / 100f, valueBefore, Tolerance);
        Assert.Equal(ThousandElementTree.TooltipNarrowWidth, tree.Tooltip.Layout.Rect.Z, Tolerance);
        Assert.Equal(0f, tree.Settings.Layout.Rect.Z);
        Assert.Same(sidebarItem, tree.Sidebar.Children[0]);
        Assert.Equal(ThousandElementTree.ViewportWideWidth, tree.Root.Layout.Rect.Z, Tolerance);

        // The first round of the rotation sets the second value of each kind.
        Run(scene, KindCount);
        Assert.Equal(statRow.Layout.Rect.Z * ThousandElementTree.ValueWidePercent / 100f, tree.StatValueLeaf.Layout.Rect.Z, Tolerance);
        Assert.Equal(ThousandElementTree.SlotWideWidth, tree.GridSlot.Layout.Rect.Z, Tolerance);
        Assert.NotEqual(wrapBefore, wrapSlot.Layout.Rect.Y, Tolerance);
        Assert.Equal(ThousandElementTree.TooltipWideWidth, tree.Tooltip.Layout.Rect.Z, Tolerance);
        Assert.Equal(Display.Flex, tree.Settings.Style.Display);

        // The overlay takes no space in the flow, so the body keeps its height while the panel shows.
        Assert.Equal(BodyHeight, body.Layout.Rect.W, Tolerance);
        Assert.True(tree.Settings.Layout.Rect.Z > 0f);
        Assert.True(tree.Settings.Children[0].Layout.Rect.Z > 0f);
        Assert.Equal(SettingsToggleWidth, tree.All[^1].Layout.Rect.Z, Tolerance);
        Assert.Same(sidebarItem, tree.Sidebar.Children[last]);
        Assert.Equal(ThousandElementTree.ViewportNarrowWidth, tree.Root.Layout.Rect.Z, Tolerance);

        // The viewport kind changes each percent rect: the sidebar is 20 percent of the narrower body.
        Assert.Equal(body.Layout.Rect.Z * SidebarPercent / 100f, tree.Sidebar.Layout.Rect.Z, Tolerance);
        Assert.True(tree.Sidebar.Layout.Rect.Z < sidebarBefore - 1f);
        Assert.Equal(DirtyFlags.None, tree.Root.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));

        // The second round reverses each change.
        Run(scene, KindCount);
        Assert.Equal(valueBefore, tree.StatValueLeaf.Layout.Rect.Z, Tolerance);
        Assert.Equal(sidebarBefore, tree.Sidebar.Layout.Rect.Z, Tolerance);
        Assert.Equal(ThousandElementTree.SlotNarrowWidth, tree.GridSlot.Layout.Rect.Z, Tolerance);
        Assert.Equal(wrapBefore, wrapSlot.Layout.Rect.Y, Tolerance);
        Assert.Equal(ThousandElementTree.TooltipNarrowWidth, tree.Tooltip.Layout.Rect.Z, Tolerance);
        Assert.Equal(Display.None, tree.Settings.Style.Display);
        Assert.Equal(0f, tree.Settings.Layout.Rect.Z);
        Assert.Equal(0f, tree.Settings.Layout.Rect.W);
        Assert.Same(sidebarItem, tree.Sidebar.Children[0]);
        Assert.Equal(ThousandElementTree.ViewportWideWidth, tree.Root.Layout.Rect.Z, Tolerance);
        Assert.Equal(DirtyFlags.None, tree.Root.Dirty & (DirtyFlags.Layout | DirtyFlags.DescendantLayout));
    }

    [Fact]
    public void TheMeasuredBodyCallsTheTextCallbacks()
    {
        Scene scene = BuildWarmScene();
        ThousandElementTree tree = scene.Tree;

        // Each callback ran in the first Update.
        Assert.True(tree.LabelText.Count > 0);
        Assert.True(tree.ParagraphText.Count > 0);

        Run(scene, KindCount * 2);
        int labelBefore = tree.LabelText.Count;
        int paragraphBefore = tree.ParagraphText.Count;

        Run(scene, Iterations);
        Assert.True(tree.LabelText.Count > labelBefore);
        Assert.True(tree.ParagraphText.Count > paragraphBefore);

        // A text step alone calls both callbacks: it marks a label and a paragraph.
        tree.LabelText.Reset();
        tree.ParagraphText.Reset();
        scene.StepCount = TextKind;
        Run(scene, 1);
        Assert.True(tree.LabelText.Count > 0);
        Assert.True(tree.ParagraphText.Count > 0);
    }

    [Fact]
    public void TheFullStepVisitsEachVisibleElement()
    {
        Scene scene = BuildWarmScene();
        ThousandElementTree tree = scene.Tree;

        // Run steps until the next step is a full step and the settings panel is hidden. The hidden kind of the
        // second round hides the panel before the full step of that round.
        int guard = KindCount * 4;
        while (guard-- > 0 && (scene.StepCount % KindCount != FullKind || tree.Settings.Style.Display != Display.None))
        {
            Run(scene, 1);
        }

        Assert.Equal(FullKind, scene.StepCount % KindCount);
        Assert.Equal(Display.None, tree.Settings.Style.Display);
        Run(scene, 1);

        long generation = tree.Root.Layout.Generation;
        Assert.True(generation > 0);
        Element[] all = tree.All;
        int inside = 0;
        for (int i = 0; i < all.Length; i++)
        {
            Element element = all[i];
            if (IsInSubtree(element, tree.Settings))
            {
                inside++;
                Assert.Equal(0L, element.Layout.Generation);
                Assert.Equal(0f, element.Layout.Rect.Z);
            }
            else
            {
                Assert.Equal(generation, element.Layout.Generation);
            }
        }

        Assert.Equal(220, inside);
    }

    [Fact]
    public void TheDrawStepEmitsEachPaintedElement()
    {
        Scene scene = BuildWarmScene();
        ThousandElementTree tree = scene.Tree;
        Element[] all = tree.All;

        // The tree starts with the settings panel hidden. Draw the hidden frame without a step.
        Assert.Equal(Display.None, tree.Settings.Style.Display);
        tree.Context.Draw(scene.Renderer);
        int hiddenRects = CountCalls(scene.Renderer, DrawCallKind.Rect);
        Assert.Equal(CountPainted(all), hiddenRects);
        Assert.Equal(CountCalls(scene.Renderer, DrawCallKind.PushClip), CountCalls(scene.Renderer, DrawCallKind.PopClip));
        Assert.True(CountCalls(scene.Renderer, DrawCallKind.PushClip) > 0);

        // Show the panel, update, and draw the visible frame.
        tree.Settings.Style.Display = Display.Flex;
        tree.Settings.MarkDirty(DirtyFlags.Layout);
        tree.Context.Update(0.016f);
        tree.Context.Draw(scene.Renderer);
        int visibleRects = CountCalls(scene.Renderer, DrawCallKind.Rect);
        Assert.Equal(CountPainted(all), visibleRects);
        Assert.Equal(CountCalls(scene.Renderer, DrawCallKind.PushClip), CountCalls(scene.Renderer, DrawCallKind.PopClip));

        // The settings panel and each one of its rows hold a fill.
        Assert.Equal(1 + ThousandElementTree.SettingsRowCount, visibleRects - hiddenRects);
        Assert.Equal(74, visibleRects - hiddenRects);
    }

    private static int CountCalls(RecordingRenderer renderer, DrawCallKind kind)
    {
        int count = 0;
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        for (int i = 0; i < calls.Length; i++)
        {
            count += calls[i].Kind == kind ? 1 : 0;
        }

        return count;
    }

    /// <summary>
    /// Counts the elements that the draw walk paints with a Rect: a visible fill or a visible border, and no
    /// <see cref="Display.None"/> and no opacity product of 0 or less on the element or an ancestor.
    /// </summary>
    private static int CountPainted(Element[] all)
    {
        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            float opacity = 1f;
            bool hidden = false;
            for (Element? current = all[i]; current is not null; current = current.Parent)
            {
                if (current.Style.Display == Display.None)
                {
                    hidden = true;
                    break;
                }

                float own = current.Style.Opacity;
                opacity *= float.IsNaN(own) ? 1f : Math.Clamp(own, 0f, 1f);
            }

            if (hidden || opacity <= 0f)
            {
                continue;
            }

            ref ComputedStyle style = ref all[i].Style;
            bool fill = style.BackgroundColor.ScaleAlpha(opacity).A > 0;
            bool border = style.BorderWidth.Left.Unit == LengthUnit.Pixel
                && style.BorderWidth.Left.Value > 0f
                && style.BorderColor.ScaleAlpha(opacity).A > 0;
            count += fill || border ? 1 : 0;
        }

        return count;
    }

    private static bool HasVisibleBorder(ref ComputedStyle style)
    {
        return style.BorderWidth.Left.Unit == LengthUnit.Pixel
            && style.BorderWidth.Left.Value > 0f
            && style.BorderColor.A > 0;
    }

    private static bool HasCornerRadius(ref ComputedStyle style)
    {
        return style.CornerRadius.TopLeft > 0f
            || style.CornerRadius.TopRight > 0f
            || style.CornerRadius.BottomRight > 0f
            || style.CornerRadius.BottomLeft > 0f;
    }

    private static bool HasPercentSize(ref ComputedStyle style)
    {
        return style.Width.Unit == LengthUnit.Percent
            || style.Height.Unit == LengthUnit.Percent
            || style.MinWidth.Unit == LengthUnit.Percent
            || style.MinHeight.Unit == LengthUnit.Percent
            || style.MaxWidth.Unit == LengthUnit.Percent
            || style.MaxHeight.Unit == LengthUnit.Percent
            || style.FlexBasis.Unit == LengthUnit.Percent;
    }

    private static int CountSubtree(Element element)
    {
        int count = 1;
        for (int i = 0; i < element.Children.Count; i++)
        {
            count += CountSubtree(element.Children[i]);
        }

        return count;
    }

    private static bool IsInSubtree(Element element, Element ancestor)
    {
        for (Element? current = element; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }
}

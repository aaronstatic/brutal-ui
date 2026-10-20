using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using Brutal.Numerics;
using Xunit;

namespace BrutalUi.Core.Tests;

/// <summary>
/// Tests of the draw walk of <see cref="UiContext.Draw(IUiRenderer)"/> against a <see cref="RecordingRenderer"/>.
/// </summary>
/// <remarks>
/// Each test builds a small tree, runs <see cref="UiContext.Update(float)"/>, runs the draw, and compares the
/// recorded calls. The demo test prints its call list.
/// </remarks>
public sealed class PaintWalkTests
{
    private static readonly Rgba8 Red = new(255, 0, 0);
    private static readonly Rgba8 Green = new(0, 255, 0);
    private static readonly Rgba8 Blue = new(0, 0, 255);

    /// <summary>
    /// The demo of phase 1 (A9): a clipped root with a border and padding, and two children. Child B overflows the
    /// root. The walk draws the root, pushes the padding box clip, draws the children, and pops the clip.
    /// </summary>
    [Fact]
    public void DrawsThreeElementTreeWithOneClip()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = new() { Name = "root" };
        root.Style.FlexDirection = FlexDirection.Column;
        root.Style.Width = 200;
        root.Style.Height = 100;
        root.Style.Padding = LengthEdges.All(10);
        root.Style.BorderWidth = LengthEdges.All(2);
        root.Style.Overflow = Overflow.Hidden;
        root.Style.BackgroundColor = new Rgba8(10, 20, 30);
        context.AddRoot(root);

        AddBox(root, 50, 20, Red);
        AddBox(root, 300, 20, Green);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        string actual = renderer.Describe();
        TestContext.Current.TestOutputHelper?.WriteLine(actual);

        string expected = string.Join(
            "\n",
            "Begin 400x300 scale 1",
            "Rect 0,0,200,100 fill #0A141EFF border 0 #00000000 radius 0,0,0,0",
            "PushClip 2,2,196,96",
            "Rect 12,12,50,20 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
            "Rect 12,32,300,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
            "PopClip",
            "End");
        Assert.Equal(expected, actual);
        Assert.Equal(0, renderer.ClipDepth);
    }

    /// <summary>The rect of a leaf holds the sum of the relative positions of its ancestors and itself.</summary>
    [Fact]
    public void AddsTheParentPositionsOnTheWayDown()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = new() { Name = "root" };
        root.Style.Width = 300;
        root.Style.Height = 200;
        root.Style.Padding = LengthEdges.All(5);
        root.Style.BackgroundColor = Blue;
        context.AddRoot(root);

        Element middle = AddBox(root, 100, 100, Green);
        middle.Style.Margin.Left = 7;
        middle.Style.Margin.Top = 3;
        middle.Style.Padding = LengthEdges.All(4);

        Element leaf = AddBox(middle, 10, 10, Red);
        leaf.Style.Margin.Left = 2;
        leaf.Style.Margin.Top = 6;

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        Assert.Equal(5, calls.Length);
        Assert.Equal(DrawCallKind.Rect, calls[3].Kind);
        float4 leafRect = calls[3].Rect;

        // The relative positions: middle (5 + 7, 5 + 3) in root, leaf (4 + 2, 4 + 6) in middle.
        Assert.Equal(new float4(12, 8, 100, 100), middle.Layout.Rect);
        Assert.Equal(new float4(6, 10, 10, 10), leaf.Layout.Rect);

        float expectedX = root.Layout.Rect.X + middle.Layout.Rect.X + leaf.Layout.Rect.X;
        float expectedY = root.Layout.Rect.Y + middle.Layout.Rect.Y + leaf.Layout.Rect.Y;
        Assert.Equal(new float4(expectedX, expectedY, 10, 10), leafRect);
        Assert.Equal(new float4(18, 18, 10, 10), leafRect);
        Assert.Equal(new float4(12, 8, 100, 100), calls[2].Rect);
    }

    /// <summary>
    /// A clipped child that overflows its clipped parent gets the intersection. A clipped element outside the clip of
    /// its parent gets a width or a height of 0, not a negative value. The zero clip stays zero for its children.
    /// </summary>
    [Fact]
    public void IntersectsANestedClipWithTheParentClip()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = new() { Name = "root" };
        root.Style.Width = 200;
        root.Style.Height = 100;
        root.Style.Overflow = Overflow.Hidden;
        context.AddRoot(root);

        // Child A: (50, 20, 300, 50) overflows the root on the right.
        Element childA = AddBox(root, 300, 50, Red);
        childA.Style.Margin.Left = 50;
        childA.Style.Margin.Top = 20;
        childA.Style.Overflow = Overflow.Hidden;

        // Child B: (250, 10, 40, 40) lies right of the root clip.
        Element childB = AddBox(root, 40, 40, Green);
        childB.Style.Position = PositionType.Absolute;
        childB.Style.Left = 250;
        childB.Style.Top = 10;
        childB.Style.Overflow = Overflow.Hidden;

        // The child of B: (250, 10, 100, 10) in the viewport, inside the zero clip of B.
        Element childOfB = AddBox(childB, 100, 10, Blue);
        childOfB.Style.Overflow = Overflow.Hidden;

        // Child C: (10, 150, 20, 20) lies below the root clip.
        Element childC = AddBox(root, 20, 20, Blue);
        childC.Style.Position = PositionType.Absolute;
        childC.Style.Left = 10;
        childC.Style.Top = 150;
        childC.Style.Overflow = Overflow.Hidden;

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        float4[] clips = PushClipRects(renderer);
        float4[] expected =
        [
            new float4(0, 0, 200, 100),
            new float4(50, 20, 150, 50),
            new float4(250, 10, 0, 40),
            new float4(250, 10, 0, 10),
            new float4(10, 150, 20, 0),
        ];
        Assert.Equal(expected, clips);

        for (int i = 0; i < clips.Length; i++)
        {
            Assert.True(clips[i].Z >= 0f, "A clip width is negative.");
            Assert.True(clips[i].W >= 0f, "A clip height is negative.");
        }

        Assert.Equal(0, renderer.ClipDepth);
    }

    /// <summary>The pop of a clip comes after the last descendant of the clipped element and before its next sibling.</summary>
    [Fact]
    public void PopsEachClipAfterTheSubtree()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = new() { Name = "root" };
        root.Style.Width = 300;
        root.Style.Height = 200;
        context.AddRoot(root);

        Element clipped = AddBox(root, 100, 100, Blue);
        clipped.Style.Overflow = Overflow.Hidden;
        AddBox(clipped, 20, 20, Red);
        Element second = AddBox(clipped, 20, 20, Green);
        AddBox(second, 5, 5, Red);

        AddBox(root, 30, 30, Green);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        string expected = string.Join(
            "\n",
            "Begin 400x300 scale 1",
            "Rect 0,0,100,100 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
            "PushClip 0,0,100,100",
            "Rect 0,0,20,20 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
            "Rect 0,20,20,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
            "Rect 0,20,5,5 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
            "PopClip",
            "Rect 0,100,30,30 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
            "End");
        Assert.Equal(expected, renderer.Describe());
    }

    /// <summary>An element with a fill alpha of 0 gets no rect. Its children still draw.</summary>
    [Fact]
    public void SkipsTheRectOfATransparentElementAndDrawsItsChildren()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = new() { Name = "root" };
        root.Style.Width = 100;
        root.Style.Height = 100;
        root.Style.BackgroundColor = new Rgba8(255, 0, 0, 0);
        context.AddRoot(root);

        Element middle = AddBox(root, 50, 50, Rgba8.Transparent);
        AddBox(middle, 10, 10, Green);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        string expected = string.Join(
            "\n",
            "Begin 400x300 scale 1",
            "Rect 0,0,10,10 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
            "End");
        Assert.Equal(expected, renderer.Describe());
    }

    /// <summary>The walk draws the roots in the order of add. A root that leaves and comes back goes to the end.</summary>
    [Fact]
    public void DrawsTheRootsInTheOrderOfAdd()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element first = NewRoot(10, Red);
        Element second = NewRoot(20, Green);
        Element third = NewRoot(30, Blue);
        context.AddRoot(first);
        context.AddRoot(second);
        context.AddRoot(third);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,10,10 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
                "Rect 0,0,20,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
                "Rect 0,0,30,30 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());

        Assert.True(context.RemoveRoot(first));
        context.AddRoot(first);
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,20,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
                "Rect 0,0,30,30 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
                "Rect 0,0,10,10 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());
    }

    /// <summary>A context with no roots gives a frame with a begin and an end only.</summary>
    [Fact]
    public void DrawsOnlyBeginAndEndWithNoRoots()
    {
        UiContext context = new() { ViewportSize = new float2(640, 480) };

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("Begin 640x480 scale 1\nEnd", renderer.Describe());
    }

    /// <summary>A null renderer throws.</summary>
    [Fact]
    public void ThrowsForANullRenderer()
    {
        UiContext context = new();

        Assert.Throws<ArgumentNullException>(() => context.Draw(null!));
    }

    /// <summary>
    /// A renderer that calls the draw of the same context from its begin call throws. The context stays usable after
    /// the throw.
    /// </summary>
    [Fact]
    public void ThrowsForADrawDuringADraw()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };
        context.AddRoot(NewRoot(10, Red));
        context.Update(0f);

        ReentrantRenderer reentrant = new(context);
        Assert.Throws<InvalidOperationException>(() => context.Draw(reentrant));
        Assert.Equal(1, reentrant.BeginCount);

        RecordingRenderer renderer = new();
        context.Draw(renderer);
        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,10,10 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());
    }

    /// <summary>
    /// A child with <see cref="Display.None"/> draws nothing, also its subtree. The clip of the hidden subtree is not
    /// pushed. The next sibling draws and takes the space of the hidden child.
    /// </summary>
    [Fact]
    public void SkipsADisplayNoneSubtreeAndDrawsTheNextSibling()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Blue);
        context.AddRoot(root);

        Element hidden = AddBox(root, 50, 50, Red);
        hidden.Style.Display = Display.None;
        hidden.Style.Overflow = Overflow.Hidden;
        Element inner = AddBox(hidden, 10, 10, Green);
        inner.Style.Overflow = Overflow.Hidden;

        AddBox(root, 20, 20, Green);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,100,100 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
                "Rect 0,0,20,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());
        Assert.Equal(0, CountKind(renderer, DrawCallKind.PushClip));
    }

    /// <summary>A child with an opacity of 0 draws nothing, also its subtree and its clip.</summary>
    [Fact]
    public void SkipsAZeroOpacitySubtree()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Blue);
        context.AddRoot(root);

        Element faded = AddBox(root, 50, 50, Red);
        faded.Style.Opacity = 0f;
        faded.Style.Overflow = Overflow.Hidden;
        Element inner = AddBox(faded, 10, 10, Green);
        inner.Style.Overflow = Overflow.Hidden;

        AddBox(root, 20, 20, Green);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,100,100 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
                "Rect 0,50,20,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());
        Assert.Equal(0, CountKind(renderer, DrawCallKind.PushClip));
    }

    /// <summary>A root with <see cref="Display.None"/> draws nothing, also its subtree. The next root draws.</summary>
    [Fact]
    public void SkipsAHiddenRoot()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element hidden = NewRoot(100, Blue);
        hidden.Style.Display = Display.None;
        hidden.Style.Overflow = Overflow.Hidden;
        context.AddRoot(hidden);
        AddBox(hidden, 10, 10, Green);

        context.AddRoot(NewRoot(20, Red));

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,20,20 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());
        Assert.Equal(0, CountKind(renderer, DrawCallKind.PushClip));
    }

    /// <summary>The walk clamps a negative opacity to 0, so the element and its subtree are hidden.</summary>
    [Fact]
    public void TreatsANegativeOpacityAsHidden()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Blue);
        root.Style.Opacity = -0.5f;
        root.Style.Overflow = Overflow.Hidden;
        context.AddRoot(root);
        AddBox(root, 10, 10, Green);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("Begin 400x300 scale 1\nEnd", renderer.Describe());
        Assert.Equal(0, CountKind(renderer, DrawCallKind.PushClip));
    }

    /// <summary>An opacity of <c>NaN</c> counts as 1, so the element draws with its full alpha.</summary>
    [Fact]
    public void TreatsANaNOpacityAsOne()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Blue);
        root.Style.Opacity = float.NaN;
        context.AddRoot(root);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,100,100 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());
    }

    /// <summary>
    /// The opacity multiplies down the subtree. Two levels of 0.5 give 0.25, so an alpha of 255 gives 64, also on the
    /// border color.
    /// </summary>
    [Fact]
    public void MultipliesOpacityDownTheSubtree()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Red);
        root.Style.Opacity = 0.5f;
        context.AddRoot(root);

        Element child = AddBox(root, 50, 50, Green);
        child.Style.Opacity = 0.5f;
        child.Style.BorderColor = Blue;
        child.Style.BorderWidth = LengthEdges.All(2);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,100,100 fill #FF000080 border 0 #00000000 radius 0,0,0,0",
                "Rect 0,0,50,50 fill #00FF0040 border 2 #0000FF40 radius 0,0,0,0",
                "End"),
            renderer.Describe());
    }

    /// <summary>An element with a transparent fill and a visible border gets one rect.</summary>
    [Fact]
    public void DrawsABorderOnlyElement()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Rgba8.Transparent);
        root.Style.BorderColor = Red;
        root.Style.BorderWidth = LengthEdges.All(3);
        context.AddRoot(root);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,100,100 fill #00000000 border 3 #FF0000FF radius 0,0,0,0",
                "End"),
            renderer.Describe());
    }

    /// <summary>The rect gets the left border width (A4), not the top width.</summary>
    [Fact]
    public void UsesTheLeftBorderWidth()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Rgba8.Transparent);
        root.Style.BorderColor = Red;
        root.Style.BorderWidth.Left = 3;
        root.Style.BorderWidth.Top = 1;
        context.AddRoot(root);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        Assert.Equal(3, calls.Length);
        Assert.Equal(DrawCallKind.Rect, calls[1].Kind);
        Assert.Equal(3f, calls[1].BorderWidth);
    }

    /// <summary>A percent border width resolves to 0 (A4), so the border is not visible.</summary>
    [Fact]
    public void IgnoresAPercentBorderWidth()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Rgba8.Transparent);
        root.Style.BorderColor = Red;
        root.Style.BorderWidth = LengthEdges.All(Length.Percent(50));
        context.AddRoot(root);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal("Begin 400x300 scale 1\nEnd", renderer.Describe());
    }

    /// <summary>
    /// A border with a transparent color is not visible. With a transparent fill the walk emits no rect. With a visible
    /// fill the rect holds the width 0 and a transparent border color. A visible color with a width of 0 is the same.
    /// </summary>
    [Fact]
    public void SkipsAnInvisibleBorder()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element noFill = NewRoot(100, Rgba8.Transparent);
        noFill.Style.BorderWidth = LengthEdges.All(2);
        context.AddRoot(noFill);

        Element withFill = NewRoot(50, Green);
        withFill.Style.BorderWidth = LengthEdges.All(2);
        context.AddRoot(withFill);

        Element zeroWidth = NewRoot(20, Rgba8.Transparent);
        zeroWidth.Style.BorderColor = Red;
        context.AddRoot(zeroWidth);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,50,50 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());
    }

    /// <summary>The rect gets the corner radius of the style, in the order top-left, top-right, bottom-right, bottom-left.</summary>
    [Fact]
    public void PassesTheCornerRadius()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Red);
        root.Style.CornerRadius = new Corners(1f, 2f, 3f, 4f);
        context.AddRoot(root);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,100,100 fill #FF0000FF border 0 #00000000 radius 1,2,3,4",
                "End"),
            renderer.Describe());
    }

    /// <summary><see cref="Overflow.Scroll"/> clips to the padding box, like <see cref="Overflow.Hidden"/> (Decision 6).</summary>
    [Fact]
    public void ClipsAScrollContainer()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Blue);
        root.Style.Overflow = Overflow.Scroll;
        root.Style.BorderWidth = LengthEdges.All(2);
        context.AddRoot(root);
        AddBox(root, 20, 20, Green);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,100,100 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
                "PushClip 2,2,96,96",
                "Rect 2,2,20,20 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
                "PopClip",
                "End"),
            renderer.Describe());
    }

    /// <summary>The draw reads the tree only (A2): the dirty bits of each element stay the same.</summary>
    [Fact]
    public void DrawChangesNoDirtyBit()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Blue);
        context.AddRoot(root);
        Element child = AddBox(root, 50, 50, Green);
        Element grandchild = AddBox(child, 10, 10, Red);

        RecordingRenderer renderer = new();
        context.Update(0f);

        child.MarkDirty(DirtyFlags.Paint | DirtyFlags.Layout);
        grandchild.MarkDirty(DirtyFlags.Paint);

        DirtyFlags rootBefore = root.Dirty;
        DirtyFlags childBefore = child.Dirty;
        DirtyFlags grandchildBefore = grandchild.Dirty;
        Assert.NotEqual(DirtyFlags.None, rootBefore);
        Assert.NotEqual(DirtyFlags.None, childBefore);
        Assert.NotEqual(DirtyFlags.None, grandchildBefore);

        context.Draw(renderer);

        Assert.Equal(rootBefore, root.Dirty);
        Assert.Equal(childBefore, child.Dirty);
        Assert.Equal(grandchildBefore, grandchild.Dirty);
        Assert.Equal(5, renderer.Calls.Length);
    }

    /// <summary>
    /// A chain of 5000 elements draws with no stack overflow (A8). The test runs no update, so the layout recursion does
    /// not run. Each element has a fill, so the walk emits 5000 rects. The draw runs on a thread with a 64 KB stack,
    /// so a recursive walk stops the test host.
    /// </summary>
    [Fact]
    public void DrawsAChainOf5000LevelsWithNoOverflow()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element top = new();
        top.Style.BackgroundColor = Red;
        context.AddRoot(top);

        Element parent = top;
        for (int i = 1; i < 5000; i++)
        {
            Element child = new();
            child.Style.BackgroundColor = Red;
            parent.Children.Add(child);
            parent = child;
        }

        // A8: the small stack (64 KB) is the proof. The default stack of a test thread holds a recursive walk of 5000
        // levels too, but 64 KB does not.
        RecordingRenderer renderer = new();
        Exception? failure = null;
        Thread thread = new(
            () =>
            {
                try
                {
                    context.Draw(renderer);
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            },
            64 * 1024);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        Assert.Equal(5000, CountKind(renderer, DrawCallKind.Rect));
        Assert.Equal(0, renderer.ClipDepth);
    }

    /// <summary>
    /// The walk clamps an opacity above 1 to 1 and treats <c>NaN</c> as 1, also under a faded parent. Under a root of
    /// 0.5, each child draws with an alpha of 128.
    /// </summary>
    [Fact]
    public void ClampsAnOpacityAboveOne()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(100, Red);
        root.Style.Opacity = 0.5f;
        context.AddRoot(root);

        Element above = AddBox(root, 50, 50, Green);
        above.Style.Opacity = 2f;

        Element notANumber = AddBox(root, 20, 20, Blue);
        notANumber.Style.Opacity = float.NaN;

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,100,100 fill #FF000080 border 0 #00000000 radius 0,0,0,0",
                "Rect 0,0,50,50 fill #00FF0080 border 0 #00000000 radius 0,0,0,0",
                "Rect 0,50,20,20 fill #0000FF80 border 0 #00000000 radius 0,0,0,0",
                "End"),
            renderer.Describe());
    }

    /// <summary>
    /// A renderer that throws from the 35th rect, deep in a clipped chain, leaves the context usable. The next draw with
    /// a new renderer gives the full call list and a clip depth of 0.
    /// </summary>
    [Fact]
    public void RecoversAfterTheRendererThrowsInTheWalk()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };
        context.AddRoot(BuildClippedChain(40));
        context.Update(0f);

        ThrowingRenderer throwing = new(35);
        Assert.Throws<InvalidOperationException>(() => context.Draw(throwing));
        Assert.Equal(35, throwing.RectCount);

        RecordingRenderer renderer = new();
        context.Draw(renderer);

        // The chain has 40 levels. Each level has a rect and a clip.
        Assert.Equal(40, CountKind(renderer, DrawCallKind.Rect));
        Assert.Equal(40, CountKind(renderer, DrawCallKind.PushClip));
        Assert.Equal(40, CountKind(renderer, DrawCallKind.PopClip));
        Assert.Equal(1 + 40 + 40 + 40 + 1, renderer.Calls.Length);
        Assert.Equal(0, renderer.ClipDepth);
    }

    /// <summary>
    /// A paint hook that throws after the clip push leaves the context usable. The next draw with a new renderer gives
    /// the full call list.
    /// </summary>
    [Fact]
    public void RecoversAfterAHookThrows()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(200, Blue);
        root.Style.Overflow = Overflow.Hidden;
        context.AddRoot(root);

        ThrowingHookElement probe = new() { ThrowOnPaint = true };
        probe.Style.Width = 100;
        probe.Style.Height = 100;
        probe.Style.BackgroundColor = Green;
        probe.Style.Overflow = Overflow.Hidden;
        root.Children.Add(probe);
        AddBox(probe, 10, 10, Red);
        AddBox(root, 20, 20, Red);
        context.Update(0f);

        RecordingRenderer failed = new();
        Assert.Throws<InvalidOperationException>(() => context.Draw(failed));
        Assert.Equal(2, CountKind(failed, DrawCallKind.PushClip));
        Assert.Equal(0, CountKind(failed, DrawCallKind.PopClip));

        probe.ThrowOnPaint = false;
        RecordingRenderer renderer = new();
        context.Draw(renderer);

        Assert.Equal(
            string.Join(
                "\n",
                "Begin 400x300 scale 1",
                "Rect 0,0,200,200 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
                "PushClip 0,0,200,200",
                "Rect 0,0,100,100 fill #00FF00FF border 0 #00000000 radius 0,0,0,0",
                "PushClip 0,0,100,100",
                "Rect 0,0,10,10 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
                "PopClip",
                "Rect 0,100,20,20 fill #FF0000FF border 0 #00000000 radius 0,0,0,0",
                "PopClip",
                "End"),
            renderer.Describe());
        Assert.Equal(0, renderer.ClipDepth);
    }

    /// <summary>
    /// The frame stack keeps no reference to the elements after a draw. A root that leaves the context is
    /// collectable at once.
    /// </summary>
    [Fact]
    public void ReleasesTheElementsAfterADraw()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        WeakReference reference = DrawAndRemoveARoot(context);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(reference.IsAlive, "The frame stack keeps the removed root alive.");
    }

    /// <summary>Creates a root with a clipped child, draws it, removes it, and gives a weak reference to it.</summary>
    /// <param name="context">The context.</param>
    /// <returns>The weak reference to the root.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference DrawAndRemoveARoot(UiContext context)
    {
        Element root = NewRoot(100, Blue);
        root.Style.Overflow = Overflow.Hidden;
        context.AddRoot(root);
        AddBox(root, 50, 50, Green).Style.Overflow = Overflow.Hidden;
        context.Update(0f);

        RecordingRenderer renderer = new();
        context.Draw(renderer);
        Assert.Equal(2, CountKind(renderer, DrawCallKind.Rect));

        Assert.True(context.RemoveRoot(root));
        return new WeakReference(root);
    }

    /// <summary>Builds a chain of clipped, filled elements. Each level has a fixed size.</summary>
    /// <param name="levels">The count of levels.</param>
    /// <returns>The top of the chain.</returns>
    private static Element BuildClippedChain(int levels)
    {
        Element top = NewRoot(200, Blue);
        top.Style.Overflow = Overflow.Hidden;

        Element parent = top;
        for (int i = 1; i < levels; i++)
        {
            Element level = AddBox(parent, 100, 100, Green);
            level.Style.Overflow = Overflow.Hidden;
            parent = level;
        }

        return top;
    }

    /// <summary>
    /// A chain of 100 clipped levels grows the frame stack (the start length is 32). The leaf after the chain still draws
    /// after the last chain rect, and each clip pops before it.
    /// </summary>
    [Fact]
    public void KeepsTheOrderWhenTheStackGrows()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(200, Blue);
        context.AddRoot(root);

        Element parent = root;
        for (int i = 0; i < 100; i++)
        {
            Element level = AddBox(parent, 100, 100, Green);
            level.Style.Overflow = Overflow.Hidden;
            parent = level;
        }

        AddBox(root, 10, 10, Red);

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        ReadOnlySpan<DrawCall> calls = renderer.Calls;

        // Begin, the root rect, 100 times (rect and push), 100 pops, the leaf rect, and End.
        Assert.Equal(1 + 1 + 200 + 100 + 1 + 1, calls.Length);
        Assert.Equal(DrawCallKind.Begin, calls[0].Kind);
        Assert.Equal(DrawCallKind.Rect, calls[1].Kind);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(DrawCallKind.Rect, calls[2 + (2 * i)].Kind);
            Assert.Equal(DrawCallKind.PushClip, calls[3 + (2 * i)].Kind);
        }

        for (int i = 202; i < 302; i++)
        {
            Assert.Equal(DrawCallKind.PopClip, calls[i].Kind);
        }

        Assert.Equal(DrawCallKind.Rect, calls[302].Kind);
        Assert.Equal(Red, calls[302].Color);
        Assert.Equal(DrawCallKind.End, calls[303].Kind);
        Assert.Equal(0, renderer.ClipDepth);
    }

    /// <summary>
    /// The demo of phase 2: a root, a hidden child with its own child, a panel with an opacity of 0.5, and a child of the
    /// panel with an opacity of 0.5. The hidden child draws no rect. The panel fill has an alpha of 128, and the child
    /// fill has an alpha of 64.
    /// </summary>
    [Fact]
    public void DrawsHiddenAndFadedTree()
    {
        UiContext context = new() { ViewportSize = new float2(400, 300) };

        Element root = NewRoot(200, Blue);
        context.AddRoot(root);

        Element hidden = AddBox(root, 50, 50, Red);
        hidden.Style.Display = Display.None;
        AddBox(hidden, 10, 10, Green);

        Element panel = AddBox(root, 100, 100, Green);
        panel.Style.Opacity = 0.5f;

        Element faded = AddBox(panel, 40, 40, Red);
        faded.Style.Opacity = 0.5f;

        RecordingRenderer renderer = new();
        context.Update(0f);
        context.Draw(renderer);

        string actual = renderer.Describe();
        TestContext.Current.TestOutputHelper?.WriteLine(actual);

        string expected = string.Join(
            "\n",
            "Begin 400x300 scale 1",
            "Rect 0,0,200,200 fill #0000FFFF border 0 #00000000 radius 0,0,0,0",
            "Rect 0,0,100,100 fill #00FF0080 border 0 #00000000 radius 0,0,0,0",
            "Rect 0,0,40,40 fill #FF000040 border 0 #00000000 radius 0,0,0,0",
            "End");
        Assert.Equal(expected, actual);
    }

    /// <summary>Counts the recorded calls of one kind.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="kind">The kind to count.</param>
    /// <returns>The count.</returns>
    private static int CountKind(RecordingRenderer renderer, DrawCallKind kind)
    {
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        int count = 0;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == kind)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Adds a child with a fixed size and a fill color.</summary>
    /// <param name="parent">The parent.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="color">The fill color.</param>
    /// <returns>The child.</returns>
    private static Element AddBox(Element parent, float width, float height, Rgba8 color)
    {
        Element child = new();
        child.Style.Width = width;
        child.Style.Height = height;
        child.Style.BackgroundColor = color;
        parent.Children.Add(child);
        return child;
    }

    /// <summary>Creates a square root with a fill color.</summary>
    /// <param name="size">The width and the height in pixels.</param>
    /// <param name="color">The fill color.</param>
    /// <returns>The root. The caller adds it to a context.</returns>
    private static Element NewRoot(float size, Rgba8 color)
    {
        Element root = new();
        root.Style.Width = size;
        root.Style.Height = size;
        root.Style.BackgroundColor = color;
        return root;
    }

    /// <summary>Gives the rects of the recorded clip pushes, in call order.</summary>
    /// <param name="renderer">The renderer.</param>
    /// <returns>The rects.</returns>
    private static float4[] PushClipRects(RecordingRenderer renderer)
    {
        ReadOnlySpan<DrawCall> calls = renderer.Calls;
        int count = 0;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.PushClip)
            {
                count++;
            }
        }

        float4[] rects = new float4[count];
        int next = 0;
        for (int i = 0; i < calls.Length; i++)
        {
            if (calls[i].Kind == DrawCallKind.PushClip)
            {
                rects[next++] = calls[i].Rect;
            }
        }

        return rects;
    }

    /// <summary>An element whose paint hook throws after the clip push, while <see cref="ThrowOnPaint"/> is set.</summary>
    private sealed class ThrowingHookElement : Element
    {
        /// <summary>Gets or sets a value that tells whether the paint hook throws.</summary>
        public bool ThrowOnPaint { get; set; }

        /// <inheritdoc/>
        internal override void PaintContent(IUiRenderer renderer, in float4 borderBox, float opacity)
        {
            if (ThrowOnPaint)
            {
                throw new InvalidOperationException("The paint hook throws.");
            }
        }
    }

    /// <summary>A renderer that throws from the rect call with a given number.</summary>
    private sealed class ThrowingRenderer : IUiRenderer
    {
        private readonly int _throwAt;

        /// <summary>Creates the renderer.</summary>
        /// <param name="throwAt">The number of the rect call that throws, counted from 1.</param>
        public ThrowingRenderer(int throwAt)
        {
            _throwAt = throwAt;
        }

        /// <summary>Gets the count of rect calls, also the call that threw.</summary>
        public int RectCount { get; private set; }

        /// <inheritdoc/>
        public void Begin(in float2 displaySize, float scale)
        {
        }

        /// <inheritdoc/>
        public void PushClip(in float4 rect)
        {
        }

        /// <inheritdoc/>
        public void PopClip()
        {
        }

        /// <inheritdoc/>
        public void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor)
        {
            RectCount++;
            if (RectCount == _throwAt)
            {
                throw new InvalidOperationException("The renderer throws.");
            }
        }

        /// <inheritdoc/>
        public void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius)
        {
        }

        /// <inheritdoc/>
        public void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8)
        {
        }

        /// <inheritdoc/>
        public float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth)
        {
            return default;
        }

        /// <inheritdoc/>
        public void End()
        {
        }
    }

    /// <summary>A renderer that calls the draw of its context from <see cref="IUiRenderer.Begin"/>.</summary>
    private sealed class ReentrantRenderer : IUiRenderer
    {
        private readonly UiContext _context;

        /// <summary>Creates the renderer.</summary>
        /// <param name="context">The context to draw again from the begin call.</param>
        public ReentrantRenderer(UiContext context)
        {
            _context = context;
        }

        /// <summary>Gets the count of begin calls.</summary>
        public int BeginCount { get; private set; }

        /// <inheritdoc/>
        public void Begin(in float2 displaySize, float scale)
        {
            BeginCount++;
            _context.Draw(this);
        }

        /// <inheritdoc/>
        public void PushClip(in float4 rect)
        {
        }

        /// <inheritdoc/>
        public void PopClip()
        {
        }

        /// <inheritdoc/>
        public void Rect(in float4 rect, in Rgba8 color, in Corners radius, float borderWidth, in Rgba8 borderColor)
        {
        }

        /// <inheritdoc/>
        public void Image(in float4 rect, TextureHandle texture, in float4 uv, in Rgba8 tint, in Corners radius)
        {
        }

        /// <inheritdoc/>
        public void Text(in float2 pos, FontHandle font, float size, in Rgba8 color, ReadOnlySpan<byte> utf8)
        {
        }

        /// <inheritdoc/>
        public float2 MeasureText(FontHandle font, float size, ReadOnlySpan<byte> utf8, float wrapWidth)
        {
            return default;
        }

        /// <inheritdoc/>
        public void End()
        {
        }
    }
}
